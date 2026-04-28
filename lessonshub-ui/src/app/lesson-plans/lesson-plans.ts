import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LessonDataStore } from '../services/lesson-data.store';

@Component({
  selector: 'app-lesson-plans',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule
  ],
  templateUrl: './lesson-plans.html',
  styleUrl: './lesson-plans.css'
})
export class LessonPlans implements OnInit {
  private store = inject(LessonDataStore);

  plans = this.store.plans;
  isLoading = this.store.plansLoading;

  sharedPlans = this.store.sharedPlans;
  sharedLoading = this.store.sharedPlansLoading;

  ngOnInit(): void {
    this.store.loadPlans();
    this.store.loadSharedPlans();
  }
}
