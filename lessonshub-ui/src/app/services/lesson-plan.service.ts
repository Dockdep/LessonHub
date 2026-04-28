import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, switchMap, takeWhile, throwError } from 'rxjs';
import { LessonPlanRequest, LessonPlanResponse } from '../models/lesson-plan.model';
import { JobAcceptedResponse, JobEvent, JobStatus } from '../models/job.model';
import { RealtimeService } from './realtime.service';

@Injectable({
  providedIn: 'root'
})
export class LessonPlanService {
  private apiUrl = '/api/lessonplan';
  private realtime = inject(RealtimeService);

  constructor(private http: HttpClient) { }

  /**
   * Generate a plan via the job pipeline.
   *
   * Flow:
   *   1. POST returns 202 + jobId.
   *   2. Subscribe to the SignalR hub for that jobId.
   *   3. Emit JobEvents to the caller — caller can show progress UI.
   *   4. On Completed, parse Job.result (JSON) into LessonPlanResponse and
   *      pass downstream as a final event.
   *   5. On Failed, throw with the job's error message.
   *
   * Returns Observable<JobEvent> so the component can render phased status
   * (queued, generating, done, error). The completed event's `result` field
   * holds the LessonPlanResponse JSON.
   */
  generateLessonPlan(request: LessonPlanRequest): Observable<JobEvent> {
    // Per-click idempotency key. Same click hitting the server twice (e.g.
    // resilience retry, network blip) will be coalesced server-side.
    const idempotencyKey = crypto.randomUUID();
    const headers = new HttpHeaders({ 'X-Idempotency-Key': idempotencyKey });

    return this.http
      .post<JobAcceptedResponse>(`${this.apiUrl}/generate`, request, { headers })
      .pipe(
        switchMap((accepted) => this.realtime.subscribe(accepted.jobId)),
        // Stop the stream once the job reaches a terminal state.
        takeWhile((e) => e.status !== JobStatus.Completed && e.status !== JobStatus.Failed, true),
        switchMap((e) => {
          if (e.status === JobStatus.Failed) {
            return throwError(() => new Error(e.error ?? 'Lesson plan generation failed.'));
          }
          return [e];
        }),
      );
  }

  /** Convenience: extracts the LessonPlanResponse from a Completed event's `result`. */
  parsePlanResult(event: JobEvent): LessonPlanResponse | null {
    if (event.status !== JobStatus.Completed || !event.result) return null;
    return JSON.parse(event.result) as LessonPlanResponse;
  }

  saveLessonPlan(
    lessonPlan: LessonPlanResponse,
    description: string,
    lessonType: string,
    nativeLanguage?: string,
    documentId?: number | null,
    languageToLearn?: string,
    useNativeLanguage?: boolean,
  ): Observable<any> {
    const request = {
      lessonPlan: lessonPlan,
      description: description,
      lessonType: lessonType,
      nativeLanguage: nativeLanguage || null,
      languageToLearn: languageToLearn || null,
      // Default to true so non-Language plans land with the right flag value.
      useNativeLanguage: useNativeLanguage ?? true,
      documentId: documentId ?? null,
    };
    return this.http.post(`${this.apiUrl}/save`, request);
  }
}
