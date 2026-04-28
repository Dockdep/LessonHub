import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType, HttpHeaders, HttpRequest } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Document } from '../models/document.model';
import { JobEvent } from '../models/job.model';
import { RealtimeService } from './realtime.service';

export interface UploadProgress {
  progress: number;          // 0..100, -1 if indeterminate
  document: Document | null; // populated only on the final event
  jobId: string | null;      // ingest job id, returned with the final event
}

interface UploadAcceptedResponse {
  document: Document;
  jobId: string;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);
  private realtime = inject(RealtimeService);
  private base = '/api/documents';

  list(): Observable<Document[]> {
    return this.http.get<Document[]>(this.base);
  }

  get(id: number): Observable<Document> {
    return this.http.get<Document>(`${this.base}/${id}`);
  }

  /**
   * Multipart upload. Emits progress while bytes upload; the final event
   * carries the saved Document (status="Pending") AND the jobId for the
   * background RAG ingest. Callers subscribe to `subscribeToIngest(jobId)`
   * to learn when the doc transitions to Ingested or Failed.
   */
  upload(file: File): Observable<UploadProgress> {
    const form = new FormData();
    form.append('file', file, file.name);

    const idempotencyKey = crypto.randomUUID();
    const headers = new HttpHeaders({ 'X-Idempotency-Key': idempotencyKey });

    const req = new HttpRequest('POST', `${this.base}/upload`, form, {
      reportProgress: true,
      headers,
    });

    return this.http.request<UploadAcceptedResponse>(req).pipe(
      map((event: HttpEvent<UploadAcceptedResponse>) => {
        if (event.type === HttpEventType.UploadProgress) {
          const progress = event.total ? Math.round((100 * event.loaded) / event.total) : -1;
          return { progress, document: null, jobId: null };
        }
        if (event.type === HttpEventType.Response) {
          const body = event.body;
          return {
            progress: 100,
            document: body?.document ?? null,
            jobId: body?.jobId ?? null,
          };
        }
        return { progress: -1, document: null, jobId: null };
      }),
    );
  }

  /**
   * Stream JobEvents for a document-ingest job. Component renders status
   * transitions (Pending → Running → Ingested/Failed) without polling.
   */
  subscribeToIngest(jobId: string): Observable<JobEvent> {
    return this.realtime.subscribe(jobId);
  }

  delete(id: number): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }
}
