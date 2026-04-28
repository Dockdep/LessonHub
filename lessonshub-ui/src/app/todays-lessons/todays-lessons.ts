import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { RouterModule } from '@angular/router';
import { LessonDataStore } from '../services/lesson-data.store';

@Component({
  selector: 'app-todays-lessons',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatProgressBarModule,
    MatChipsModule,
    RouterModule
  ],
  templateUrl: './todays-lessons.html',
  styleUrl: './todays-lessons.css'
})
export class TodaysLessons implements OnInit {
  private store = inject(LessonDataStore);

  today = new Date();
  lessonDay = this.store.todayLessons;
  isLoading = this.store.todayLoading;

  greeting = computed(() => {
    const hour = this.today.getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  });

  ngOnInit(): void {
    this.store.loadToday(this.formatDateToYYYYMMDD(this.today));
  }

  loadTodaysLessons(): void {
    this.store.refreshToday(this.formatDateToYYYYMMDD(this.today));
  }

  formatDateToYYYYMMDD(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
