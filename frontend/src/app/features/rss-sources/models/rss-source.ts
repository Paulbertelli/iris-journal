export interface RssSource {
  id: string;
  name: string;
  feedUrl: string;
  countryCode: string;
  isEnabled: boolean;
  description: string | null;
  logoUrl: string | null;
}