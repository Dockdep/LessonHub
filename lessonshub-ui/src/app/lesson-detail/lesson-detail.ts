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
import { LessonService } from '../services/lesson.service';
import { Lesson, Exercise, UpdateLessonInfo } from '../models/lesson.model';
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
    MarkdownModule
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
    this.lessonService.generateExercise(lesson.id, params.difficulty, params.comment).subscribe({
      next: (exercise) => {
        lesson.exercises.push(exercise);
        this.lesson.set({ ...lesson });
        this.isGeneratingExercise.set(false);
        this.openNewPanel();
      },
      error: (err) => {
        console.error('Error generating exercise', err);
        this.error.set('Failed to generate exercise: ' + (err.error?.message || err.message));
        this.isGeneratingExercise.set(false);
      }
    });
  }

  private retryExercise(params: GenerateExerciseDialogResult): void {
    const lesson = this.lesson();
    if (!lesson || !params.review) return;

    this.isGeneratingExercise.set(true);
    this.lessonService.retryExercise(lesson.id, params.difficulty, params.review, params.comment).subscribe({
      next: (exercise) => {
        lesson.exercises.push(exercise);
        this.lesson.set({ ...lesson });
        this.isGeneratingExercise.set(false);
        this.openNewPanel();
      },
      error: (err) => {
        console.error('Error retrying exercise', err);
        this.error.set('Failed to generate exercise: ' + (err.error?.message || err.message));
        this.isGeneratingExercise.set(false);
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
    this.lessonService.submitExerciseAnswer(exercise.id, answer).subscribe({
      next: (result) => {
        exercise.answers.push(result);
        ctrl.setValue('');
        this.submittingExerciseId.set(null);
      },
      error: (err) => {
        console.error('Error submitting answer', err);
        this.error.set('Failed to submit answer: ' + (err.error?.message || err.message));
        this.submittingExerciseId.set(null);
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
    this.error.set('');

    this.lessonService.regenerateContent(lesson.id, bypassDocCache).subscribe({
      next: (updated) => {
        this.lesson.set(updated);
        this.isRegenerating.set(false);
        this.notify.success('Lesson content regenerated!');
      },
      error: (err) => {
        console.error('Error regenerating content', err);
        this.notify.error('Failed to regenerate lesson.');
        this.isRegenerating.set(false);
      }
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
