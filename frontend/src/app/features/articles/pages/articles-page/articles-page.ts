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
}