export interface UpdateRssSourceRequest {
  name: string;
  feedUrl: string;
  countryCode: string;
  isEnabled: boolean;
  description: string | null;
  logoUrl: string | null;
}