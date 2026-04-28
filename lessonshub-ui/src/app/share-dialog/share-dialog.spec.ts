import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ShareDialog } from './share-dialog';

describe('ShareDialog component', () => {
  let http: HttpTestingController;
  const closeSpy = vi.fn();

  beforeEach(() => {
    closeSpy.mockReset();
    TestBed.configureTestingModule({
      imports: [ShareDialog],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: { close: closeSpy } },
        { provide: MAT_DIALOG_DATA, useValue: { planId: 99, planName: 'My Plan' } }
      ]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads existing shares on init', () => {
    const fixture = TestBed.createComponent(ShareDialog);
    fixture.detectChanges();

    const req = http.expectOne('/api/lessonplan/99/shares');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 1, userId: 5, email: 'a@b', name: 'A', sharedAt: '2026-01-01' }]);

    expect(fixture.componentInstance.shares()).toHaveLength(1);
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('add() POSTs the email, appends the new share, clears input', () => {
    const fixture = TestBed.createComponent(ShareDialog);
    fixture.detectChanges();
    http.expectOne('/api/lessonplan/99/shares').flush([]);

    fixture.componentInstance.email = 'new@user.com';
    fixture.componentInstance.add();

    const req = http.expectOne('/api/lessonplan/99/shares');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'new@user.com' });
    req.flush({ id: 7, userId: 12, email: 'new@user.com', name: 'New', sharedAt: '2026-04-19' });

    expect(fixture.componentInstance.shares()).toHaveLength(1);
    expect(fixture.componentInstance.shares()[0].email).toBe('new@user.com');
    expect(fixture.componentInstance.email).toBe('');
  });

  it('add() surfaces backend error message in errorMessage signal', () => {
    const fixture = TestBed.createComponent(ShareDialog);
    fixture.detectChanges();
    http.expectOne('/api/lessonplan/99/shares').flush([]);

    fixture.componentInstance.email = 'ghost@nowhere.com';
    fixture.componentInstance.add();

    const req = http.expectOne('/api/lessonplan/99/shares');
    req.flush({ message: 'No user found with that email.' }, { status: 404, statusText: 'Not Found' });

    expect(fixture.componentInstance.errorMessage()).toBe('No user found with that email.');
    expect(fixture.componentInstance.shares()).toHaveLength(0);
  });

  it('remove() DELETEs and removes the share from the list', () => {
    const fixture = TestBed.createComponent(ShareDialog);
    fixture.detectChanges();
    http.expectOne('/api/lessonplan/99/shares').flush([
      { id: 1, userId: 5, email: 'a@b', name: 'A', sharedAt: '2026-01-01' },
      { id: 2, userId: 6, email: 'c@d', name: 'C', sharedAt: '2026-01-02' }
    ]);

    fixture.componentInstance.remove({ id: 1, userId: 5, email: 'a@b', name: 'A', sharedAt: '2026-01-01' });

    const req = http.expectOne('/api/lessonplan/99/shares/5');
    expect(req.request.method).toBe('DELETE');
    req.flush({});

    expect(fixture.componentInstance.shares()).toHaveLength(1);
    expect(fixture.componentInstance.shares()[0].userId).toBe(6);
  });

  it('close() invokes dialogRef.close', () => {
    const fixture = TestBed.createComponent(ShareDialog);
    fixture.detectChanges();
    http.expectOne('/api/lessonplan/99/shares').flush([]);

    fixture.componentInstance.close();
    expect(closeSpy).toHaveBeenCalled();
  });
});
