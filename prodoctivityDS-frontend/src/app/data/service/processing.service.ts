import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ProcessProgress } from '../../core/models/process-progress.model';
import { ProcessRequest } from '../../core/models/process-request.model';

@Injectable({ providedIn: 'root' })
export class ProcessingService {
  private http = inject(HttpClient);
  private apiUrl = '/api/processing';
  private sessionIdKey = 'sessionId';

  private getSessionId(): string | null {
    return localStorage.getItem(this.sessionIdKey);
  }

  private setSessionId(sessionId: string): void {
    localStorage.setItem(this.sessionIdKey, sessionId);
  }

  private clearSessionId(): void {
    localStorage.removeItem(this.sessionIdKey);
  }

  private createHeaders(): HttpHeaders {
    let headers = new HttpHeaders();
    const sessionId = this.getSessionId();
    if (sessionId) {
      headers = headers.set('X-Session-Id', sessionId);
    }
    return headers;
  }

  startProcessing(request: ProcessRequest): Observable<{ sessionId: string; message: string }> {
    console.log('Enviando startProcessing con headers:', this.createHeaders());
    return this.http
      .post<{ sessionId: string; message: string }>(`${this.apiUrl}/start`, request, {
        headers: this.createHeaders(),
      })
      .pipe(
        tap((response) => {
          console.log('Respuesta de startProcessing:', response);
          if (response?.sessionId) {
            this.setSessionId(response.sessionId);
            console.log('SessionId guardado:', response.sessionId);
          } else {
            console.warn('No se recibió sessionId en la respuesta');
          }
        }),
      );
  }

  getProgress(): Observable<ProcessProgress> {
    console.log('Obteniendo progreso con sessionId:', this.getSessionId());
    return this.http
      .get<ProcessProgress>(`${this.apiUrl}/progress`, {
        headers: this.createHeaders(),
      })
      .pipe(tap((progress) => console.log('Progreso recibido:', progress)));
  }

  cancelProcessing(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/cancel`,
      {},
      {
        headers: this.createHeaders(),
      },
    );
  }

  clearProgress(): Observable<{ message: string }> {
    return this.http
      .delete<{ message: string }>(`${this.apiUrl}/progress`, {
        headers: this.createHeaders(),
      })
      .pipe(
        tap(() => this.clearSessionId()), // Limpieza opcional al finalizar
      );
  }

  deleteDocument(documentId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/documents/${documentId}`);
  }
}
