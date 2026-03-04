export interface ActivityLogEntry {
  id?: string;
  timestamp: Date; // o string si viene en ISO
  level: 'INFO' | 'SUCCESS' | 'WARNING' | 'ERROR' | 'DEBUG';
  category: string;
  message: string;
  documentId?: string;
}