import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'sources',
  },
  {
    path: 'sources',
    loadComponent: () =>
      import('./features/rss-sources/pages/rss-sources-page/rss-sources-page').then(
        (module) => module.RssSourcesPage,
      ),
  },
];