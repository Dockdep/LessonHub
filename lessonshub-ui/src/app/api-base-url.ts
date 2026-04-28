import { InjectionToken, inject, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';

/**
 * Absolute base URL for the .NET API.
 *
 * Empty when same-origin (local dev with Caddy). Set when the UI is hosted
 * on a different origin from the API (e.g. Cloud Run, where the UI lives at
 * `lessonshub-ui-XXXX.a.run.app` and the API at `lessonshub-XXXX.a.run.app`).
 *
 * Browser pass: read from a `<meta name="api-base-url">` tag injected by the
 * SSR Express server.
 *
 * SSR pass: read directly from the `API_BASE_URL` env var.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => {
    if (isPlatformBrowser(inject(PLATFORM_ID))) {
      const meta = inject(DOCUMENT).querySelector<HTMLMetaElement>('meta[name="api-base-url"]');
      return (meta?.content ?? '').replace(/\/$/, '');
    }
    const env = (globalThis as { process?: { env?: Record<string, string> } }).process?.env;
    return (env?.['API_BASE_URL'] ?? '').replace(/\/$/, '');
  },
});
