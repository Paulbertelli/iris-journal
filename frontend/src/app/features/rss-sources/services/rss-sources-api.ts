import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RssSource } from '../models/rss-source';

@Injectable({ providedIn: 'root' })
export class RssSourcesApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<RssSource[]> {
    return this.http.get<RssSource[]>('/api/rss-sources');
  }
}