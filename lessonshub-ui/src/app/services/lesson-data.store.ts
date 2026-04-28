import { Injectable, signal, computed, inject } from '@angular/core';
import { LessonDayService } from './lesson-day.service';
import { LessonPlanShareService } from './lesson-plan-share.service';
import { LessonPlanSummary, LessonDay } from '../models/lesson-day.model';

@Injectable({ providedIn: 'root' })
export class LessonDataStore {
  // --- Plans cache ---
  private _plans = signal<LessonPlanSummary[]>([]);
  private _plansLoaded = signal(false);
  private _plansLoading = signal(false);

  readonly plans = this._plans.asReadonly();
  readonly plansLoading = this._plansLoading.asReadonly();
  readonly plansLoaded = this._plansLoaded.asReadonly();

  // --- Shared plans cache ---
  private _sharedPlans = signal<LessonPlanSummary[]>([]);
  private _sharedPlansLoaded = signal(false);
  private _sharedPlansLoading = signal(false);

  readonly sharedPlans = this._sharedPlans.asReadonly();
  readonly sharedPlansLoading = this._sharedPlansLoading.asReadonly();

  private shareService = inject(LessonPlanShareService);

  // --- Schedule cache ---
  private _scheduleMonth = signal<{ year: number; month: number } | null>(null);
  private _lessonDays = signal<LessonDay[]>([]);
  private _scheduleLoading = signal(false);

  readonly lessonDays = this._lessonDays.asReadonly();
  readonly scheduleLoading = this._scheduleLoading.asReadonly();

  // --- Today cache ---
  private _todayLessons = signal<LessonDay | null>(null);
  private _todayLoaded = signal(false);
  private _todayLoading = signal(false);

  readonly todayLessons = this._todayLessons.asReadonly();
  readonly todayLoading = this._todayLoading.asReadonly();

  // --- Derived ---
  readonly planCount = computed(() => this._plans().length);

  constructor(private lessonDayService: LessonDayService) {}

  // ========== Plans ==========

  loadPlans(force = false): void {
    if (this._plansLoaded() && !force) return;
    this._plansLoading.set(true);

    this.lessonDayService.getLessonPlans().subscribe({
      next: (data) => {
        this._plans.set(data);
        this._plansLoaded.set(true);
        this._plansLoading.set(false);
      },
      error: () => {
        this._plansLoading.set(false);
      }
    });
  }

  invalidatePlans(): void {
    this._plansLoaded.set(false);
  }

  refreshPlans(): void {
    this.loadPlans(true);
  }

  loadSharedPlans(force = false): void {
    if (this._sharedPlansLoaded() && !force) return;
    this._sharedPlansLoading.set(true);

    this.shareService.getSharedWithMe().subscribe({
      next: (data) => {
        this._sharedPlans.set(data);
        this._sharedPlansLoaded.set(true);
        this._sharedPlansLoading.set(false);
      },
      error: () => {
        this._sharedPlansLoading.set(false);
      }
    });
  }

  refreshSharedPlans(): void {
    this.loadSharedPlans(true);
  }

  // ========== Schedule ==========

  loadSchedule(year: number, month: number, force = false): void {
    const cached = this._scheduleMonth();
    if (!force && cached && cached.year === year && cached.month === month) return;

    this._scheduleLoading.set(true);
    this._scheduleMonth.set({ year, month });

    this.lessonDayService.getLessonDaysByMonth(year, month).subscribe({
      next: (days) => {
        this._lessonDays.set(days);
        this._scheduleLoading.set(false);
      },
      error: () => {
        this._scheduleLoading.set(false);
      }
    });
  }

  refreshSchedule(): void {
    const m = this._scheduleMonth();
    if (m) this.loadSchedule(m.year, m.month, true);
  }

  // ========== Today ==========

  loadToday(dateStr: string, force = false): void {
    if (this._todayLoaded() && !force) return;
    this._todayLoading.set(true);

    this.lessonDayService.getLessonDayByDate(dateStr).subscribe({
      next: (data) => {
        this._todayLessons.set(data);
        this._todayLoaded.set(true);
        this._todayLoading.set(false);
      },
      error: () => {
        this._todayLoading.set(false);
      }
    });
  }

  refreshToday(dateStr: string): void {
    this.loadToday(dateStr, true);
  }

  // ========== Cross-cutting mutations ==========

  /** Call after assigning/unassigning a lesson to refresh both schedule and today */
  onScheduleChanged(todayDateStr?: string): void {
    this.refreshSchedule();
    if (todayDateStr) this.refreshToday(todayDateStr);
  }

  /** Call after saving/deleting a plan to refresh plans list */
  onPlanChanged(): void {
    this.refreshPlans();
  }
}
