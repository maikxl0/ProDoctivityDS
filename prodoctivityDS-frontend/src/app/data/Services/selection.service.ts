import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class SelectionService {
  private http = inject(HttpClient);
  private baseUrl = '/api/selection';
  private sessionIdKey = 'x-session-id';

  private getSessionId(): string | null {
    return localStorage.getItem(this.sessionIdKey);
  }

  private setSessionId(id: string): void {
    localStorage.setItem(this.sessionIdKey, id);
  }

  private request<T>(method: string, url: string, body?: any): Observable<T> {
    let headers = new HttpHeaders();
    const sessionId = this.getSessionId();
    if (sessionId) {
      headers = headers.set('X-Session-Id', sessionId);
    }

    // Realizar la petición y observar la respuesta completa para leer headers
    return this.http.request<T>(method, url, { body, headers, observe: 'response' }).pipe(
      tap(response => {
        const newSessionId = response.headers.get('X-Session-Id');
        if (newSessionId) {
          this.setSessionId(newSessionId);
        }
      }),
      map(response => response.body as T)
    );
  }

  selectDocuments(documentIds: string[]): Observable<any> {
    return this.request('POST', `${this.baseUrl}/select`, documentIds);
  }

  deselectDocuments(documentIds: string[]): Observable<any> {
    return this.request('POST', `${this.baseUrl}/deselect`, documentIds);
  }

  getSelectedDocuments(): Observable<string[]> {
    return this.request('GET', `${this.baseUrl}/selected`);
  }

  getSelectedCount(): Observable<number> {
    return this.request('GET', `${this.baseUrl}/selected-count`);
  }
}