import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';

/**
 * Inline progress banner used for every long-running AI job in the app —
 * lesson plan generation, lesson content (re)generation, exercise generation /
 * retry / review. Renders nothing when `phase` is empty, so callers can drop
 * it inline without wrapping `*ngIf`.
 *
 * Visual language matches the Documents upload progress UI so users see one
 * consistent "work in progress" treatment across the app.
 */
@Component({
  selector: 'app-generation-banner',
  standalone: true,
  imports: [CommonModule, MatProgressBarModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div *ngIf="phase" class="generation-banner">
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
      <span class="generation-banner-text">
        <mat-icon>auto_awesome</mat-icon>
        <ng-container [ngSwitch]="phase">
          <span *ngSwitchCase="'queued'">Queued — {{ label }} will start shortly…</span>
          <span *ngSwitchCase="'generating'">Generating {{ label }} ({{ etaHint }})…</span>
          <span *ngSwitchDefault>Working…</span>
        </ng-container>
      </span>
    </div>
  `,
  styles: [`
    .generation-banner {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      padding: 1rem 1.25rem;
      margin-bottom: 1.5rem;
      border-radius: 12px;
      background-color: #eef2ff;
      border: 1px solid #c7d2fe;
    }
    .generation-banner mat-progress-bar {
      border-radius: 999px;
      --mdc-linear-progress-track-height: 6px;
      --mdc-linear-progress-active-indicator-height: 6px;
    }
    .generation-banner-text {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      color: #3730a3;
      font-weight: 500;
      font-size: 0.95rem;
    }
    .generation-banner-text mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
      color: #4338ca;
    }
  `],
})
export class GenerationBanner {
  /** When empty/null, the banner is hidden. */
  @Input() phase: 'queued' | 'generating' | '' | null = '';

  /** What's being generated. Used in the status text — e.g. "lesson plan", "exercise", "your answer review". */
  @Input() label = 'your content';

  /** Hint for typical duration. Shown only during the "generating" phase. */
  @Input() etaHint = 'this can take a while';
}
