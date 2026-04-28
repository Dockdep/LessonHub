import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, FormArray } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { LessonDayService } from '../services/lesson-day.service';
import { LessonPlanDetail as LessonPlanDetailModel, UpdateLessonPlanRequest } from '../models/lesson-day.model';
import { ConfirmDialog } from '../confirm-dialog/confirm-dialog';
import { NotificationService } from '../services/notification.service';
import { LessonDataStore } from '../services/lesson-data.store';
import { ShareDialog } from '../share-dialog/share-dialog';

@Component({
  selector: 'app-lesson-plan-detail',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './lesson-plan-detail.html',
  styleUrl: './lesson-plan-detail.css'
})
export class LessonPlanDetail implements OnInit {
  plan = signal<LessonPlanDetailModel | null>(null);
  isLoading = signal(true);
  isDeleting = signal(false);
  isSaving = signal(false);
  isEditing = signal(false);
  error = signal('');

  editForm = new FormGroup({
    name: new FormControl(''),
    topic: new FormControl(''),
    description: new FormControl(''),
    nativeLanguage: new FormControl(''),
    languageToLearn: new FormControl(''),
    useNativeLanguage: new FormControl(true),
    lessons: new FormArray<FormGroup>([])
  });

  get editLessons(): FormArray<FormGroup> {
    return this.editForm.get('lessons') as FormArray<FormGroup>;
  }

  private notify = inject(NotificationService);
  private store = inject(LessonDataStore);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private lessonDayService: LessonDayService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.loadPlan(id);
    } else {
      this.error.set('Invalid plan ID');
      this.isLoading.set(false);
    }
  }

  loadPlan(id: number): void {
    this.lessonDayService.getLessonPlanDetail(id).subscribe({
      next: (data) => {
        this.plan.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading plan detail', err);
        this.error.set('Failed to load lesson plan.');
        this.isLoading.set(false);
      }
    });
  }

  openShareDialog(): void {
    const plan = this.plan();
    if (!plan) return;

    this.dialog.open(ShareDialog, {
      width: '480px',
      data: { planId: plan.id, planName: plan.name }
    });
  }

  confirmDelete(): void {
    const plan = this.plan();
    if (!plan) return;

    const dialogRef = this.dialog.open(ConfirmDialog, {
      width: '420px',
      data: {
        title: 'Delete Plan',
        message: `Are you sure you want to delete "${plan.name}"? This will permanently delete all lessons, exercises, and answers associated with this plan.`,
        confirmText: 'Delete',
        cancelText: 'Cancel'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) this.deletePlan();
    });
  }

  startEditing(): void {
    const plan = this.plan();
    if (!plan) return;

    this.editForm.patchValue({
      name: plan.name,
      topic: plan.topic,
      description: plan.description,
      nativeLanguage: plan.nativeLanguage || '',
      languageToLearn: plan.languageToLearn || '',
      useNativeLanguage: plan.useNativeLanguage ?? true
    });

    this.editLessons.clear();
    plan.lessons.forEach(l => {
      this.editLessons.push(new FormGroup({
        id: new FormControl(l.id),
        lessonNumber: new FormControl(l.lessonNumber),
        name: new FormControl(l.name),
        shortDescription: new FormControl(l.shortDescription),
        lessonTopic: new FormControl(l.lessonTopic)
      }));
    });

    this.isEditing.set(true);
  }

  cancelEditing(): void {
    this.isEditing.set(false);
  }

  removeEditLesson(index: number): void {
    this.editLessons.removeAt(index);
    this.editLessons.controls.forEach((g, i) => g.get('lessonNumber')!.setValue(i + 1));
  }

  saveEdits(): void {
    const plan = this.plan();
    if (!plan) return;

    this.isSaving.set(true);
    this.error.set('');

    const v = this.editForm.value;
    const request: UpdateLessonPlanRequest = {
      name: v.name || '',
      topic: v.topic || '',
      description: v.description || '',
      nativeLanguage: v.nativeLanguage || undefined,
      languageToLearn: v.languageToLearn || undefined,
      useNativeLanguage: v.useNativeLanguage ?? true,
      lessons: (v.lessons || []).map((l: any) => ({
        id: l.id,
        lessonNumber: l.lessonNumber,
        name: l.name || '',
        shortDescription: l.shortDescription || '',
        lessonTopic: l.lessonTopic || '',
        keyPoints: []
      }))
    };

    this.lessonDayService.updateLessonPlan(plan.id, request).subscribe({
      next: (updated) => {
        this.plan.set(updated);
        this.isEditing.set(false);
        this.isSaving.set(false);
        this.store.onPlanChanged();
        this.notify.success('Plan updated!');
      },
      error: (err) => {
        console.error('Error updating plan', err);
        this.notify.error('Failed to save changes.');
        this.error.set('Failed to save changes.');
        this.isSaving.set(false);
      }
    });
  }

  private deletePlan(): void {
    const plan = this.plan();
    if (!plan) return;

    this.isDeleting.set(true);
    this.lessonDayService.deleteLessonPlan(plan.id).subscribe({
      next: () => {
        this.store.onPlanChanged();
        this.notify.success('Plan deleted.');
        this.router.navigate(['/lesson-plans']);
      },
      error: (err) => {
        console.error('Error deleting plan', err);
        this.notify.error('Failed to delete lesson plan.');
        this.isDeleting.set(false);
      }
    });
  }
}
