import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LessonDay, LessonPlanSummary, LessonPlanDetail, AvailableLesson, AssignLessonRequest, UpdateLessonPlanRequest } from '../models/lesson-day.model';

@Injectable({
  providedIn: 'root'
})
export class LessonDayService {
  private apiUrl = '/api/lessonday';

  constructor(private http: HttpClient) { }

  getLessonPlans(): Observable<LessonPlanSummary[]> {
    return this.http.get<LessonPlanSummary[]>(`${this.apiUrl}/plans`);
  }

  getLessonPlanDetail(id: number): Observable<LessonPlanDetail> {
    return this.http.get<LessonPlanDetail>(`/api/lessonplan/${id}`);
  }

  deleteLessonPlan(id: number): Observable<any> {
    return this.http.delete(`/api/lessonplan/${id}`);
  }

  updateLessonPlan(id: number, request: UpdateLessonPlanRequest): Observable<LessonPlanDetail> {
    return this.http.put<LessonPlanDetail>(`/api/lessonplan/${id}`, request);
  }

  getAvailableLessons(lessonPlanId: number): Observable<AvailableLesson[]> {
    return this.http.get<AvailableLesson[]>(`${this.apiUrl}/plans/${lessonPlanId}/lessons`);
  }

  getLessonDaysByMonth(year: number, month: number): Observable<LessonDay[]> {
    return this.http.get<LessonDay[]>(`${this.apiUrl}/${year}/${month}`);
  }

  assignLesson(request: AssignLessonRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/assign`, request);
  }

  unassignLesson(lessonId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/unassign/${lessonId}`);
  }

  getLessonDayByDate(date: string): Observable<LessonDay | null> {
  // Format date as yyyy-MM-dd for the API
  return this.http.get<LessonDay | null>(`${this.apiUrl}/date/${date}`);
}
}
