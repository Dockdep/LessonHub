import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, switchMap, takeWhile, throwError } from 'rxjs';
import { Lesson, Exercise, ExerciseAnswer, UpdateLessonInfo } from '../models/lesson.model';
import { JobAcceptedResponse, JobEvent, JobStatus } from '../models/job.model';
import { RealtimeService } from './realtime.service';

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiUrl = 'api/lesson';
  private realtime = inject(RealtimeService);

  constructor(private http: HttpClient) { }

  getLessonById(id: number): Observable<Lesson> {
    return this.http.get<Lesson>(`${this.apiUrl}/${id}`);
  }

  /**
   * Lazy content gen — call this when getLessonById returns a lesson with
   * empty Content. Streams JobEvents until terminal; component renders the
   * final Lesson DTO from event.result on Completed.
   */
  generateContent(lessonId: number): Observable<JobEvent> {
    return this.enqueueAndSubscribe(`${this.apiUrl}/${lessonId}/generate-content`);
  }

  /** Owner-only regen. */
  regenerateContent(lessonId: number, bypassDocCache = false): Observable<JobEvent> {
    const url = `${this.apiUrl}/${lessonId}/regenerate-content${bypassDocCache ? '?bypassDocCache=true' : ''}`;
    return this.enqueueAndSubscribe(url);
  }

  generateExercise(lessonId: number, difficulty: string, comment?: string): Observable<JobEvent> {
    let url = `${this.apiUrl}/${lessonId}/generate-exercise?difficulty=${encodeURIComponent(difficulty)}`;
    if (comment) url += `&comment=${encodeURIComponent(comment)}`;
    return this.enqueueAndSubscribe(url);
  }

  retryExercise(lessonId: number, difficulty: string, review: string, comment?: string): Observable<JobEvent> {
    let url = `${this.apiUrl}/${lessonId}/retry-exercise?difficulty=${encodeURIComponent(difficulty)}&review=${encodeURIComponent(review)}`;
    if (comment) url += `&comment=${encodeURIComponent(comment)}`;
    return this.enqueueAndSubscribe(url);
  }

  submitExerciseAnswer(exerciseId: number, answer: string): Observable<JobEvent> {
    const url = `${this.apiUrl}/exercise/${exerciseId}/check`;
    const idempotencyKey = crypto.randomUUID();
    const headers = new HttpHeaders({
      'X-Idempotency-Key': idempotencyKey,
      'Content-Type': 'application/json',
    });
    return this.http
      .post<JobAcceptedResponse>(url, JSON.stringify(answer), { headers })
      .pipe(
        switchMap((accepted) => this.realtime.subscribe(accepted.jobId)),
        takeWhile((e) => e.status !== JobStatus.Completed && e.status !== JobStatus.Failed, true),
        switchMap((e) => {
          if (e.status === JobStatus.Failed) {
            return throwError(() => new Error(e.error ?? 'Answer review failed.'));
          }
          return [e];
        }),
      );
  }

  // ---- Sync endpoints (no AI work) — unchanged ----

  updateLesson(lessonId: number, info: UpdateLessonInfo): Observable<Lesson> {
    return this.http.put<Lesson>(`${this.apiUrl}/${lessonId}`, info);
  }

  completeLesson(lessonId: number): Observable<Lesson> {
    return this.http.patch<Lesson>(`${this.apiUrl}/${lessonId}/complete`, {});
  }

  getSiblingLessonIds(lessonId: number): Observable<{ prevLessonId: number | null, nextLessonId: number | null }> {
    return this.http.get<{ prevLessonId: number | null, nextLessonId: number | null }>(`${this.apiUrl}/${lessonId}/siblings`);
  }

  // ---- Helpers for callers ----

  /** Parse a Completed event's `result` into a Lesson DTO. */
  parseLessonResult(event: JobEvent): Lesson | null {
    if (event.status !== JobStatus.Completed || !event.result) return null;
    return JSON.parse(event.result) as Lesson;
  }

  /** Parse a Completed event's `result` into an Exercise DTO. */
  parseExerciseResult(event: JobEvent): Exercise | null {
    if (event.status !== JobStatus.Completed || !event.result) return null;
    return JSON.parse(event.result) as Exercise;
  }

  /** Parse a Completed event's `result` into an ExerciseAnswer DTO. */
  parseAnswerResult(event: JobEvent): ExerciseAnswer | null {
    if (event.status !== JobStatus.Completed || !event.result) return null;
    return JSON.parse(event.result) as ExerciseAnswer;
  }

  // ---- Private ----

  private enqueueAndSubscribe(url: string): Observable<JobEvent> {
    const idempotencyKey = crypto.randomUUID();
    const headers = new HttpHeaders({ 'X-Idempotency-Key': idempotencyKey });
    return this.http
      .post<JobAcceptedResponse>(url, {}, { headers })
      .pipe(
        switchMap((accepted) => this.realtime.subscribe(accepted.jobId)),
        takeWhile((e) => e.status !== JobStatus.Completed && e.status !== JobStatus.Failed, true),
        switchMap((e) => {
          if (e.status === JobStatus.Failed) {
            return throwError(() => new Error(e.error ?? 'Job failed.'));
          }
          return [e];
        }),
      );
  }
}
