export interface Document {
  documentId: string;
  name: string;
  documentTypeName: string;
  documentTypeId: string;
  createdAt: number; // timestamp Unix
  analysisStatus?: string; // Pendiente, Removida, Conservada
  data?: any;
}

// Interfaz para la respuesta paginada de la API
export interface DocumentSearchResponse {
  documents: Document[];
  totalCount: number;
  currentPage: number;
}