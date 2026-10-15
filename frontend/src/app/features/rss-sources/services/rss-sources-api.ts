import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RssSource } from '../models/rss-source';
import { CreateRssSourceRequest } from '../models/create-rss-source-request';

@Injectable({ providedIn: 'root' })
export class RssSourcesApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<RssSource[]> {
    return this.http.get<RssSource[]>('/api/rss-sources');
  }

  create(request: CreateRssSourceRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>('/api/rss-sources', request);
  }

  collect(id: string): Observable<{ addedCount: number }> {
    return this.http.post<{ addedCount: number }>(`/api/rss-sources/${id}/collect`, null);
  }
}