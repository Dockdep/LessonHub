import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { DIFFICULTIES } from '../models/lesson.model';

export interface GenerateExerciseDialogData {
  review?: string;
}

export interface GenerateExerciseDialogResult {
  difficulty: string;
  comment?: string;
  review?: string;
}

@Component({
  selector: 'app-generate-exercise-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  template: `
    <h2 mat-dialog-title class="dialog-title">
      <mat-icon>{{ data.review ? 'replay' : 'auto_awesome' }}</mat-icon>
      {{ data.review ? 'Generate Task Based on Review' : 'Generate Task' }}
    </h2>
    <mat-dialog-content>
      <div class="dialog-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Difficulty</mat-label>
          <mat-select [formControl]="difficulty">
            <mat-option *ngFor="let d of difficulties" [value]="d">{{ d }}</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Comment (optional)</mat-label>
          <textarea matInput [formControl]="comment" rows="3" placeholder="e.g. Focus on past tense, include translation tasks..."></textarea>
        </mat-form-field>

        <div class="review-preview" *ngIf="data.review">
          <div class="review-label">Based on review:</div>
          <p class="review-text">{{ data.review }}</p>
        </div>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancel</button>
      <button mat-raised-button color="primary" (click)="onGenerate()">
        <mat-icon>{{ data.review ? 'replay' : 'auto_awesome' }}</mat-icon>
        Generate
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: 700;
    }
    .dialog-title mat-icon {
      color: #6d63c2;
    }
    .dialog-form {
      display: flex;
      flex-direction: column;
      min-width: 350px;
      padding-top: 1rem;
    }
    .full-width { width: 100%; }
    .review-preview {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 0.75rem;
      margin-top: 0.25rem;
    }
    .review-label {
      font-size: 0.7rem;
      font-weight: 700;
      text-transform: uppercase;
      color: #94a3b8;
      margin-bottom: 0.5rem;
    }
    .review-text {
      font-size: 0.85rem;
      color: #475569;
      margin: 0;
      max-height: 120px;
      overflow-y: auto;
      white-space: pre-wrap;
      line-height: 1.5;
    }
  `]
})
export class GenerateExerciseDialog {
  difficulty = new FormControl('Average');
  comment = new FormControl('');
  difficulties = DIFFICULTIES;

  constructor(
    public dialogRef: MatDialogRef<GenerateExerciseDialog>,
    @Inject(MAT_DIALOG_DATA) public data: GenerateExerciseDialogData
  ) {}

  onCancel(): void {
    this.dialogRef.close(null);
  }

  onGenerate(): void {
    const result: GenerateExerciseDialogResult = {
      difficulty: this.difficulty.value || 'Average',
      comment: this.comment.value?.trim() || undefined,
      review: this.data.review
    };
    this.dialogRef.close(result);
  }
}
