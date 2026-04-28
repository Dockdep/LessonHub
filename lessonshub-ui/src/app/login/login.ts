import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, inject, NgZone, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { MatIconModule } from '@angular/material/icon';

declare const google: {
  accounts: {
    id: {
      initialize(config: { client_id: string; callback: (response: { credential: string }) => void }): void;
      renderButton(parent: HTMLElement, options: object): void;
    };
  };
};

@Component({
  selector: 'app-login',
  imports: [MatIconModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login implements OnInit, AfterViewInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  private ngZone = inject(NgZone);
  private document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  @ViewChild('googleBtn') googleBtn!: ElementRef<HTMLElement>;

  error = '';
  loading = false;

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/today']);
    }
  }

  ngAfterViewInit(): void {
    // Google's GSI script must be loaded by the browser. During SSR we render
    // an empty button slot; the browser then hydrates and pulls in the script.
    if (!this.isBrowser) return;

    if (!this.auth.isLoggedIn()) {
      this.loadGoogleScript()
        .then(() => this.initGoogle())
        .catch(err => this.error = err);
    }
  }

  private loadGoogleScript(): Promise<void> {
    return new Promise((resolve, reject) => {
      // Check if already defined on window
      const win = this.document.defaultView as any;
      if (win && win.google?.accounts?.id) {
        resolve();
        return;
      }

      const scriptUrl = 'https://accounts.google.com/gsi/client';
      const existingScript = this.document.querySelector(`script[src*="${scriptUrl}"]`) as HTMLScriptElement;

      if (existingScript) {
        existingScript.addEventListener('load', () => resolve());
        existingScript.addEventListener('error', () => reject('Google Sign-In failed to load. Check your internet connection.'));
        return;
      }

      const script = this.document.createElement('script');
      script.src = scriptUrl;
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject('Google Sign-In failed to load. Check your internet connection.');
      this.document.head.appendChild(script);
    });
  }

  private initGoogle(): void {
    const win = this.document.defaultView as any;
    const clientId = win?.GOOGLE_CLIENT_ID
      ?? this.document.querySelector<HTMLMetaElement>('meta[name="google-client-id"]')?.content
      ?? '';

    if (!clientId) {
      this.error = 'Google Client ID is missing.';
      return;
    }

    google.accounts.id.initialize({
      client_id: clientId,
      callback: (response: { credential: string }) => {
        // Run inside NgZone so Angular change detection handles the updates
        this.ngZone.run(async () => {
          this.loading = true;
          this.error = '';
          try {
            await this.auth.loginWithGoogle(response.credential);
            await this.router.navigate(['/today']);
          } catch (err) {
            console.error('Sign-in error:', err);
            this.error = 'Sign-in failed. Please try again.';
          } finally {
            this.loading = false;
          }
        });
      }
    });

    if (this.googleBtn?.nativeElement) {
      google.accounts.id.renderButton(this.googleBtn.nativeElement, {
        type: 'standard',
        shape: 'rectangular',
        theme: 'outline',
        size: 'large',
        text: 'signin_with',
        width: 280
      });
    }
  }
}
