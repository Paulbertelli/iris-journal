import { Component, inject, OnInit, signal } from '@angular/core';
import { RssSource } from '../../models/rss-source';
import { RssSourcesApi } from '../../services/rss-sources-api';
import { HttpErrorResponse } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-rss-sources-page',
  templateUrl: './rss-sources-page.html',
  styleUrl: './rss-sources-page.scss',
  imports: [ReactiveFormsModule],
})
export class RssSourcesPage implements OnInit {
  private readonly api = inject(RssSourcesApi);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly sources = signal<RssSource[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly collectingId = signal<string | null>(null);
  readonly collectionFeedback = signal<Record<string, { message: string; error: boolean }>>({});
  readonly editingId = signal<string | null>(null);
  readonly deletingId = signal<string | null>(null);
  readonly deleteError = signal<string | null>(null);
  readonly failedLogoUrls = signal<ReadonlySet<string>>(new Set());
  readonly collectingAll = signal(false);
  readonly collectAllMessage = signal<string | null>(null);
  readonly collectAllError = signal<string | null>(null);

  readonly form = this.formBuilder.group({
    name: ['', [Validators.required, Validators.pattern(/\S/)]],
    feedUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/\S+$/i)]],
    countryCode: ['FR', [Validators.required, Validators.pattern(/^[a-zA-Z]{2}$/)]],
    description: [''],
    logoUrl: ['', Validators.pattern(/^https?:\/\/\S+$/i)],
    isEnabled: [true],
  });

  ngOnInit(): void {
    this.loadSources();
  }

  private loadSources(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getAll().subscribe({
      next: (sources) => {
        this.sources.set(sources);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Impossible de charger les sources RSS.');
        this.loading.set(false);
      },
    });
  }

  submit(): void {
    if (this.collectingAll()) return;

    if (this.deletingId() !== null) return;

    if (this.submitting()) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    const values = this.form.getRawValue();
    const id = this.editingId();

    const request = {
      name: values.name.trim(),
      feedUrl: values.feedUrl.trim(),
      countryCode: values.countryCode.toUpperCase(),
      description: values.description.trim() || null,
      logoUrl: values.logoUrl.trim() || null,
      isEnabled: values.isEnabled,
    };

    const onSuccess = (): void => {
      this.submitting.set(false);
      this.resetForm();
      this.loadSources();
    };

    const onError = (error: HttpErrorResponse): void => {
      this.submitting.set(false);
      this.submitError.set(
        error.status === 409
          ? 'Cette URL est déjà enregistrée.'
          : error.status === 404
            ? 'Cette source n’existe plus.'
            : 'Impossible d’enregistrer la source. Vérifie les données et réessaie.',
      );
    };

    if (id !== null) {
      this.api.update(id, request).subscribe({
        next: onSuccess,
        error: onError,
      });
    } else {
      this.api.create(request).subscribe({
        next: onSuccess,
        error: onError,
      });
    }
  }

  collect(id: string): void {
    if (this.collectingAll()) return;
    if (this.collectingId() !== null || this.deletingId() !== null) return;

    this.collectingId.set(id);

    this.collectionFeedback.update((feedback) => {
      const updated = { ...feedback };
      delete updated[id];
      return updated;
    });

    this.api.collect(id).subscribe({
      next: ({ addedCount }) => {
        this.collectingId.set(null);

        const message =
          addedCount === 0
            ? 'Aucun nouvel article.'
            : `${addedCount} nouvel${addedCount > 1 ? 's' : ''} article${addedCount > 1 ? 's' : ''} collecté${addedCount > 1 ? 's' : ''}.`;

        this.collectionFeedback.update((feedback) => ({
          ...feedback,
          [id]: { message, error: false },
        }));
      },
      error: (error: HttpErrorResponse) => {
        this.collectingId.set(null);

        const message =
          error.status === 504
            ? 'Le site met trop de temps à répondre.'
            : error.status === 502
              ? 'Impossible de lire ce flux.'
              : 'La collecte a échoué. Réessaie.';

        this.collectionFeedback.update((feedback) => ({
          ...feedback,
          [id]: { message, error: true },
        }));
      },
    });
  }

  edit(source: RssSource): void {
    if (this.collectingAll()) return;
    if (this.deletingId() !== null) return;

    if (this.submitting()) return;

    this.editingId.set(source.id);
    this.submitError.set(null);

    this.form.reset({
      name: source.name,
      feedUrl: source.feedUrl,
      countryCode: source.countryCode,
      description: source.description ?? '',
      logoUrl: source.logoUrl ?? '',
      isEnabled: source.isEnabled,
    });
  }

  cancelEdit(): void {
    if (this.submitting()) return;

    this.resetForm();
  }

  private resetForm(): void {
    this.editingId.set(null);
    this.submitError.set(null);

    this.form.reset({
      name: '',
      feedUrl: '',
      countryCode: 'FR',
      description: '',
      logoUrl: '',
      isEnabled: true,
    });
  }

  deleteSource(source: RssSource): void {
    if (this.collectingAll()) return;
    if (this.deletingId() !== null || this.submitting() || this.collectingId() !== null) return;

    const confirmed = window.confirm(
      `Supprimer « ${source.name} » et tous ses articles ? Cette action est définitive.`,
    );

    if (!confirmed) return;

    this.deletingId.set(source.id);
    this.deleteError.set(null);

    this.api.delete(source.id).subscribe({
      next: () => {
        if (this.editingId() === source.id) {
          this.resetForm();
        }

        this.sources.update((sources) => sources.filter((item) => item.id !== source.id));

        this.deletingId.set(null);
      },
      error: () => {
        this.deletingId.set(null);
        this.deleteError.set('Impossible de supprimer la source. Réessaie.');
      },
    });
  }

  markLogoFailed(url: string): void {
    this.failedLogoUrls.update((urls) => new Set([...urls, url]));
  }

  collectAll(): void {
    if (
      this.collectingAll() ||
      this.collectingId() !== null ||
      this.deletingId() !== null ||
      this.submitting() ||
      this.loading() ||
      this.error() !== null ||
      !this.sources().some((source) => source.isEnabled)
    ) {
      return;
    }

    this.collectingAll.set(true);
    this.collectAllMessage.set(null);
    this.collectAllError.set(null);
    this.collectionFeedback.set({});

    this.api.collectAll().subscribe({
      next: (result) => {
        this.collectingAll.set(false);

        const feedback: Record<string, { message: string; error: boolean }> = {};

        for (const source of result.sources) {
          switch (source.status) {
            case 'Collected':
              feedback[source.sourceId] = {
                message:
                  source.addedCount === 0
                    ? 'Aucun nouvel article.'
                    : `${source.addedCount} article(s) collecté(s).`,
                error: false,
              };
              break;

            case 'Timeout':
              feedback[source.sourceId] = {
                message: 'Le site met trop de temps à répondre.',
                error: true,
              };
              break;

            case 'Failed':
              feedback[source.sourceId] = {
                message: 'Impossible de lire ce flux.',
                error: true,
              };
              break;

            case 'Disabled':
              feedback[source.sourceId] = {
                message: 'Cette source est désactivée.',
                error: true,
              };
              break;

            case 'NotFound':
              feedback[source.sourceId] = {
                message: 'Cette source n’existe plus.',
                error: true,
              };
              break;
          }
        }

        this.collectionFeedback.set(feedback);

        const succeeded = result.sources.filter((source) => source.status === 'Collected').length;

        const failed = result.sources.length - succeeded;

        this.collectAllMessage.set(
          `${result.totalAddedCount} article(s) ajouté(s). ` +
            `${succeeded} source(s) collectée(s), ${failed} non collectée(s).`,
        );
      },
      error: () => {
        this.collectingAll.set(false);
        this.collectAllError.set(
          'La collecte globale a été interrompue. Certains articles peuvent déjà avoir été enregistrés.',
        );
      },
    });
  }

  hasActiveSources(): boolean {
    return this.sources().some((source) => source.isEnabled);
  }
}
