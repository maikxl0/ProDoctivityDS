import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DuplicateCheckRequest, DuplicateCheckResponse } from '../../core/models/duplicate.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DuplicateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/duplicate`;

  checkByCedula(request: DuplicateCheckRequest): Observable<DuplicateCheckResponse> {
    return this.http.post<DuplicateCheckResponse>(`${this.apiUrl}/check-by-cedula`, request);
  }
}