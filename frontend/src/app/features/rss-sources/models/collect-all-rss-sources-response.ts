export interface CollectAllRssSourcesResponse {
  totalAddedCount: number;
  sources: {
    sourceId: string;
    sourceName: string;
    status: 'Collected' | 'NotFound' | 'Disabled' | 'Failed' | 'Timeout';
    addedCount: number;
    error: string | null;
  }[];
}
