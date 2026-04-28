import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, defer, map, of, switchMap, takeWhile, throwError } from 'rxjs';
import { JobDto, JobEvent, JobStatus } from '../models/job.model';
import { RealtimeService } from './realtime.service';

interface JobAcceptedResponse {
  jobId: string;
}

/**
 * Optional `postAndStream` knobs. Most callers won't need any of these.
 */
export interface PostAndStreamOptions {
  /** Merged onto the default `X-Idempotency-Key` header. */
  extraHeaders?: HttpHeaders;
  /** Skip auto-generating the `X-Idempotency-Key` (e.g. caller supplies its own). */
  skipIdempotencyKey?: boolean;
}

/**
 * Single client for the `/api/jobs/*` surface. Two responsibilities:
 *
 *   1. **Lookup** — `findInFlight` / `listInFlightForEntity` so pages can
 *      restore banners + auto-resume on revisit.
 *   2. **Posting + streaming** — `postAndStream` collapses the per-service
 *      boilerplate (idempotency key + POST + subscribe via SignalR + filter
 *      on terminal status + throw on failure) into one call.
 *
 * Use `postAndStream` for any endpoint that returns `202 { jobId }`. Use
 * `subscribeToExistingJob` to resume tracking by id (handles already-terminal
 * race). Use the find/list methods on page load to discover jobs the user
 * left running on a previous visit.
 */
@Injectable({ providedIn: 'root' })
export class JobsService {
  private http = inject(HttpClient);
  private realtime = inject(RealtimeService);

  // -- Lookup --------------------------------------------------------------

  get(jobId: string): Observable<JobDto> {
    return this.http.get<JobDto>(`/api/jobs/${jobId}`);
  }

  findInFlight(
    type: string,
    relatedEntityType?: string,
    relatedEntityId?: number,
  ): Observable<JobDto | null> {
    let params = new HttpParams().set('type', type);
    if (relatedEntityType) params = params.set('relatedEntityType', relatedEntityType);
    if (relatedEntityId !== undefined) params = params.set('relatedEntityId', String(relatedEntityId));
    return this.http.get<JobDto | null>('/api/jobs/in-flight', { params });
  }

  /**
   * Every in-flight job tied to one entity. Used by detail pages to restore
   * all relevant banners on a single load instead of N queries by type.
   */
  listInFlightForEntity(relatedEntityType: string, relatedEntityId: number): Observable<JobDto[]> {
    const params = new HttpParams()
      .set('relatedEntityType', relatedEntityType)
      .set('relatedEntityId', String(relatedEntityId));
    return this.http.get<JobDto[]>('/api/jobs/in-flight-for-entity', { params });
  }

  // -- Streaming -----------------------------------------------------------

  /**
   * POST a body to the given endpoint, expect `202 { jobId }`, and return
   * the SignalR event stream for that job. Stream emits one event per
   * status transition and completes on terminal (Completed/Failed). On
   * Failed it errors with the server's message so callers can `.subscribe({
   * next, error })` the same way they do for any HTTP call.
   */
  postAndStream<TBody>(
    url: string,
    body: TBody,
    options: PostAndStreamOptions = {},
  ): Observable<JobEvent> {
    const headers = this.buildHeaders(options);
    const post$ = this.http
      .post<JobAcceptedResponse>(url, body, { headers })
      .pipe(map((accepted) => accepted.jobId));
    return post$.pipe(switchMap((jobId) => this.subscribeToExistingJob(jobId)));
  }

  /**
   * Subscribe to events for a known jobId. Polls once first to avoid
   * missing the result if the executor finished between page load and the
   * SignalR connection opening — emits a synthetic Completed/Failed event
   * in that case.
   */
  subscribeToExistingJob(jobId: string): Observable<JobEvent> {
    return defer(() => this.get(jobId)).pipe(
      switchMap((job) => {
        if (job.status === JobStatus.Completed || job.status === JobStatus.Failed) {
          const synthetic: JobEvent = {
            id: job.id,
            type: job.type,
            status: job.status,
            result: job.result,
            error: job.error,
            relatedEntityType: job.relatedEntityType,
            relatedEntityId: job.relatedEntityId,
            timestamp: new Date().toISOString(),
          };
          return of(synthetic);
        }
        return this.realtime.subscribe(jobId);
      }),
      // Stop after the first terminal event. `inclusive=true` keeps that
      // event in the stream so callers see it.
      takeWhile((e) => e.status !== JobStatus.Completed && e.status !== JobStatus.Failed, true),
      switchMap((e) =>
        e.status === JobStatus.Failed
          ? throwError(() => new Error(e.error ?? 'Job failed.'))
          : of(e),
      ),
    );
  }

  // -- Internals -----------------------------------------------------------

  private buildHeaders(options: PostAndStreamOptions): HttpHeaders {
    let headers = options.extraHeaders ?? new HttpHeaders();
    if (!options.skipIdempotencyKey && !headers.has('X-Idempotency-Key')) {
      headers = headers.set('X-Idempotency-Key', crypto.randomUUID());
    }
    return headers;
  }
}
