import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login').then(c => c.Login)
  },
  {
    path: '',
    redirectTo: '/today',
    pathMatch: 'full'
  },
  {
    path: 'today',
    loadComponent: () => import('./todays-lessons/todays-lessons').then(c => c.TodaysLessons),
    canActivate: [authGuard]
  },
  {
    path: 'lesson/:id',
    loadComponent: () => import('./lesson-detail/lesson-detail').then(c => c.LessonDetail),
    canActivate: [authGuard]
  },
  {
    path: 'lesson-plan',
    loadComponent: () => import('./lesson-plan/lesson-plan').then(c => c.LessonPlan),
    canActivate: [authGuard]
  },
  {
    path: 'lesson-plans',
    loadComponent: () => import('./lesson-plans/lesson-plans').then(c => c.LessonPlans),
    canActivate: [authGuard]
  },
  {
    path: 'lesson-plans/:id',
    loadComponent: () => import('./lesson-plan-detail/lesson-plan-detail').then(c => c.LessonPlanDetail),
    canActivate: [authGuard]
  },
  {
    path: 'lesson-days',
    loadComponent: () => import('./lesson-days/lesson-days').then(c => c.LessonDays),
    canActivate: [authGuard]
  },
  {
    path: 'documents',
    loadComponent: () => import('./documents/documents').then(c => c.Documents),
    canActivate: [authGuard]
  },
  {
    path: 'profile',
    loadComponent: () => import('./profile/profile').then(c => c.Profile),
    canActivate: [authGuard]
  }
];