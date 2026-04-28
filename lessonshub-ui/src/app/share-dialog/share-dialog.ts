import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LessonPlanShareService } from '../services/lesson-plan-share.service';
import { LessonPlanShareItem } from '../models/lesson-day.model';
import { NotificationService } from '../services/notification.service';

export interface ShareDialogData {
  planId: number;
  planName: string;
}

@Component({
  selector: 'app-share-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './share-dialog.html',
  styleUrl: './share-dialog.css'
})
export class ShareDialog implements OnInit {
  private shareService = inject(LessonPlanShareService);
  private notify = inject(NotificationService);

  shares = signal<LessonPlanShareItem[]>([]);
  loading = signal(true);
  adding = signal(false);
  removingUserId = signal<number | null>(null);

  email = '';
  errorMessage = signal('');

  constructor(
    public dialogRef: MatDialogRef<ShareDialog>,
    @Inject(MAT_DIALOG_DATA) public data: ShareDialogData
  ) {}

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.shareService.getShares(this.data.planId).subscribe({
      next: (data) => {
        this.shares.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.notify.error('Failed to load shares.');
        this.loading.set(false);
      }
    });
  }

  add(): void {
    const email = this.email.trim();
    if (!email) return;

    this.adding.set(true);
    this.errorMessage.set('');
    this.shareService.addShare(this.data.planId, { email }).subscribe({
      next: (share) => {
        this.shares.update(list => [...list, share].sort((a, b) => a.email.localeCompare(b.email)));
        this.email = '';
        this.adding.set(false);
        this.notify.success(`Shared with ${share.email}`);
      },
      error: (err) => {
        this.adding.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Failed to share.');
      }
    });
  }

  remove(share: LessonPlanShareItem): void {
    this.removingUserId.set(share.userId);
    this.shareService.removeShare(this.data.planId, share.userId).subscribe({
      next: () => {
        this.shares.update(list => list.filter(s => s.userId !== share.userId));
        this.removingUserId.set(null);
        this.notify.success(`Removed access for ${share.email}`);
      },
      error: () => {
        this.removingUserId.set(null);
        this.notify.error('Failed to remove share.');
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
