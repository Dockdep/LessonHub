import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { UserProfileService } from './user-profile.service';

describe('UserProfileService', () => {
  let service: UserProfileService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), UserProfileService]
    });
    service = TestBed.inject(UserProfileService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('GET /api/user/profile and returns the parsed profile', () => {
    let returned: unknown;
    service.getProfile().subscribe(p => (returned = p));

    const req = http.expectOne('/api/user/profile');
    expect(req.request.method).toBe('GET');
    req.flush({ email: 'a@b', name: 'A', googleApiKey: 'k' });

    expect(returned).toEqual({ email: 'a@b', name: 'A', googleApiKey: 'k' });
  });

  it('PUT /api/user/profile with the body and returns the updated profile', () => {
    let returned: unknown;
    service.updateProfile({ googleApiKey: 'new-key' }).subscribe(p => (returned = p));

    const req = http.expectOne('/api/user/profile');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ googleApiKey: 'new-key' });
    req.flush({ email: 'a@b', name: 'A', googleApiKey: 'new-key' });

    expect(returned).toMatchObject({ googleApiKey: 'new-key' });
  });
});
