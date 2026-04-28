import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

function makeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' })).replace(/=+$/, '');
  const body = btoa(JSON.stringify(payload)).replace(/=+$/, '');
  return `${header}.${body}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerNavigate: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    localStorage.clear();
    routerNavigate = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: { navigate: routerNavigate } }
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('starts logged out when there is no stored token', () => {
    expect(service.isLoggedIn()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('loginWithGoogle posts idToken, stores returned token, and decodes the user', async () => {
    const futureExp = Math.floor(Date.now() / 1000) + 3600;
    const token = makeJwt({ sub: '7', email: 'x@y.com', name: 'X Y', exp: futureExp });

    const promise = service.loginWithGoogle('google-id-token');

    const req = httpMock.expectOne('/api/auth/google');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ idToken: 'google-id-token' });
    req.flush({ token, user: { id: 7, email: 'x@y.com', name: 'X Y' } });

    await promise;

    expect(localStorage.getItem('auth_token')).toBe(token);
    expect(service.isLoggedIn()).toBe(true);
    expect(service.currentUser()).toEqual({ id: 7, email: 'x@y.com', name: 'X Y', pictureUrl: undefined });
  });

  it('logout clears storage, user signal, and navigates to /login', () => {
    localStorage.setItem('auth_token', 'anything');
    service.logout();

    expect(localStorage.getItem('auth_token')).toBeNull();
    expect(service.currentUser()).toBeNull();
    expect(routerNavigate).toHaveBeenCalledWith(['/login']);
  });

  it('restoreSession discards an expired token instead of restoring the user', () => {
    const expired = makeJwt({ sub: '1', email: 'a@b', name: 'A', exp: Math.floor(Date.now() / 1000) - 60 });
    localStorage.setItem('auth_token', expired);

    // Re-create service so its constructor's restoreSession() runs again with the seeded token.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: { navigate: vi.fn() } }
      ]
    });
    const fresh = TestBed.inject(AuthService);

    expect(fresh.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('auth_token')).toBeNull();
  });
});
