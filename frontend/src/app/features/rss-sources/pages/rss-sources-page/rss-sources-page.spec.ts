import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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
    description: null,
    logoUrl: null,
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
    vi.restoreAllMocks();
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

  function collectButton(): HTMLButtonElement {
    return page.querySelector<HTMLButtonElement>('.source-actions button[title="Collecter"]')!;
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
      description: null,
      logoUrl: null,
      isEnabled: true,
    });

    expect(page.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(true);

    request.flush({ id: source.id });

    const refresh = http.expectOne('/api/rss-sources');

    expect(refresh.request.method).toBe('GET');
    refresh.flush([source]);

    fixture.detectChanges();

    expect(page.querySelector('.source-heading h2')?.textContent).toContain(source.name);

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

    collectButton().click();
    fixture.detectChanges();

    expect(collectButton().disabled).toBe(true);

    const request = http.expectOne(`/api/rss-sources/${source.id}/collect`);

    expect(request.request.method).toBe('POST');
    request.flush({ addedCount: 0 });

    fixture.detectChanges();

    expect(page.querySelector('.source-card [role="status"]')?.textContent).toContain(
      'Aucun nouvel article.',
    );

    expect(collectButton().disabled).toBe(false);
  });

  it('shows a timeout error and allows another collection', () => {
    http.expectOne('/api/rss-sources').flush([source]);
    fixture.detectChanges();

    collectButton().click();

    http
      .expectOne(`/api/rss-sources/${source.id}/collect`)
      .flush({}, { status: 504, statusText: 'Gateway Timeout' });

    fixture.detectChanges();

    expect(page.querySelector('.source-card [role="alert"]')?.textContent).toContain(
      'Le site met trop de temps à répondre.',
    );

    expect(collectButton().disabled).toBe(false);
  });

  it('edits a source and refreshes the list after saving', () => {
    http.expectOne('/api/rss-sources').flush([source]);
    fixture.detectChanges();

    page.querySelector<HTMLButtonElement>('.source-actions button[title="Modifier"]')!.click();

    fixture.detectChanges();

    const nameInput = page.querySelector<HTMLInputElement>('#source-name')!;

    expect(nameInput.value).toBe(source.name);

    nameInput.value = 'Source modifiée';
    nameInput.dispatchEvent(new Event('input', { bubbles: true }));

    submitForm();

    const request = http.expectOne(`/api/rss-sources/${source.id}`);

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Source modifiée',
      feedUrl: source.feedUrl,
      countryCode: source.countryCode,
      isEnabled: source.isEnabled,
      description: null,
      logoUrl: null,
    });

    request.flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    const refresh = http.expectOne('/api/rss-sources');

    expect(refresh.request.method).toBe('GET');
    refresh.flush([{ ...source, name: 'Source modifiée' }]);

    fixture.detectChanges();

    expect(page.querySelector('.source-heading h2')?.textContent).toContain('Source modifiée');

    expect(nameInput.value).toBe('');

    expect(page.querySelector<HTMLButtonElement>('button[type="submit"]')!.textContent).toContain(
      'Ajouter la source',
    );
  });

  it('does not delete a source when confirmation is canceled', () => {
    const confirmation = vi.spyOn(window, 'confirm').mockReturnValue(false);

    http.expectOne('/api/rss-sources').flush([source]);
    fixture.detectChanges();

    page.querySelector<HTMLButtonElement>('.source-actions button[title="Supprimer"]')!.click();

    fixture.detectChanges();

    expect(confirmation).toHaveBeenCalledTimes(1);

    http.expectNone((request) => request.method === 'DELETE');

    expect(page.querySelector('.source-heading h2')?.textContent).toContain(source.name);
  });

  it('removes the source card after confirmed deletion succeeds', () => {
    const confirmation = vi.spyOn(window, 'confirm').mockReturnValue(true);

    http.expectOne('/api/rss-sources').flush([source]);
    fixture.detectChanges();

    page.querySelector<HTMLButtonElement>('.source-actions button[title="Supprimer"]')!.click();

    fixture.detectChanges();

    expect(confirmation).toHaveBeenCalledTimes(1);
    expect(confirmation.mock.calls[0][0]).toContain(source.name);
    expect(confirmation.mock.calls[0][0]).toContain('tous ses articles');

    const request = http.expectOne(`/api/rss-sources/${source.id}`);

    expect(request.request.method).toBe('DELETE');

    // La carte reste présente tant que le serveur n’a pas confirmé.
    expect(page.querySelector('.source-card')).not.toBeNull();

    expect(
      page.querySelector<HTMLButtonElement>('.source-actions button[title="Supprimer"]')!.disabled,
    ).toBe(true);

    request.flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    fixture.detectChanges();

    expect(page.querySelector('.source-card')).toBeNull();
    expect(page.textContent).toContain('Aucune source RSS enregistrée.');
  });
});