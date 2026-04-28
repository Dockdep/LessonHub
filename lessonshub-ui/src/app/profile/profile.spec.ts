import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Profile } from './profile';

describe('Profile component', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Profile],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideNoopAnimations()]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads profile on init and renders the name and email', async () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges(); // triggers ngOnInit

    const req = http.expectOne('/api/user/profile');
    req.flush({ email: 'me@x.com', name: 'Me', googleApiKey: 'abc' });

    fixture.detectChanges();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('me@x.com');
    expect(text).toContain('Me');

    // The input is bound to the loaded key
    const apiKeyInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('#apiKey');
    expect(apiKeyInput?.value).toBe('abc');
  });

  it('save() PUTs the trimmed key and refreshes local state', async () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();
    http.expectOne('/api/user/profile').flush({ email: 'me@x.com', name: 'Me', googleApiKey: '' });
    fixture.detectChanges();

    fixture.componentInstance['googleApiKey'] = '  fresh-key  ';
    fixture.componentInstance.save();

    const req = http.expectOne('/api/user/profile');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ googleApiKey: 'fresh-key' });
    req.flush({ email: 'me@x.com', name: 'Me', googleApiKey: 'fresh-key' });

    expect(fixture.componentInstance['googleApiKey']).toBe('fresh-key');
    expect(fixture.componentInstance['saving']()).toBe(false);
  });

  it('save() with blank value sends null (clears the key)', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();
    http.expectOne('/api/user/profile').flush({ email: 'me@x.com', name: 'Me', googleApiKey: 'abc' });

    fixture.componentInstance['googleApiKey'] = '   ';
    fixture.componentInstance.save();

    const req = http.expectOne('/api/user/profile');
    expect(req.request.body).toEqual({ googleApiKey: null });
    req.flush({ email: 'me@x.com', name: 'Me', googleApiKey: null });
  });
});
