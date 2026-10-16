export interface Article {
  id: string;
  rssSourceId: string;
  title: string;
  articleUrl: string;
  publishedAt: string | null;
  collectedAt: string;
  sourceName: string;
  sourceLogoUrl: string | null;
}