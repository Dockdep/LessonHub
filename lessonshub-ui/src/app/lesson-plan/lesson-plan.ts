import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LessonPlanService } from '../services/lesson-plan.service';
import { NotificationService } from '../services/notification.service';
import { LessonDataStore } from '../services/lesson-data.store';
import { DocumentService } from '../services/document.service';
import { Document as Doc } from '../models/document.model';
import { LessonPlanRequest, LessonPlanResponse, LESSON_TYPES } from '../models/lesson-plan.model';
import { JobStatus } from '../models/job.model';

@Component({
  selector: 'app-lesson-plan',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatIconModule,
    MatSelectModule,
    MatTooltipModule
  ],
  templateUrl: './lesson-plan.html',
  styleUrl: './lesson-plan.css',
})
export class LessonPlan implements OnInit {
  lessonTypes = LESSON_TYPES;
  editingLessonIndex = -1;

  form = new FormGroup({
    lessonType: new FormControl('Default'),
    planName: new FormControl(''),
    topic: new FormControl(''),
    numberOfDays: new FormControl<number | null>(null),
    description: new FormControl(''),
    nativeLanguage: new FormControl(''),
    languageToLearn: new FormControl(''),
    useNativeLanguage: new FormControl(true),
    bypassDocCache: new FormControl(false)
  });

  isLoading = signal(false);
  isSaving = signal(false);
  error = signal('');
  saveSuccess = signal(false);
  generatedPlan = signal<LessonPlanResponse | null>(null);

  // Phased status drives the spinner copy: 'queued' between POST 202 and the
  // first JobUpdated event, 'generating' once Status=Running comes through.
  // Renders nothing when isLoading is false.
  generationPhase = signal<'queued' | 'generating' | ''>('');

  // Currently-attached document. Can come from either the /documents page
  // (?documentId=N query param) or the in-form picker. Optional and
  // orthogonal to lessonType — any of Default/Technical/Language can be
  // grounded in a document.
  sourceDocument = signal<Doc | null>(null);

  // Ingested documents the user can pick from in the in-form selector.
  availableDocuments = signal<Doc[]>([]);

  // Inline edit controls for generated plan name
  planNameEdit = new FormControl('');

  private notify = inject(NotificationService);
  private store = inject(LessonDataStore);
  private route = inject(ActivatedRoute);
  private docs = inject(DocumentService);

  constructor(private lessonPlanService: LessonPlanService) {}

  ngOnInit(): void {
    // Populate the picker with this user's ingested documents. Failure here
    // is non-fatal — the user can still create a plan without a doc.
    this.docs.list().subscribe({
      next: (docs) => this.availableDocuments.set(
        docs.filter((d) => d.ingestionStatus === 'Ingested')
      ),
      error: () => this.availableDocuments.set([]),
    });

    // Pre-select a document if we arrived via /documents → "Generate plan".
    const docIdParam = this.route.snapshot.queryParamMap.get('documentId');
    if (!docIdParam) return;
    const docId = Number(docIdParam);
    if (!Number.isFinite(docId)) return;
    this.docs.get(docId).subscribe({
      next: (doc) => {
        this.sourceDocument.set(doc);
        // Pre-fill plan name + topic from the file name as a convenience.
        // Do NOT touch lessonType — user picks whichever pedagogical voice
        // (Default / Technical / Language) they want; the document is just
        // a source of facts layered on top.
        this.form.patchValue({
          planName: doc.name.replace(/\.[^.]+$/, ''),
          topic: doc.name,
        });
      },
      error: () => {
        this.error.set('Could not load the source document. Continuing without it.');
      },
    });
  }

  attachDocumentById(id: number | null): void {
    if (id == null) {
      this.sourceDocument.set(null);
      return;
    }
    const match = this.availableDocuments().find((d) => d.id === id) ?? null;
    this.sourceDocument.set(match);
  }

  clearSourceDocument(): void {
    this.sourceDocument.set(null);
  }

  generateLessonPlan(): void {
    const { planName, topic } = this.form.value;
    if (!planName?.trim() || !topic?.trim()) {
      this.error.set('Please provide a plan name and topic.');
      return;
    }

    this.isLoading.set(true);
    this.error.set('');
    this.generatedPlan.set(null);
    this.generationPhase.set('queued');

    const v = this.form.value;
    const sourceDoc = this.sourceDocument();
    const isLanguage = v.lessonType === 'Language';
    const request: LessonPlanRequest = {
      lessonType: v.lessonType || 'Default',
      planName: v.planName || '',
      numberOfDays: v.numberOfDays || null,
      topic: v.topic || '',
      description: v.description || '',
      nativeLanguage: v.nativeLanguage || undefined,
      // Only send the target-language fields for Language lessons; ignored elsewhere.
      languageToLearn: isLanguage ? (v.languageToLearn || undefined) : undefined,
      useNativeLanguage: isLanguage ? !!v.useNativeLanguage : undefined,
      bypassDocCache: v.lessonType === 'Technical' ? !!v.bypassDocCache : false,
      // Send documentId whenever a document is attached, regardless of
      // lesson type. The Python service layers document grounding on top of
      // the chosen pedagogical voice.
      documentId: sourceDoc?.id ?? null,
    };

    this.lessonPlanService.generateLessonPlan(request).subscribe({
      next: (event) => {
        if (event.status === JobStatus.Running) {
          this.generationPhase.set('generating');
          return;
        }
        if (event.status === JobStatus.Completed) {
          const plan = this.lessonPlanService.parsePlanResult(event);
          if (plan) {
            this.generatedPlan.set(plan);
            this.planNameEdit.setValue(plan.planName);
          }
          this.isLoading.set(false);
          this.generationPhase.set('');
        }
      },
      error: (err) => {
        console.error('Error:', err);
        this.error.set('Error generating lesson plan: ' + (err.error?.message || err.message));
        this.isLoading.set(false);
        this.generationPhase.set('');
      }
    });
  }

  saveLessonPlan(): void {
    const plan = this.generatedPlan();
    if (!plan) return;

    // Apply edited plan name
    plan.planName = this.planNameEdit.value || plan.planName;

    this.isSaving.set(true);
    this.saveSuccess.set(false);
    this.error.set('');

    const v = this.form.value;
    const sourceDoc = this.sourceDocument();
    const docId = sourceDoc?.id ?? null;
    const isLanguage = v.lessonType === 'Language';
    this.lessonPlanService.saveLessonPlan(
      plan,
      v.description || '',
      v.lessonType || 'Default',
      v.nativeLanguage || undefined,
      docId,
      isLanguage ? (v.languageToLearn || undefined) : undefined,
      isLanguage ? !!v.useNativeLanguage : undefined,
    ).subscribe({
      next: () => {
        this.saveSuccess.set(true);
        this.isSaving.set(false);
        this.store.onPlanChanged();
        this.notify.success('Plan saved to library!');
        setTimeout(() => this.saveSuccess.set(false), 3000);
      },
      error: (error) => {
        console.error('Save error:', error);
        this.notify.error('Error saving lesson plan: ' + (error.error?.message || error.message));
        this.error.set('Error saving lesson plan: ' + (error.error?.message || error.message));
        this.isSaving.set(false);
      }
    });
  }

  removeLesson(index: number): void {
    const plan = this.generatedPlan();
    if (!plan) return;
    plan.lessons.splice(index, 1);
    plan.lessons.forEach((l, i) => l.lessonNumber = i + 1);
    this.generatedPlan.set({ ...plan });
    this.editingLessonIndex = -1;
  }

  resetForm(): void {
    this.form.reset({ lessonType: 'Default' });
    this.error.set('');
    this.saveSuccess.set(false);
    this.generatedPlan.set(null);
    this.editingLessonIndex = -1;
  }

  downloadJson(): void {
    const plan = this.generatedPlan();
    if (!plan) return;

    const dataStr = JSON.stringify(plan, null, 2);
    const dataUri = 'data:application/json;charset=utf-8,' + encodeURIComponent(dataStr);
    const name = (this.planNameEdit.value || 'plan').replace(/\s+/g, '_');

    const linkElement = document.createElement('a');
    linkElement.setAttribute('href', dataUri);
    linkElement.setAttribute('download', `${name}_lesson_plan.json`);
    linkElement.click();
  }
}
