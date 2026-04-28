import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LessonPlanShareItem, LessonPlanSummary, AddShareRequest } from '../models/lesson-day.model';

@Injectable({ providedIn: 'root' })
export class LessonPlanShareService {
  private http = inject(HttpClient);
  private base = '/api/lessonplan';

  getSharedWithMe(): Observable<LessonPlanSummary[]> {
    return this.http.get<LessonPlanSummary[]>(`${this.base}/shared-with-me`);
  }

  getShares(planId: number): Observable<LessonPlanShareItem[]> {
    return this.http.get<LessonPlanShareItem[]>(`${this.base}/${planId}/shares`);
  }

  addShare(planId: number, request: AddShareRequest): Observable<LessonPlanShareItem> {
    return this.http.post<LessonPlanShareItem>(`${this.base}/${planId}/shares`, request);
  }

  removeShare(planId: number, userId: number): Observable<unknown> {
    return this.http.delete(`${this.base}/${planId}/shares/${userId}`);
  }
}
