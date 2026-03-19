import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SelectionService {
  private http = inject(HttpClient);
  private baseUrl = '/api/selection';
  private sessionIdKey = 'app-session-id';

  private getSessionId(): string {
    let sessionId = localStorage.getItem(this.sessionIdKey);
    if (!sessionId) {
      // Generar un UUID v4 (compatible con navegadores modernos)
      sessionId = crypto.randomUUID ? crypto.randomUUID() : this.generateUUID();
      localStorage.setItem(this.sessionIdKey, sessionId);
    }
    return sessionId;
  }

  // Fallback para navegadores sin crypto.randomUUID
  private generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  private createHeaders(): HttpHeaders {
    return new HttpHeaders().set('X-Session-Id', this.getSessionId());
  }

  selectDocuments(documentIds: string[]): Observable<any> {
    return this.http.post(`${this.baseUrl}/select`, documentIds, { headers: this.createHeaders() });
  }

  clearAllSelection(): Observable<any> {
  return this.http.delete(`${this.baseUrl}/clear`, { headers: this.createHeaders() });
}

  getSelectedDocuments(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/selected`, { headers: this.createHeaders() });
  }

  getSelectedCount(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/selected-count`, { headers: this.createHeaders() });
  }
}