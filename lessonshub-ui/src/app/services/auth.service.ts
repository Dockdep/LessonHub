import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

export interface AuthUser {
  id: number;
  email: string;
  name: string;
  pictureUrl?: string;
}

interface JwtPayload {
  sub: string;
  email: string;
  name: string;
  picture?: string;
  exp: number;
}

const TOKEN_KEY = 'auth_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _user = signal<AuthUser | null>(null);
  readonly currentUser = this._user.asReadonly();
  readonly isLoggedIn = computed(() => this._user() !== null);

  /**
   * Whether we're running in a real browser (vs. the Node SSR pass).
   * `localStorage`, `window`, etc. are only safe to touch when this is true.
   */
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  constructor(private http: HttpClient, private router: Router) {
    if (this.isBrowser) {
      this.restoreSession();
    }
    // On the server pass we skip session restore — the user signal stays null,
    // so any guard that depends on `isLoggedIn()` will treat the request as
    // unauthenticated. The browser pass re-runs constructors and hydrates
    // the signal from localStorage as usual.
  }

  private restoreSession(): void {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
      const user = this.decodeToken(token);
      if (user) {
        this._user.set(user);
      } else {
        localStorage.removeItem(TOKEN_KEY);
      }
    }
  }

  private decodeToken(token: string): AuthUser | null {
    try {
      const base64url = token.split('.')[1];
      const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '=');
      const bytes = Uint8Array.from(atob(padded), c => c.charCodeAt(0));
      const payload: JwtPayload = JSON.parse(new TextDecoder().decode(bytes));
      if (payload.exp * 1000 < Date.now()) return null;
      return {
        id: Number(payload.sub),
        email: payload.email,
        name: payload.name,
        pictureUrl: payload.picture
      };
    } catch {
      return null;
    }
  }

  getToken(): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem(TOKEN_KEY);
  }

  async loginWithGoogle(idToken: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<{ token: string; user: AuthUser }>('/api/auth/google', { idToken })
    );
    if (this.isBrowser) {
      localStorage.setItem(TOKEN_KEY, response.token);
    }
    this._user.set(response.user);
  }

  logout(): void {
    if (this.isBrowser) {
      localStorage.removeItem(TOKEN_KEY);
    }
    this._user.set(null);
    this.router.navigate(['/login']);
  }
}
