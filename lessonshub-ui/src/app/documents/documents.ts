import { Component, OnInit, signal, inject, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Document } from '../models/document.model';
import { DocumentService } from '../services/document.service';
import { NotificationService } from '../services/notification.service';
import { ConfirmDialog } from '../confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatDialogModule,
  ],
  templateUrl: './documents.html',
  styleUrl: './documents.css',
})
export class Documents implements OnInit {
  documents = signal<Document[]>([]);
  isLoading = signal(true);
  uploading = signal<{ name: string; progress: number } | null>(null);
  error = signal('');

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  private docs = inject(DocumentService);
  private notify = inject(NotificationService);
  private dialog = inject(MatDialog);

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.isLoading.set(true);
    this.docs.list().subscribe({
      next: (data) => {
        this.documents.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load documents', err);
        this.error.set('Failed to load documents.');
        this.isLoading.set(false);
      },
    });
  }

  pickFile(): void {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // reset so the same file can be picked again
    if (!file) return;
    this.upload(file);
  }

  private upload(file: File): void {
    this.uploading.set({ name: file.name, progress: 0 });
    this.error.set('');
    this.docs.upload(file).subscribe({
      next: (event) => {
        this.uploading.set({ name: file.name, progress: event.progress });
        if (event.document) {
          // Final event — server returned the saved + ingested document.
          this.uploading.set(null);
          if (event.document.ingestionStatus === 'Failed') {
            this.notify.error(`Upload saved but ingestion failed: ${event.document.ingestionError ?? 'unknown error'}`);
          } else {
            this.notify.success(`"${event.document.name}" ingested (${event.document.chunkCount} chunks).`);
          }
          this.refresh();
        }
      },
      error: (err) => {
        console.error('Upload failed', err);
        this.uploading.set(null);
        const detail = err.error?.message ?? err.message ?? 'Upload failed.';
        this.error.set(detail);
        this.notify.error(detail);
      },
    });
  }

  delete(doc: Document): void {
    const ref = this.dialog.open(ConfirmDialog, {
      width: '420px',
      data: {
        title: 'Delete document?',
        message: `"${doc.name}" and its embeddings will be permanently removed. This cannot be undone.`,
        confirmText: 'Delete',
      },
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.docs.delete(doc.id).subscribe({
        next: () => {
          this.notify.success('Document deleted.');
          this.documents.update((list) => list.filter((d) => d.id !== doc.id));
        },
        error: (err) => {
          console.error('Delete failed', err);
          this.notify.error('Failed to delete document.');
        },
      });
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  statusIcon(status: Document['ingestionStatus']): string {
    switch (status) {
      case 'Ingested': return 'check_circle';
      case 'Failed':   return 'error';
      default:         return 'hourglass_empty';
    }
  }
}
