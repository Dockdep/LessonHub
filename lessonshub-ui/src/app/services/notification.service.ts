import { Injectable, signal } from '@angular/core';

export interface AppNotification {
  message: string;
  type: 'success' | 'error';
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private _notification = signal<AppNotification | null>(null);
  readonly notification = this._notification.asReadonly();

  success(message: string): void {
    this._notification.set({ message, type: 'success' });
    setTimeout(() => this.clear(), 4000);
  }

  error(message: string): void {
    this._notification.set({ message, type: 'error' });
    setTimeout(() => this.clear(), 6000);
  }

  clear(): void {
    this._notification.set(null);
  }
}
