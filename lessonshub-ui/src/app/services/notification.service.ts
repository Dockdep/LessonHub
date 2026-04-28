import { Injectable, computed, signal } from '@angular/core';

export interface AppNotification {
  id: number;
  message: string;
  type: 'success' | 'error';
  createdAt: Date;
  read: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private static MAX_HISTORY = 20;
  private nextId = 1;

  /** The currently-visible toast (auto-dismissing). At most one at a time. */
  private _current = signal<AppNotification | null>(null);
  readonly notification = this._current.asReadonly();

  /** Recent notification history (most-recent first). Capped at 20 entries. */
  private _history = signal<AppNotification[]>([]);
  readonly history = this._history.asReadonly();

  /** Count of unread notifications — drives the badge on the bell icon. */
  readonly unreadCount = computed(() => this._history().filter((n) => !n.read).length);

  success(message: string): void {
    this.push({ message, type: 'success' });
    setTimeout(() => this.clear(), 4000);
  }

  error(message: string): void {
    this.push({ message, type: 'error' });
    setTimeout(() => this.clear(), 6000);
  }

  /** Clears the current visible toast (does NOT touch history). */
  clear(): void {
    this._current.set(null);
  }

  /** Mark all notifications as read — call when the user opens the dropdown. */
  markAllRead(): void {
    this._history.update((items) => items.map((n) => (n.read ? n : { ...n, read: true })));
  }

  /** Reset history (e.g. on logout). */
  clearHistory(): void {
    this._history.set([]);
    this._current.set(null);
  }

  private push(seed: Omit<AppNotification, 'id' | 'createdAt' | 'read'>): void {
    const item: AppNotification = {
      id: this.nextId++,
      createdAt: new Date(),
      read: false,
      ...seed,
    };
    this._current.set(item);
    this._history.update((items) => [item, ...items].slice(0, NotificationService.MAX_HISTORY));
  }
}
