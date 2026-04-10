import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Configuration, SaveConfigurationRequest } from '../../core/models/configuration.model';
import { ApiCredentials } from '../../core/models/api-credentials.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ConfigurationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/configuration`;

  /** Obtiene la configuración activa (campos sensibles ocultos) */
  getConfiguration(): Observable<Configuration> {
    return this.http.get<Configuration>(this.apiUrl);
  }

  /** Guarda la configuración completa */
  saveConfiguration(request: SaveConfigurationRequest): Observable<void> {
    return this.http.put<void>(this.apiUrl, request);
  }

  /** Prueba la conexión con las credenciales proporcionadas */
  testConnection(credentials: ApiCredentials): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/test-connection`, credentials);
  }

  /** Exporta la configuración actual (con credenciales visibles) */
  exportConfiguration(): Observable<Configuration> {
    return this.http.get<Configuration>(`${this.apiUrl}/export`);
  }

  /** Importa una configuración desde un JSON */
  importConfiguration(config: Configuration): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/import`, config);
  }
}