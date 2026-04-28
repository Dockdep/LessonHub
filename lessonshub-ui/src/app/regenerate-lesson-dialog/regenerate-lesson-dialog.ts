import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface RegenerateLessonDialogData {
  isTechnical: boolean;
}

export interface RegenerateLessonDialogResult {
  bypassDocCache: boolean;
}

@Component({
  selector: 'app-regenerate-lesson-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './regenerate-lesson-dialog.html',
  styleUrl: './regenerate-lesson-dialog.css'
})
export class RegenerateLessonDialog {
  bypassDocCache = false;

  constructor(
    public dialogRef: MatDialogRef<RegenerateLessonDialog, RegenerateLessonDialogResult | null>,
    @Inject(MAT_DIALOG_DATA) public data: RegenerateLessonDialogData
  ) {}

  cancel(): void {
    this.dialogRef.close(null);
  }

  confirm(): void {
    this.dialogRef.close({
      bypassDocCache: this.data.isTechnical && this.bypassDocCache
    });
  }
}
