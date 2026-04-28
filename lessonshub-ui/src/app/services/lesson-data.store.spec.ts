import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { LessonDataStore } from './lesson-data.store';

describe('LessonDataStore', () => {
  let store: LessonDataStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), LessonDataStore]
    });
    store = TestBed.inject(LessonDataStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loadPlans only hits the API once if called twice without force', () => {
    store.loadPlans();
    const req1 = http.expectOne('/api/lessonday/plans');
    req1.flush([{ id: 1, name: 'P', topic: 't', description: '', createdDate: '', lessonsCount: 0, isOwner: true }]);

    store.loadPlans();           // should NOT trigger another HTTP call
    http.expectNone('/api/lessonday/plans');

    expect(store.plans().length).toBe(1);
    expect(store.plansLoaded()).toBe(true);
  });

  it('refreshPlans (force) re-fetches even after a prior load', () => {
    store.loadPlans();
    http.expectOne('/api/lessonday/plans').flush([]);

    store.refreshPlans();
    const req2 = http.expectOne('/api/lessonday/plans');
    req2.flush([{ id: 9, name: 'After', topic: 't', description: '', createdDate: '', lessonsCount: 0, isOwner: true }]);

    expect(store.plans()[0].name).toBe('After');
  });

  it('loadSharedPlans is independent of owned plans cache', () => {
    store.loadPlans();
    http.expectOne('/api/lessonday/plans').flush([{ id: 1, name: 'Mine', topic: 't', description: '', createdDate: '', lessonsCount: 0, isOwner: true }]);

    store.loadSharedPlans();
    http.expectOne('/api/lessonplan/shared-with-me').flush([
      { id: 2, name: 'Shared', topic: 't', description: '', createdDate: '', lessonsCount: 0, isOwner: false, ownerName: 'Owner' }
    ]);

    expect(store.plans()[0].name).toBe('Mine');
    expect(store.sharedPlans()[0].name).toBe('Shared');
    expect(store.sharedPlans()[0].isOwner).toBe(false);
  });
});
