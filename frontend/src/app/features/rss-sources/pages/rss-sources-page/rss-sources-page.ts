import { Component, inject, OnInit, signal } from '@angular/core';
import { RssSource } from '../../models/rss-source';
import { RssSourcesApi } from '../../services/rss-sources-api';

@Component({
  selector: 'app-rss-sources-page',
  templateUrl: './rss-sources-page.html',
  styleUrl: './rss-sources-page.scss',
})
export class RssSourcesPage implements OnInit {
  private readonly api = inject(RssSourcesApi);

  readonly sources = signal<RssSource[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
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
}
