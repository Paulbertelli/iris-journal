import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { Article } from '../../models/article';
import { ArticlesApi } from '../../services/articles-api';

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

  ngOnInit(): void {
    this.api.getLatest().subscribe({
      next: (articles) => {
        this.articles.set(articles);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Impossible de charger les articles.');
        this.loading.set(false);
      },
    });
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