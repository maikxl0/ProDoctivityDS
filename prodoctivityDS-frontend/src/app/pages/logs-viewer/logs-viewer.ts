import { Component, OnInit, OnDestroy, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { Subscription, interval, startWith, switchMap } from 'rxjs';
import { LogsService } from '../../data/service/logs.service';
import { ActivityLogEntry } from '../../core/models/activity-log-entry.model';

@Component({
  selector: 'app-logs-viewer',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule,
    MatCheckboxModule,
  ],
  templateUrl: './logs-viewer.html',
  styleUrls: ['./logs-viewer.css']
})
export class LogsViewerComponent implements OnInit, OnDestroy {
  private logsService = inject(LogsService);
  private snackBar = inject(MatSnackBar);

  logs: ActivityLogEntry[] = [];
  displayedColumns: string[] = ['timestamp', 'level', 'category', 'message', 'documentId'];
  loading = false;
  autoRefresh = false;
  refreshInterval = 5; // segundos
  private refreshSubscription?: Subscription;

  // Filtros
  selectedLevel: string = '';
  levels: string[] = ['INFO', 'SUCCESS', 'WARNING', 'ERROR', 'DEBUG'];
  searchDocumentId: string = '';
  limit: number = 100;

  // Paginación local (opcional)
  pageSize = 50;
  pageIndex = 0;

  ngOnInit(): void {
    this.loadLogs();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  loadLogs(): void {
    this.loading = true;
    const observable = this.searchDocumentId
      ? this.logsService.getLogsByDocument(this.searchDocumentId, this.limit)
      : this.logsService.getLogs(this.selectedLevel, this.limit);

    observable.subscribe({
      next: (data) => {
        this.logs = data;
        this.loading = false;
        // Resetear paginación al cargar nuevos datos
        this.pageIndex = 0;
      },
      error: (err) => {
        console.error('Error cargando logs', err);
        this.snackBar.open('Error al cargar logs', 'Cerrar', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.loadLogs();
  }

  clearFilters(): void {
    this.selectedLevel = '';
    this.searchDocumentId = '';
    this.limit = 100;
    this.loadLogs();
  }

  toggleAutoRefresh(): void {
    if (this.autoRefresh) {
      this.startAutoRefresh();
    } else {
      this.stopAutoRefresh();
    }
  }

  private startAutoRefresh(): void {
    this.stopAutoRefresh(); // asegurar que no haya duplicados
    this.refreshSubscription = interval(this.refreshInterval * 1000)
      .pipe(startWith(0))
      .subscribe(() => {
        this.loadLogs();
      });
  }

  private stopAutoRefresh(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
      this.refreshSubscription = undefined;
    }
  }

  clearAllLogs(): void {
    if (!confirm('¿Estás seguro de eliminar todos los logs? Esta acción no se puede deshacer.')) {
      return;
    }
    this.logsService.clearLogs().subscribe({
      next: () => {
        this.snackBar.open('Logs eliminados', 'OK', { duration: 2000 });
        this.logs = [];
      },
      error: (err) => {
        console.error('Error al eliminar logs', err);
        this.snackBar.open('Error al eliminar logs', 'Cerrar', { duration: 3000 });
      }
    });
  }

  formatTimestamp(timestamp: any): string {
    if (!timestamp) return '';
    const date = new Date(timestamp);
    return date.toLocaleString();
  }

  getLevelClass(level: string): string {
    return `level-${level.toLowerCase()}`;
  }

  // Método para manejar cambios de página
  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    // Como la paginación es local, no necesitamos recargar datos,
    // solo actualizar índices. La tabla se actualizará automáticamente
    // porque estamos usando [dataSource]="logs" sin paginación remota.
    // Si quisieras paginación remota, aquí llamarías a loadLogs con los nuevos parámetros.
  }

  // Getter para obtener los logs de la página actual (para paginación local)
  get paginatedLogs(): ActivityLogEntry[] {
    const start = this.pageIndex * this.pageSize;
    const end = start + this.pageSize;
    return this.logs.slice(start, end);
  }
}