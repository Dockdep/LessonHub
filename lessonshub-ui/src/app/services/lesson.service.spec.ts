import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { LessonService } from './lesson.service';

describe('LessonService.regenerateContent', () => {
  let service: LessonService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), LessonService]
    });
    service = TestBed.inject(LessonService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('omits the bypassDocCache query param when false (default)', () => {
    service.regenerateContent(7).subscribe();
    const req = http.expectOne('api/lesson/7/regenerate-content');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('appends bypassDocCache=true when requested', () => {
    service.regenerateContent(7, true).subscribe();
    const req = http.expectOne('api/lesson/7/regenerate-content?bypassDocCache=true');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
