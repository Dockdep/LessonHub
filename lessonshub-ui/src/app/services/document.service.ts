import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType, HttpRequest } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Document } from '../models/document.model';

export interface UploadProgress {
  progress: number;          // 0..100, -1 if indeterminate
  document: Document | null; // populated only on the final event
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);
  private base = '/api/documents';

  list(): Observable<Document[]> {
    return this.http.get<Document[]>(this.base);
  }

  get(id: number): Observable<Document> {
    return this.http.get<Document>(`${this.base}/${id}`);
  }

  /**
   * Multipart upload. Emits progress events while the file uploads, then
   * the final event carries the saved Document.
   *
   * Note: server-side ingestion (chunk + embed) happens synchronously
   * after the upload bytes finish, so the user keeps waiting on the same
   * request until the document is "Ingested" (or "Failed").
   */
  upload(file: File): Observable<UploadProgress> {
    const form = new FormData();
    form.append('file', file, file.name);

    const req = new HttpRequest('POST', `${this.base}/upload`, form, {
      reportProgress: true,
    });

    return this.http.request<Document>(req).pipe(
      map((event: HttpEvent<Document>) => {
        if (event.type === HttpEventType.UploadProgress) {
          const progress = event.total ? Math.round((100 * event.loaded) / event.total) : -1;
          return { progress, document: null };
        }
        if (event.type === HttpEventType.Response) {
          return { progress: 100, document: event.body };
        }
        return { progress: -1, document: null };
      }),
    );
  }

  delete(id: number): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }
}
