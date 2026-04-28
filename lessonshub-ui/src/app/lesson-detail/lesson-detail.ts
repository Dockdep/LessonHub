import { Component, OnInit, signal, inject, ViewChildren, QueryList, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule, MatExpansionPanel } from '@angular/material/expansion';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MarkdownModule } from 'ngx-markdown';
import { GenerationBanner } from '../generation-banner/generation-banner';
import { LessonService } from '../services/lesson.service';
import { JobsService } from '../services/jobs.service';
import { Lesson, Exercise, UpdateLessonInfo } from '../models/lesson.model';
import { JobStatus } from '../models/job.model';
import { GenerateExerciseDialog, GenerateExerciseDialogResult } from '../generate-exercise-dialog/generate-exercise-dialog';
import { ConfirmDialog } from '../confirm-dialog/confirm-dialog';
import { RegenerateLessonDialog, RegenerateLessonDialogResult } from '../regenerate-lesson-dialog/regenerate-lesson-dialog';
import { NotificationService } from '../services/notification.service';

@Component({
  selector: 'app-lesson-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatExpansionModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatDialogModule,
    MarkdownModule,
    GenerationBanner
  ],
  templateUrl: './lesson-detail.html',
  styleUrl: './lesson-detail.css'
})
export class LessonDetail implements OnInit {
  lesson = signal<Lesson | null>(null);
  isLoading = signal(true);
  error = signal('');
  isGeneratingExercise = signal(false);
  isRegenerating = signal(false);

  // Phased status for the GenerationBanner — one signal per concurrent kind
  // of work. Empty string hides the banner. Driven by the JobEvent stream.
  regenerationPhase = signal<'queued' | 'generating' | ''>('');
  exercisePhase = signal<'queued' | 'generating' | ''>('');
  exerciseLabel = signal<'a new exercise' | 'a remedial exercise' | 'your answer review'>('a new exercise');
  isTogglingComplete = signal(false);
  isEditingInfo = signal(false);
  isSavingInfo = signal(false);
  prevLessonId = signal<number | null>(null);
  nextLessonId = signal<number | null>(null);
  submittingExerciseId = signal<number | null>(null);

  // Edit info form
  editForm = new FormGroup({
    name: new FormControl(''),
    lessonTopic: new FormControl(''),
    shortDescription: new FormControl('')
  });
  editKeyPoints: string[] = [];
  newKeyPoint = new FormControl('');

  // Answer text controls per exercise
  answerControls: { [exerciseId: number]: FormControl<string> } = {};

  private notify = inject(NotificationService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  @ViewChildren(MatExpansionPanel) exercisePanels!: QueryList<MatExpansionPanel>;

  constructor(
    private route: ActivatedRoute,
    private lessonService: LessonService,
    private jobsService: JobsService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const lessonId = Number(params.get('id'));
      if (lessonId) {
        this.isLoading.set(true);
        this.lesson.set(null);
        this.prevLessonId.set(null);
        this.nextLessonId.set(null);
        this.error.set('');
        this.answerControls = {};
        if (this.isBrowser) window.scrollTo({ top: 0 });
        this.loadLesson(lessonId);
      } else {
        this.error.set('Invalid Lesson ID');
        this.isLoading.set(false);
      }
    });
  }

  getAnswerControl(exerciseId: number): FormControl<string> {
    if (!this.answerControls[exerciseId]) {
      this.answerControls[exerciseId] = new FormControl('', { nonNullable: true });
    }
    return this.answerControls[exerciseId];
  }

  loadLesson(id: number): void {
    this.lessonService.getLessonById(id).subscribe({
      next: (data) => {
        this.lesson.set(data);
        this.isLoading.set(false);
        this.lessonService.getSiblingLessonIds(id).subscribe({
          next: (res) => {
            this.prevLessonId.set(res.prevLessonId);
            this.nextLessonId.set(res.nextLessonId);
          }
        });
        if (this.isBrowser) {
          // Restore any banners for jobs the user left running on a previous
          // visit (regenerate / exercise gen / retry). Catches the "did the
          // tab survive my coffee break" case across the whole detail page.
          this.restoreInFlightBanners(id);

          // Lazy content gen: backend no longer auto-generates on read.
          // If Content is still empty AND no LessonContentGenerate job is
          // already in flight (restoreInFlightBanners would have picked it
          // up), kick off a fresh one.
          if (!data.content?.trim()) {
            this.triggerLazyContentGen(id);
          }
        }
      },
      error: (err) => {
        console.error('Error loading lesson', err);
        this.error.set('Failed to load lesson details.');
        this.isLoading.set(false);
      }
    });
  }

  openGenerateDialog(review?: string): void {
    const dialogRef = this.dialog.open(GenerateExerciseDialog, {
      width: '480px',
      data: { review }
    });

    dialogRef.afterClosed().subscribe((result: GenerateExerciseDialogResult | null) => {
      if (result) {
        if (result.review) {
          this.retryExercise(result);
        } else {
          this.generateExercise(result);
        }
      }
    });
  }

  private generateExercise(params: GenerateExerciseDialogResult): void {
    const lesson = this.lesson();
    if (!lesson) return;

    this.isGeneratingExercise.set(true);
    this.exercisePhase.set('queued');
    this.exerciseLabel.set('a new exercise');
    this.lessonService.generateExercise(lesson.id, params.difficulty, params.comment).subscribe({
      next: (event) => {
        if (event.status === JobStatus.Running) {
          this.exercisePhase.set('generating');
          return;
        }
        if (event.status === JobStatus.Completed) {
          const exercise = this.lessonService.parseExerciseResult(event);
          if (exercise) {
            lesson.exercises.push(exercise);
            this.lesson.set({ ...lesson });
            this.openNewPanel();
            this.notify.success('Exercise ready.');
          }
          this.isGeneratingExercise.set(false);
          this.exercisePhase.set('');
        }
      },
      error: (err) => {
        console.error('Error generating exercise', err);
        const detail = err.error?.message || err.message || 'unknown error';
        this.error.set('Failed to generate exercise: ' + detail);
        this.notify.error('Exercise generation failed: ' + detail);
        this.isGeneratingExercise.set(false);
        this.exercisePhase.set('');
      }
    });
  }

  private retryExercise(params: GenerateExerciseDialogResult): void {
    const lesson = this.lesson();
    if (!lesson || !params.review) return;

    this.isGeneratingExercise.set(true);
    this.exercisePhase.set('queued');
    this.exerciseLabel.set('a remedial exercise');
    this.lessonService.retryExercise(lesson.id, params.difficulty, params.review, params.comment).subscribe({
      next: (event) => {
        if (event.status === JobStatus.Running) {
          this.exercisePhase.set('generating');
          return;
        }
        if (event.status === JobStatus.Completed) {
          const exercise = this.lessonService.parseExerciseResult(event);
          if (exercise) {
            lesson.exercises.push(exercise);
            this.lesson.set({ ...lesson });
            this.openNewPanel();
            this.notify.success('Remedial exercise ready.');
          }
          this.isGeneratingExercise.set(false);
          this.exercisePhase.set('');
        }
      },
      error: (err) => {
        console.error('Error retrying exercise', err);
        const detail = err.error?.message || err.message || 'unknown error';
        this.error.set('Failed to generate exercise: ' + detail);
        this.notify.error('Exercise retry failed: ' + detail);
        this.isGeneratingExercise.set(false);
        this.exercisePhase.set('');
      }
    });
  }

  private openNewPanel(): void {
    this.exercisePanels.forEach(p => p.close());
    if (this.exercisePanels.last) {
      this.exercisePanels.last.open();
    }
  }

  submitAnswer(exercise: Exercise): void {
    const ctrl = this.getAnswerControl(exercise.id);
    const answer = ctrl.value.trim();
    if (!answer) return;

    this.submittingExerciseId.set(exercise.id);
    this.exercisePhase.set('queued');
    this.exerciseLabel.set('your answer review');
    this.lessonService.submitExerciseAnswer(exercise.id, answer).subscribe({
      next: (event) => {
        if (event.status === JobStatus.Running) {
          this.exercisePhase.set('generating');
          return;
        }
        if (event.status === JobStatus.Completed) {
          const reviewed = this.lessonService.parseAnswerResult(event);
          if (reviewed) {
            exercise.answers.push(reviewed);
            ctrl.setValue('');
            this.notify.success('Your answer was reviewed.');
          }
          this.submittingExerciseId.set(null);
          this.exercisePhase.set('');
        }
      },
      error: (err) => {
        console.error('Error submitting answer', err);
        const detail = err.error?.message || err.message || 'unknown error';
        this.error.set('Failed to submit answer: ' + detail);
        this.notify.error('Answer review failed: ' + detail);
        this.submittingExerciseId.set(null);
        this.exercisePhase.set('');
      }
    });
  }

  startEditingInfo(): void {
    const lesson = this.lesson();
    if (!lesson) return;
    this.editForm.patchValue({
      name: lesson.name,
      shortDescription: lesson.shortDescription,
      lessonTopic: lesson.lessonTopic
    });
    this.editKeyPoints = [...lesson.keyPoints];
    this.newKeyPoint.setValue('');
    this.isEditingInfo.set(true);
  }

  cancelEditingInfo(): void {
    this.isEditingInfo.set(false);
  }

  addKeyPoint(): void {
    const val = this.newKeyPoint.value?.trim();
    if (!val) return;
    this.editKeyPoints.push(val);
    this.newKeyPoint.setValue('');
  }

  removeKeyPoint(index: number): void {
    this.editKeyPoints.splice(index, 1);
  }

  saveInfo(): void {
    const lesson = this.lesson();
    if (!lesson) return;

    this.isSavingInfo.set(true);
    this.error.set('');

    const v = this.editForm.value;
    const info: UpdateLessonInfo = {
      name: v.name || '',
      shortDescription: v.shortDescription || '',
      lessonTopic: v.lessonTopic || '',
      keyPoints: this.editKeyPoints
    };

    this.lessonService.updateLesson(lesson.id, info).subscribe({
      next: (updated) => {
        this.lesson.set(updated);
        this.isEditingInfo.set(false);
        this.isSavingInfo.set(false);
        this.notify.success('Lesson info updated!');
      },
      error: (err) => {
        console.error('Error saving lesson info', err);
        this.notify.error('Failed to save changes.');
        this.isSavingInfo.set(false);
      }
    });
  }

  confirmRegenerate(): void {
    const lesson = this.lesson();
    if (!lesson) return;

    const dialogRef = this.dialog.open(RegenerateLessonDialog, {
      width: '460px',
      data: { isTechnical: lesson.lessonType === 'Technical' }
    });

    dialogRef.afterClosed().subscribe((result: RegenerateLessonDialogResult | null) => {
      if (result) this.regenerateContent(result.bypassDocCache);
    });
  }

  private regenerateContent(bypassDocCache = false): void {
    const lesson = this.lesson();
    if (!lesson) return;

    this.isRegenerating.set(true);
    this.regenerationPhase.set('queued');
    this.error.set('');

    this.lessonService.regenerateContent(lesson.id, bypassDocCache).subscribe({
      next: (event) => {
        if (event.status === JobStatus.Running) {
          this.regenerationPhase.set('generating');
          return;
        }
        if (event.status === JobStatus.Completed) {
          const updated = this.lessonService.parseLessonResult(event);
          if (updated) {
            this.lesson.set(updated);
            this.notify.success('Lesson content regenerated!');
          }
          this.isRegenerating.set(false);
          this.regenerationPhase.set('');
        }
      },
      error: (err) => {
        console.error('Error regenerating content', err);
        this.notify.error('Failed to regenerate lesson.');
        this.isRegenerating.set(false);
        this.regenerationPhase.set('');
      }
    });
  }

  /**
   * Resume tracking every job the user has running on this lesson — covers
   * regenerate, generate-exercise, retry-exercise, and any other lesson-scoped
   * job type. Each found job dispatches to the matching banner state +
   * subscribes to the existing SignalR stream so the banner reappears mid-flight.
   */
  private restoreInFlightBanners(lessonId: number): void {
    this.jobsService.listInFlightForEntity('Lesson', lessonId).subscribe({
      next: (jobs) => {
        for (const job of jobs) {
          this.resumeJobByType(job.id, job.type, job.status);
        }
      },
    });
  }

  private resumeJobByType(jobId: string, type: string, currentStatus: JobStatus): void {
    const initialPhase: 'queued' | 'generating' = currentStatus === JobStatus.Running ? 'generating' : 'queued';
    const stream$ = this.jobsService.subscribeToExistingJob(jobId);

    switch (type) {
      case 'LessonContentGenerate':
      case 'LessonContentRegenerate':
        this.isRegenerating.set(true);
        this.regenerationPhase.set(initialPhase);
        this.consumeContentJobEvents(stream$);
        break;

      case 'ExerciseGenerate':
        this.isGeneratingExercise.set(true);
        this.exercisePhase.set(initialPhase);
        this.exerciseLabel.set('a new exercise');
        this.consumeExerciseJobEvents(stream$);
        break;

      case 'ExerciseRetry':
        this.isGeneratingExercise.set(true);
        this.exercisePhase.set(initialPhase);
        this.exerciseLabel.set('a remedial exercise');
        this.consumeExerciseJobEvents(stream$);
        break;

      // ExerciseReview is per-exercise; resuming would need exerciseId in the
      // banner. Skip for now — answer eventually lands when the lesson reloads.
    }
  }

  /** Shared exercise-job event consumer (used by generate, retry, and resume paths). */
  private consumeExerciseJobEvents(stream$: ReturnType<LessonService['generateExercise']>): void {
    const lesson = this.lesson();
    if (!lesson) return;
    stream$.subscribe({
      next: (event) => {
        if (event.status === JobStatus.Running) {
          this.exercisePhase.set('generating');
          return;
        }
        if (event.status === JobStatus.Completed) {
          const exercise = this.lessonService.parseExerciseResult(event);
          if (exercise && !lesson.exercises.some((e) => e.id === exercise.id)) {
            lesson.exercises.push(exercise);
            this.lesson.set({ ...lesson });
            this.openNewPanel();
            this.notify.success('Exercise ready.');
          }
          this.isGeneratingExercise.set(false);
          this.exercisePhase.set('');
        }
      },
      error: (err) => {
        console.error('Error on exercise job', err);
        const detail = err.error?.message || err.message || 'unknown error';
        this.notify.error('Exercise generation failed: ' + detail);
        this.isGeneratingExercise.set(false);
        this.exercisePhase.set('');
      },
    });
  }

  /**
   * Lazy content gen kicked off when GetLesson returns empty Content.
   *
   * First checks for an in-flight `LessonContentGenerate` job for this
   * lesson — if one exists (because the user navigated away and came back),
   * we resume tracking it. Only enqueue a fresh job when there's none
   * already running. Prevents the double-fire bug where rapid navigation
   * spawned duplicate jobs and double-billed the user.
   */
  private triggerLazyContentGen(lessonId: number): void {
    this.isRegenerating.set(true);
    this.regenerationPhase.set('queued');

    this.jobsService.findInFlight('LessonContentGenerate', 'Lesson', lessonId).subscribe({
      next: (existing) => {
        const stream$ = existing
          ? this.jobsService.subscribeToExistingJob(existing.id)
          : this.lessonService.generateContent(lessonId);
        this.consumeContentJobEvents(stream$);
      },
      error: () => {
        // If the lookup itself fails (rare — network blip), fall through to
        // a fresh job rather than blocking the user.
        this.consumeContentJobEvents(this.lessonService.generateContent(lessonId));
      },
    });
  }

  private consumeContentJobEvents(stream$: ReturnType<LessonService['generateContent']>): void {
    stream$.subscribe({
      next: (event) => {
        if (event.status === JobStatus.Running) {
          this.regenerationPhase.set('generating');
          return;
        }
        if (event.status === JobStatus.Completed) {
          const updated = this.lessonService.parseLessonResult(event);
          if (updated) {
            this.lesson.set(updated);
          } else {
            // Result was missing — re-fetch the lesson to be safe.
            const id = this.lesson()?.id;
            if (id) this.lessonService.getLessonById(id).subscribe((l) => this.lesson.set(l));
          }
          this.notify.success('Lesson content ready.');
          this.isRegenerating.set(false);
          this.regenerationPhase.set('');
        }
      },
      error: (err) => {
        console.error('Error generating content', err);
        const detail = err.error?.message || err.message || 'unknown error';
        this.error.set('Failed to generate lesson content: ' + detail);
        this.notify.error('Lesson content generation failed: ' + detail);
        this.isRegenerating.set(false);
        this.regenerationPhase.set('');
      },
    });
  }

  toggleComplete(): void {
    const lesson = this.lesson();
    if (!lesson) return;

    this.isTogglingComplete.set(true);
    this.lessonService.completeLesson(lesson.id).subscribe({
      next: (updated) => {
        this.lesson.set(updated);
        this.isTogglingComplete.set(false);
      },
      error: (err) => {
        console.error('Error toggling lesson completion', err);
        this.error.set('Failed to update lesson status.');
        this.isTogglingComplete.set(false);
      }
    });
  }
}
