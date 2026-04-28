import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LessonPlanRequest, LessonPlanResponse } from '../models/lesson-plan.model';

@Injectable({
  providedIn: 'root'
})
export class LessonPlanService {
  private apiUrl = '/api/lessonplan';

  constructor(private http: HttpClient) { }

  generateLessonPlan(request: LessonPlanRequest): Observable<LessonPlanResponse> {
    return this.http.post<LessonPlanResponse>(`${this.apiUrl}/generate`, request);
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
