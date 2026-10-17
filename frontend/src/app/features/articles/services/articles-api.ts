import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Article } from '../models/article';

@Injectable({ providedIn: 'root' })
export class ArticlesApi {
  private readonly http = inject(HttpClient);

  getLatest(limit = 50, sourceId: string | null = null): Observable<Article[]> {
    const params: Record<string, string | number> = { limit };

    if (sourceId) {
      params['sourceId'] = sourceId;
    }
    
    return this.http.get<Article[]>('/api/articles', { params });
  }
}
