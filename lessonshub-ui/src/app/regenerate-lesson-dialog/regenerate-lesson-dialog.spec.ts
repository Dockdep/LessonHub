import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { RegenerateLessonDialog } from './regenerate-lesson-dialog';

describe('RegenerateLessonDialog', () => {
  let closeSpy: ReturnType<typeof vi.fn>;

  function build(isTechnical: boolean) {
    closeSpy = vi.fn();
    TestBed.configureTestingModule({
      imports: [RegenerateLessonDialog],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: { close: closeSpy } },
        { provide: MAT_DIALOG_DATA, useValue: { isTechnical } }
      ]
    });
    const fixture = TestBed.createComponent(RegenerateLessonDialog);
    fixture.detectChanges();
    return fixture;
  }

  it('hides the docs toggle for non-Technical lessons', () => {
    const fixture = build(false);
    const checkbox = (fixture.nativeElement as HTMLElement).querySelector('input[type="checkbox"]');
    expect(checkbox).toBeNull();
  });

  it('shows the docs toggle for Technical lessons', () => {
    const fixture = build(true);
    const checkbox = (fixture.nativeElement as HTMLElement).querySelector('input[type="checkbox"]');
    expect(checkbox).not.toBeNull();
  });

  it('cancel() closes with null', () => {
    const fixture = build(true);
    fixture.componentInstance.cancel();
    expect(closeSpy).toHaveBeenCalledWith(null);
  });

  it('confirm() with Technical+checked returns bypassDocCache=true', () => {
    const fixture = build(true);
    fixture.componentInstance.bypassDocCache = true;
    fixture.componentInstance.confirm();
    expect(closeSpy).toHaveBeenCalledWith({ bypassDocCache: true });
  });

  it('confirm() with non-Technical always returns bypassDocCache=false even if user toggled', () => {
    const fixture = build(false);
    fixture.componentInstance.bypassDocCache = true; // user shouldn't be able to but defend anyway
    fixture.componentInstance.confirm();
    expect(closeSpy).toHaveBeenCalledWith({ bypassDocCache: false });
  });
});
