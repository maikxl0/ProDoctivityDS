import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ActivityLogEntry } from '../../core/models/activity-log-entry.model';

@Injectable({ providedIn: 'root' })
export class LogsService {
  private http = inject(HttpClient);
  private baseUrl = '/api/logs';

  getLogs(level?: string, limit: number = 100): Observable<ActivityLogEntry[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (level) {
      params = params.set('level', level);
    }
    return this.http.get<ActivityLogEntry[]>(this.baseUrl, { params });
  }

  getLogsByDocument(documentId: string, limit: number = 100): Observable<ActivityLogEntry[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<ActivityLogEntry[]>(`${this.baseUrl}/document/${documentId}`, { params });
  }

  clearLogs(): Observable<void> {
    return this.http.delete<void>(this.baseUrl);
  }
}