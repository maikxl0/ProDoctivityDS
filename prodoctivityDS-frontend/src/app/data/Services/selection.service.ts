import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SelectionService {
  private http = inject(HttpClient);
  private baseUrl = '/api/selection';

  /**
   * Envía los IDs seleccionados al backend (si es necesario)
   */
  selectDocuments(documentIds: string[]): Observable<any> {
    return this.http.post(`${this.baseUrl}/select`, documentIds);
  }

  /**
   * Deselecciona documentos
   */
  deselectDocuments(documentIds: string[]): Observable<any> {
    return this.http.post(`${this.baseUrl}/deselect`, documentIds);
  }

  /**
   * Obtiene los IDs seleccionados actualmente
   */
  getSelectedDocuments(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/selected`);
  }

  /**
   * Obtiene el contador de seleccionados
   */
  getSelectedCount(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/selected-count`);
  }
}