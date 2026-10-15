import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Article } from '../models/article';

@Injectable({ providedIn: 'root' })
export class ArticlesApi {
  private readonly http = inject(HttpClient);

  getLatest(limit = 50): Observable<Article[]> {
    return this.http.get<Article[]>('/api/articles', {
      params: { limit },
    });
  }
}