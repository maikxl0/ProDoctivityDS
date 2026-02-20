import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Document } from '../../core/models/document.model';

export interface SearchResponse {
  documents: Document[];
  totalCount: number;
  currentPage: number;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);
  private apiUrl = '/api/documents'; // ajusta según tu backend

  searchDocuments(
    page: number,
    pageSize: number,
    name?: string,
    documentTypeIds?: string[]
  ): Observable<SearchResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('rowsPerPage', pageSize.toString());

    if (name) {
      params = params.set('name', name);
    }
    if (documentTypeIds && documentTypeIds.length) {
      // Si el backend espera múltiples valores con el mismo nombre, usa append
      documentTypeIds.forEach(id => {
        params = params.append('documentTypeIds', id);
      });
    }

    return this.http.get<SearchResponse>(this.apiUrl, { params });
  }
}