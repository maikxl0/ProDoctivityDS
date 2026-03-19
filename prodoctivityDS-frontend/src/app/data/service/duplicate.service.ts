import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DuplicateCheckRequest, DuplicateCheckResponse } from '../../core/models/duplicate.models';

@Injectable({ providedIn: 'root' })
export class DuplicateService {
  private http = inject(HttpClient);
  private apiUrl = '/api/duplicate';

  checkByCedula(request: DuplicateCheckRequest): Observable<DuplicateCheckResponse> {
    return this.http.post<DuplicateCheckResponse>(`${this.apiUrl}/check-by-cedula`, request);
  }
}