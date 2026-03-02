import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProcessProgress } from '../../core/models/process-progress.model';
import { ProcessRequest } from '../../core/models/process-request.model';

@Injectable({ providedIn: 'root' })
export class ProcessingService {
  private http = inject(HttpClient);
  private apiUrl = '/api/processing';

  startProcessing(request: ProcessRequest): Observable<{ sessionId: string; message: string }> {
    return this.http.post<{ sessionId: string; message: string }>(`${this.apiUrl}/start`, request);
  }

  getProgress(): Observable<ProcessProgress> {
    return this.http.get<ProcessProgress>(`${this.apiUrl}/progress`);
  }

  cancelProcessing(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/cancel`, {});
  }

  clearProgress(): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/progress`);
  }
}