import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { RssSource } from '../../models/rss-source';
import { RssSourcesPage } from './rss-sources-page';

describe('RssSourcesPage', () => {
  let fixture: ComponentFixture<RssSourcesPage>;
  let http: HttpTestingController;
  let page: HTMLElement;

  const source: RssSource = {
    id: 'source-1',
    name: 'Source test',
    feedUrl: 'https://example.com/feed',
    countryCode: 'FR',
    isEnabled: true,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RssSourcesPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(RssSourcesPage);
    page = fixture.nativeElement as HTMLElement;

    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
  });

  function fillForm(): void {
    const values = {
      'source-name': 'Source test',
      'source-url': 'https://example.com/feed',
      'source-country': 'fr',
    };

    for (const [id, value] of Object.entries(values)) {
      const input = page.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input', { bubbles: true }));
    }

    fixture.detectChanges();
  }

  function submitForm(): void {
    page
      .querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    fixture.detectChanges();
  }

  it('adds a source, resets the form and refreshes the list', () => {
    http.expectOne('/api/rss-sources').flush([]);
    fixture.detectChanges();

    fillForm();
    submitForm();

    const request = http.expectOne('/api/rss-sources');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      name: 'Source test',
      feedUrl: 'https://example.com/feed',
      countryCode: 'FR',
    });

    expect(page.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(true);

    request.flush({ id: source.id });

    const refresh = http.expectOne('/api/rss-sources');
    expect(refresh.request.method).toBe('GET');
    refresh.flush([source]);

    fixture.detectChanges();

    expect(page.querySelector('li strong')?.textContent).toContain(source.name);
    expect(page.querySelector<HTMLInputElement>('#source-name')!.value).toBe('');
    expect(page.querySelector<HTMLInputElement>('#source-url')!.value).toBe('');
    expect(page.querySelector<HTMLInputElement>('#source-country')!.value).toBe('FR');
    expect(page.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(false);
  });

  it('shows a duplicate error and preserves the entered values', () => {
    http.expectOne('/api/rss-sources').flush([]);
    fixture.detectChanges();

    fillForm();
    submitForm();

    http.expectOne('/api/rss-sources').flush({}, { status: 409, statusText: 'Conflict' });

    fixture.detectChanges();

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'Cette URL est déjà enregistrée.',
    );

    expect(page.querySelector<HTMLInputElement>('#source-name')!.value).toBe('Source test');

    expect(page.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(false);

    http.expectNone('/api/rss-sources');
  });

  it('shows a successful collection with zero new articles', () => {
    http.expectOne('/api/rss-sources').flush([source]);
    fixture.detectChanges();

    page.querySelector<HTMLButtonElement>('li button')!.click();
    fixture.detectChanges();

    expect(page.querySelector<HTMLButtonElement>('li button')!.disabled).toBe(true);

    const request = http.expectOne(`/api/rss-sources/${source.id}/collect`);

    expect(request.request.method).toBe('POST');
    request.flush({ addedCount: 0 });

    fixture.detectChanges();

    expect(page.querySelector('[role="status"]')?.textContent).toContain(
      '0 nouvel(s) article(s) collecté(s).',
    );

    expect(page.querySelector<HTMLButtonElement>('li button')!.disabled).toBe(false);
  });

  it('shows a timeout error and allows another collection', () => {
    http.expectOne('/api/rss-sources').flush([source]);
    fixture.detectChanges();

    page.querySelector<HTMLButtonElement>('li button')!.click();

    http
      .expectOne(`/api/rss-sources/${source.id}/collect`)
      .flush({}, { status: 504, statusText: 'Gateway Timeout' });

    fixture.detectChanges();

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'Le site met trop de temps à répondre.',
    );

    expect(page.querySelector<HTMLButtonElement>('li button')!.disabled).toBe(false);
  });
});