import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Lesson, Exercise, ExerciseAnswer, UpdateLessonInfo } from '../models/lesson.model';

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiUrl = 'api/lesson';

  constructor(private http: HttpClient) { }

  getLessonById(id: number): Observable<Lesson> {
    return this.http.get<Lesson>(`${this.apiUrl}/${id}`);
  }

  generateExercise(lessonId: number, difficulty: string, comment?: string): Observable<Exercise> {
    let url = `${this.apiUrl}/${lessonId}/generate-exercise?difficulty=${difficulty}`;
    if (comment) {
      url += `&comment=${encodeURIComponent(comment)}`;
    }
    return this.http.post<Exercise>(url, {});
  }

  retryExercise(lessonId: number, difficulty: string, review: string, comment?: string): Observable<Exercise> {
    let url = `${this.apiUrl}/${lessonId}/retry-exercise?difficulty=${encodeURIComponent(difficulty)}&review=${encodeURIComponent(review)}`;
    if (comment) {
      url += `&comment=${encodeURIComponent(comment)}`;
    }
    return this.http.post<Exercise>(url, {});
  }

  submitExerciseAnswer(exerciseId: number, answer: string): Observable<ExerciseAnswer> {
    return this.http.post<ExerciseAnswer>(`${this.apiUrl}/exercise/${exerciseId}/check`, JSON.stringify(answer), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  updateLesson(lessonId: number, info: UpdateLessonInfo): Observable<Lesson> {
    return this.http.put<Lesson>(`${this.apiUrl}/${lessonId}`, info);
  }

  regenerateContent(lessonId: number, bypassDocCache = false): Observable<Lesson> {
    const url = `${this.apiUrl}/${lessonId}/regenerate-content${bypassDocCache ? '?bypassDocCache=true' : ''}`;
    return this.http.post<Lesson>(url, {});
  }

  completeLesson(lessonId: number): Observable<Lesson> {
    return this.http.patch<Lesson>(`${this.apiUrl}/${lessonId}/complete`, {});
  }

  getSiblingLessonIds(lessonId: number): Observable<{ prevLessonId: number | null, nextLessonId: number | null }> {
    return this.http.get<{ prevLessonId: number | null, nextLessonId: number | null }>(`${this.apiUrl}/${lessonId}/siblings`);
  }
}