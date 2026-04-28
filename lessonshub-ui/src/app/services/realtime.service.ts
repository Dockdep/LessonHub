import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Observable, Subject, filter } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';
import { JobEvent } from '../models/job.model';
import { AuthService } from './auth.service';

/**
 * Manages a single SignalR connection to /hubs/generation. Lazily connects on
 * first subscribe; reconnects automatically on transient drops; multiplexes
 * incoming JobUpdated events into per-jobId Observables.
 *
 * SSR-safe: the connection only opens in the browser. SSR `subscribe()` calls
 * complete immediately (no events) so server-side renders don't block.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private connection?: HubConnection;
  private startPromise?: Promise<void>;
  private readonly events$ = new Subject<JobEvent>();

  constructor(
    @Inject(PLATFORM_ID) private platformId: object,
    @Inject(API_BASE_URL) private apiBaseUrl: string,
    private auth: AuthService,
  ) {}

  /**
   * Returns events for a single job. Filters the shared event stream so each
   * caller only gets transitions for the job they care about.
   */
  subscribe(jobId: string): Observable<JobEvent> {
    return new Observable<JobEvent>((observer) => {
      this.ensureConnection().catch((err) => observer.error(err));
      const sub = this.events$
        .pipe(filter((e) => e.id === jobId))
        .subscribe(observer);
      return () => sub.unsubscribe();
    });
  }

  private ensureConnection(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return Promise.resolve();
    if (this.startPromise) return this.startPromise;

    const url = `${this.apiBaseUrl}/hubs/generation`;
    this.connection = new HubConnectionBuilder()
      .withUrl(url, {
        // SignalR adds ?access_token=... on the WS handshake — the .NET side
        // accepts that for /hubs/* paths only (see Program.cs JwtBearerEvents).
        // Source the token from AuthService so we stay in sync with whatever
        // localStorage key it uses (currently `auth_token`).
        accessTokenFactory: () => this.auth.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('JobUpdated', (event: JobEvent) => this.events$.next(event));

    // If the bearer token rotates while the connection is open, the next
    // reconnect cycle will pick up the new value via accessTokenFactory.
    this.startPromise = this.connection.start();
    return this.startPromise;
  }

  /** Visible-for-test/diagnostics; prod code should not need this. */
  get state(): HubConnectionState | 'NotConnected' {
    return this.connection?.state ?? 'NotConnected';
  }
}
