export interface DuplicateCheckRequest {
  cedula: string;
}

export interface DuplicateDocument {
  documentId: string;
  name: string;
  documentTypeId: string;
  documentTypeName: string;
  fileSize: number;
  fileHash?: string;
  createdAt: Date;
  groupKey: string;
}

export interface DuplicateGroup {
  groupKey: string;
  documents: DuplicateDocument[];
  reason: string;
}

export interface DuplicateCheckResponse {
  groups: DuplicateGroup[];
  totalDocuments: number;
}