import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { Article } from '../../models/article';
import { ArticlesApi } from '../../services/articles-api';
import { RssSourcesApi } from '../../../rss-sources/services/rss-sources-api';
import { BehaviorSubject, catchError, map, of, startWith, switchMap } from 'rxjs';
import { RssSource } from '../../../rss-sources/models/rss-source';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-articles-page',
  imports: [DatePipe],
  templateUrl: './articles-page.html',
  styleUrl: './articles-page.scss',
})
export class ArticlesPage implements OnInit {
  private readonly api = inject(ArticlesApi);

  readonly articles = signal<Article[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly failedLogoUrls = signal<ReadonlySet<string>>(new Set());
  readonly failedImageUrls = signal<ReadonlySet<string>>(new Set());

  private readonly sourcesApi = inject(RssSourcesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sourceFilter = new BehaviorSubject<string | null>(null);

  readonly sources = signal<RssSource[]>([]);
  readonly selectedSourceId = signal<string | null>(null);
  readonly sourcesError = signal<string | null>(null);

  ngOnInit(): void {
    this.sourcesApi
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (sources) => this.sources.set(sources),
        error: () => {
          this.sourcesError.set('Impossible de charger le filtre des sources.');
        },
      });

    this.sourceFilter
      .pipe(
        switchMap((sourceId) =>
          this.api.getLatest(50, sourceId).pipe(
            map((articles) => ({
              articles,
              loading: false,
              error: null as string | null,
            })),
            catchError(() =>
              of({
                articles: [] as Article[],
                loading: false,
                error: 'Impossible de charger les articles.',
              }),
            ),
            startWith({
              articles: [] as Article[],
              loading: true,
              error: null as string | null,
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((state) => {
        this.articles.set(state.articles);
        this.loading.set(state.loading);
        this.error.set(state.error);
      });
  }

  changeSource(value: string): void {
    const sourceId = value || null;

    this.selectedSourceId.set(sourceId);
    this.sourceFilter.next(sourceId);
  }

  markLogoFailed(url: string): void {
    this.failedLogoUrls.update((urls) => new Set([...urls, url]));
  }

  markImageFailed(url: string): void {
    this.failedImageUrls.update((urls) => new Set([...urls, url]));
  }

  relativePublishedAt(value: string | null): string {
    if (!value) return 'Date inconnue';

    const timestamp = Date.parse(value);
    if (Number.isNaN(timestamp)) return 'Date inconnue';

    const seconds = (timestamp - Date.now()) / 1000;
    const absoluteSeconds = Math.abs(seconds);

    const units: [Intl.RelativeTimeFormatUnit, number][] = [
      ['year', 365.25 * 24 * 3600],
      ['month', 30.44 * 24 * 3600],
      ['day', 24 * 3600],
      ['hour', 3600],
      ['minute', 60],
      ['second', 1],
    ];

    const [unit, duration] =
      units.find(([, duration]) => absoluteSeconds >= duration) ?? units[units.length - 1];

    return new Intl.RelativeTimeFormat('fr', {
      numeric: 'always',
    }).format(Math.trunc(seconds / duration) || 0, unit);
  }
}