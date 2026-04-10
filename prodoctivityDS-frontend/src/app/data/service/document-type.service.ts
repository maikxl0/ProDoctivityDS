import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { DocumentType } from '../../core/models/document-type.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DocumentTypeService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/documenttypes`;

  getDocumentTypes(): Observable<DocumentType[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(items => items.map(item => ({
        id: item.documentTypeId,   // <-- mapea documentTypeId a id
        name: item.name
      })))
    );
  }
}
