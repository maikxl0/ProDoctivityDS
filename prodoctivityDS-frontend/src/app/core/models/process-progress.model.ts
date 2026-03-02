export interface ProcessProgress {
  total: number;
  processed: number;
  updated: number;
  pagesRemoved: number;
  errors: number;
  skipped: number;
  currentDocumentName: string;
  currentDocumentId?: string;
  status: string;
  percentComplete: number; // se calcula en el backend o front
}