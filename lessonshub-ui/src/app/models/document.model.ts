export type IngestionStatus = 'Pending' | 'Ingested' | 'Failed';

export interface Document {
  id: number;
  name: string;
  contentType: string;
  sizeBytes: number;
  ingestionStatus: IngestionStatus;
  ingestionError: string | null;
  chunkCount: number | null;
  createdAt: string;
  ingestedAt: string | null;
}
