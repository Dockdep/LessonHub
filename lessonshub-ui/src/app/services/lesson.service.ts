import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Lesson, Exercise, ExerciseAnswer, UpdateLessonInfo } from '../models/lesson.model';
import { JobEvent, JobStatus } from '../models/job.model';
import { JobsService } from './jobs.service';

@Injectable({ providedIn: 'root' })
export class LessonService {
  private apiUrl = 'api/lesson';
  private jobs = inject(JobsService);

  constructor(private http: HttpClient) { }

  // ---- Reads (sync) ----

  getLessonById(id: number): Observable<Lesson> {
    return this.http.get<Lesson>(`${this.apiUrl}/${id}`);
  }

  getSiblingLessonIds(lessonId: number): Observable<{ prevLessonId: number | null, nextLessonId: number | null }> {
    return this.http.get<{ prevLessonId: number | null, nextLessonId: number | null }>(`${this.apiUrl}/${lessonId}/siblings`);
  }

  // ---- Sync mutations (no AI) ----

  updateLesson(lessonId: number, info: UpdateLessonInfo): Observable<Lesson> {
    return this.http.put<Lesson>(`${this.apiUrl}/${lessonId}`, info);
  }

  completeLesson(lessonId: number): Observable<Lesson> {
    return this.http.patch<Lesson>(`${this.apiUrl}/${lessonId}/complete`, {});
  }

  // ---- Async (job-pipelined) ----

  /** Lazy content gen — call when getLessonById returns empty `content`. */
  generateContent(lessonId: number): Observable<JobEvent> {
    return this.jobs.postAndStream(`${this.apiUrl}/${lessonId}/generate-content`, {});
  }

  /** Owner-only forced regen, optionally bypassing the doc-search cache. */
  regenerateContent(lessonId: number, bypassDocCache = false): Observable<JobEvent> {
    const url = `${this.apiUrl}/${lessonId}/regenerate-content${bypassDocCache ? '?bypassDocCache=true' : ''}`;
    return this.jobs.postAndStream(url, {});
  }

  generateExercise(lessonId: number, difficulty: string, comment?: string): Observable<JobEvent> {
    let url = `${this.apiUrl}/${lessonId}/generate-exercise?difficulty=${encodeURIComponent(difficulty)}`;
    if (comment) url += `&comment=${encodeURIComponent(comment)}`;
    return this.jobs.postAndStream(url, {});
  }

  retryExercise(lessonId: number, difficulty: string, review: string, comment?: string): Observable<JobEvent> {
    let url = `${this.apiUrl}/${lessonId}/retry-exercise?difficulty=${encodeURIComponent(difficulty)}&review=${encodeURIComponent(review)}`;
    if (comment) url += `&comment=${encodeURIComponent(comment)}`;
    return this.jobs.postAndStream(url, {});
  }

  /**
   * Submit answer review. Uses postAndStream with a Content-Type header
   * since the body is a JSON string (the answer text), not an object —
   * ASP.NET expects `[FromBody] string`.
   */
  submitExerciseAnswer(exerciseId: number, answer: string): Observable<JobEvent> {
    const headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    return this.jobs.postAndStream(
      `${this.apiUrl}/exercise/${exerciseId}/check`,
      JSON.stringify(answer),
      { extraHeaders: headers },
    );
  }

  // ---- Result parsers ----

  parseLessonResult(event: JobEvent): Lesson | null {
    if (event.status !== JobStatus.Completed || !event.result) return null;
    return JSON.parse(event.result) as Lesson;
  }

  parseExerciseResult(event: JobEvent): Exercise | null {
    if (event.status !== JobStatus.Completed || !event.result) return null;
    return JSON.parse(event.result) as Exercise;
  }

  parseAnswerResult(event: JobEvent): ExerciseAnswer | null {
    if (event.status !== JobStatus.Completed || !event.result) return null;
    return JSON.parse(event.result) as ExerciseAnswer;
  }
}
