/**
 * Express server hosting the Angular SSR app.
 *
 * Layout:
 *  - dist/lessonshub-ui/server/server.mjs  ← this file (compiled)
 *  - dist/lessonshub-ui/browser/...        ← browser bundle + static assets
 *
 * Strategy:
 *  - Static assets are served from /browser with long cache headers.
 *  - Anything else falls through to AngularNodeAppEngine for SSR.
 *  - HTML responses get a `<meta name="api-base-url">` tag injected with the
 *    value of the API_BASE_URL env var, so the browser knows where the .NET
 *    API lives when the UI is deployed cross-origin (e.g. Cloud Run).
 *  - HTML responses get the `<meta name="google-client-id">` tag *replaced*
 *    with the value of GOOGLE_OAUTH_CLIENT_ID when set, overriding the dev
 *    fallback hardcoded in index.html.
 *  - /api/* requests are NOT handled here; in production we expect a reverse
 *    proxy (Caddy in docker-compose) to route them to the .NET service.
 */
import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

// Trim trailing slash; safer when concatenating with paths.
const apiBaseUrl = (process.env['API_BASE_URL'] ?? '').replace(/\/$/, '');
const googleClientId = process.env['GOOGLE_OAUTH_CLIENT_ID'] ?? '';

const escapeHtmlAttr = (value: string) => value.replace(/"/g, '&quot;');

app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

// All other GETs are SSR-rendered by Angular.
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then(async (response) => {
      if (!response) return next();

      const contentType = response.headers.get('content-type') ?? '';
      const isHtml = contentType.includes('text/html');
      const shouldInject = isHtml && (apiBaseUrl || googleClientId);

      if (!shouldInject) {
        return writeResponseToNodeResponse(response, res);
      }

      let html = await response.text();

      if (apiBaseUrl) {
        // Inject a new meta tag (no equivalent exists in index.html).
        html = html.replace(
          '</head>',
          `<meta name="api-base-url" content="${escapeHtmlAttr(apiBaseUrl)}"></head>`,
        );
      }

      if (googleClientId) {
        // Override the dev fallback baked into index.html.
        html = html.replace(
          /(<meta\s+name="google-client-id"\s+content=")[^"]*"/,
          `$1${escapeHtmlAttr(googleClientId)}"`,
        );
      }

      res.status(response.status);
      response.headers.forEach((value, key) => {
        if (key.toLowerCase() === 'content-length') return;
        res.setHeader(key, value);
      });
      res.send(html);
    })
    .catch(next);
});

if (isMainModule(import.meta.url)) {
  const port = Number(process.env['PORT']) || 4000;
  app.listen(port, () => {
    console.log(`lessonshub-ui SSR server listening on http://0.0.0.0:${port}`);
  });
}

export const reqHandler = createNodeRequestHandler(app);
