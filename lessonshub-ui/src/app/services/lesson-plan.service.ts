import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LessonPlanRequest, LessonPlanResponse } from '../models/lesson-plan.model';
import { JobEvent, JobStatus } from '../models/job.model';
import { JobsService } from './jobs.service';

@Injectable({ providedIn: 'root' })
export class LessonPlanService {
  private apiUrl = '/api/lessonplan';
  private jobs = inject(JobsService);

  constructor(private http: HttpClient) { }

  /**
   * Generate a plan via the SignalR job pipeline. Stream emits one event per
   * status transition; on Completed `event.result` holds the JSON-serialized
   * LessonPlanResponse — use {@link parsePlanResult} to deserialize.
   */
  generateLessonPlan(request: LessonPlanRequest): Observable<JobEvent> {
    return this.jobs.postAndStream(`${this.apiUrl}/generate`, request);
  }

  /** Extracts the LessonPlanResponse from a Completed event's `result`. */
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
