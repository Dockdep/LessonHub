import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDialogModule } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { LessonDayService } from '../services/lesson-day.service';
import { LessonDataStore } from '../services/lesson-data.store';
import { NotificationService } from '../services/notification.service';
import { LessonDay, AvailableLesson } from '../models/lesson-day.model';

@Component({
  selector: 'app-lesson-days',
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDialogModule,
    MatListModule,
    MatChipsModule
  ],
  templateUrl: './lesson-days.html',
  styleUrl: './lesson-days.css',
})
export class LessonDays implements OnInit {
  private store = inject(LessonDataStore);
  private notify = inject(NotificationService);
  private lessonDayService = inject(LessonDayService);

  form = new FormGroup({
    selectedPlan: new FormControl<number | null>(null),
    selectedLesson: new FormControl<AvailableLesson | null>(null),
    selectedDate: new FormControl<Date>(new Date()),
    dayName: new FormControl(''),
    dayDescription: new FormControl('')
  });

  currentMonth = new Date().getMonth();
  currentYear = new Date().getFullYear();

  // From store
  lessonPlans = this.store.plans;
  lessonDays = this.store.lessonDays;

  // Local state
  availableLessons = signal<AvailableLesson[]>([]);
  isAssigning = signal(false);

  ngOnInit(): void {
    this.store.loadPlans();
    this.store.loadSchedule(this.currentYear, this.currentMonth + 1);
  }

  onLessonPlanChange(): void {
    const planId = this.form.value.selectedPlan;
    if (planId) {
      this.loadAvailableLessons(planId);
    } else {
      this.availableLessons.set([]);
    }
  }

  loadAvailableLessons(planId: number): void {
    this.lessonDayService.getAvailableLessons(planId).subscribe({
      next: (lessons) => this.availableLessons.set(lessons),
      error: () => this.notify.error('Failed to load lessons')
    });
  }

  populateFieldsForSelectedDate(): void {
    const date = this.form.value.selectedDate;
    if (!date) return;
    const existingDay = this.getLessonDayForDate(date);
    if (existingDay) {
      this.form.patchValue({ dayName: existingDay.name, dayDescription: existingDay.shortDescription });
    }
  }

  onDateChange(date: Date): void {
    this.form.patchValue({ selectedDate: date });
    const existingDay = this.getLessonDayForDate(date);
    if (existingDay) {
      this.form.patchValue({ dayName: existingDay.name, dayDescription: existingDay.shortDescription });
    } else {
      this.form.patchValue({ dayName: '', dayDescription: '' });
    }
  }

  assignLesson(): void {
    const v = this.form.value;
    if (!v.selectedLesson || !v.selectedDate) {
      this.notify.error('Please select a lesson and a date');
      return;
    }

    this.isAssigning.set(true);

    const request = {
      lessonId: v.selectedLesson.id,
      date: this.formatDateToYYYYMMDD(v.selectedDate),
      dayName: v.dayName || `Lesson Day - ${this.formatDate(v.selectedDate)}`,
      dayDescription: v.dayDescription || v.selectedLesson.shortDescription
    };

    this.lessonDayService.assignLesson(request).subscribe({
      next: () => {
        this.isAssigning.set(false);
        this.store.onScheduleChanged(this.getTodayStr());
        this.resetForm();
        this.notify.success('Lesson assigned!');
      },
      error: () => {
        this.notify.error('Failed to assign lesson');
        this.isAssigning.set(false);
      }
    });
  }

  unassignLesson(lessonId: number): void {
    this.lessonDayService.unassignLesson(lessonId).subscribe({
      next: () => {
        this.store.onScheduleChanged(this.getTodayStr());
        const planId = this.form.value.selectedPlan;
        if (planId) this.loadAvailableLessons(planId);
        this.notify.success('Lesson unassigned!');
      },
      error: () => this.notify.error('Failed to unassign lesson')
    });
  }

  resetForm(): void {
    this.form.patchValue({ selectedLesson: null, dayName: '', dayDescription: '' });
  }

  formatDate(date: Date): string {
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
  }

  formatDateToYYYYMMDD(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private getTodayStr(): string {
    return this.formatDateToYYYYMMDD(new Date());
  }

  getLessonDayForDate(date: Date): LessonDay | undefined {
    const dateStr = this.formatDateToYYYYMMDD(date);
    return this.lessonDays().find(day => day.date.startsWith(dateStr));
  }

  previousMonth(): void {
    if (this.currentMonth === 0) { this.currentMonth = 11; this.currentYear--; }
    else { this.currentMonth--; }
    this.store.loadSchedule(this.currentYear, this.currentMonth + 1, true);
  }

  nextMonth(): void {
    if (this.currentMonth === 11) { this.currentMonth = 0; this.currentYear++; }
    else { this.currentMonth++; }
    this.store.loadSchedule(this.currentYear, this.currentMonth + 1, true);
  }

  get currentMonthName(): string {
    return new Date(this.currentYear, this.currentMonth).toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }
}
