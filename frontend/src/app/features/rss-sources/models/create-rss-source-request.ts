export interface CreateRssSourceRequest {
  name: string;
  feedUrl: string;
  countryCode: string;
  description?: string | null;
  logoUrl?: string | null;
}