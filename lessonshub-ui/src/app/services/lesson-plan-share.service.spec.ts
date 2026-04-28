import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { LessonPlanShareService } from './lesson-plan-share.service';

describe('LessonPlanShareService', () => {
  let service: LessonPlanShareService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), LessonPlanShareService]
    });
    service = TestBed.inject(LessonPlanShareService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getSharedWithMe hits /api/lessonplan/shared-with-me', () => {
    service.getSharedWithMe().subscribe();
    const req = http.expectOne('/api/lessonplan/shared-with-me');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getShares hits /api/lessonplan/{planId}/shares', () => {
    service.getShares(42).subscribe();
    const req = http.expectOne('/api/lessonplan/42/shares');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('addShare POSTs the email body', () => {
    service.addShare(42, { email: 'x@y.com' }).subscribe();
    const req = http.expectOne('/api/lessonplan/42/shares');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'x@y.com' });
    req.flush({ id: 1, userId: 5, email: 'x@y.com', name: 'X', sharedAt: '2026-01-01' });
  });

  it('removeShare DELETEs by userId', () => {
    service.removeShare(42, 5).subscribe();
    const req = http.expectOne('/api/lessonplan/42/shares/5');
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });
});
