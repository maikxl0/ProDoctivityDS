export interface ProcessingOptions {
  removeFirstPage: boolean;
  onlyIfCriteriaMet: boolean;
  updateApi: boolean;
  saveOriginalFiles: boolean;
  autoRemoveAllSeparators: boolean;
  createBackup: boolean; // reservado
  removePagesEnabled: boolean;
  pagesToRemove: string; // ej. "1,3-5,7"
  removeMode: string;    // "specific" o "range"
  pageRangeStart: number;
  pageRangeEnd: number;
  analyzeAllPages: boolean;
  showExtractedText: boolean;
}