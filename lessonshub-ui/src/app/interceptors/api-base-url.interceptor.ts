import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_URL } from '../api-base-url';

/**
 * Prefix relative `/api/...` (or `api/...`) URLs with the API base URL when
 * the UI runs cross-origin from the API. No-op when base URL is empty.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = inject(API_BASE_URL);
  if (!baseUrl) return next(req);
  if (/^https?:\/\//i.test(req.url)) return next(req);
  const path = req.url.startsWith('/') ? req.url : `/${req.url}`;
  return next(req.clone({ url: baseUrl + path }));
};
