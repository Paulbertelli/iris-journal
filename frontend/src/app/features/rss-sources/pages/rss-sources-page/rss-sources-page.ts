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

  readonly form = this.formBuilder.group({
    name: ['', Validators.required],
    feedUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/\S+$/i)]],
    countryCode: ['FR', [Validators.required, Validators.pattern(/^[a-zA-Z]{2}$/)]],
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
    if (this.submitting()) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    const values = this.form.getRawValue();

    this.api
      .create({
        name: values.name.trim(),
        feedUrl: values.feedUrl.trim(),
        countryCode: values.countryCode.toUpperCase(),
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.form.reset({ name: '', feedUrl: '', countryCode: 'FR' });
          this.loadSources();
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.submitError.set(
            error.status === 409
              ? 'Cette URL est déjà enregistrée.'
              : 'Impossible d’ajouter la source. Vérifie les données et réessaie.',
          );
        },
      });
  }
}
