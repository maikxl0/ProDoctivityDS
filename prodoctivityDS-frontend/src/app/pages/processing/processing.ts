import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { Subscription, interval, switchMap, tap, catchError, of } from 'rxjs';
import { ProcessRequest } from '../../core/models/process-request.model';
import { ProcessingService } from '../../data/services/processing.service';
import { SelectionService } from '../../data/services/selection.service';
import { ProcessProgress } from '../../core/models/process-progress.model';

@Component({
  selector: 'app-processing',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatProgressBarModule,
    MatCardModule,
    MatIconModule,
    MatSnackBarModule,
  ],
  templateUrl: './processing.html',
  styleUrls: ['./processing.css']
})
export class ProcessingComponent implements OnInit, OnDestroy {
  private processingService = inject(ProcessingService);
  private selectionService = inject(SelectionService);
  private snackBar = inject(MatSnackBar);
  private cdr = inject(ChangeDetectorRef);

  selectedCount = 0;
  selectedDocumentIds: string[] = [];

  isProcessing = false;
  progress: ProcessProgress | null = null;
  private pollingSubscription?: Subscription;
  private sessionId?: string;

  ngOnInit(): void {
    this.loadSelectedDocuments();
    this.checkExistingProgress();
  }

  ngOnDestroy(): void {
    this.stopPolling();
    // Si el progreso está completado al salir, lo limpiamos
    if (this.progress?.status === 'Completado') {
      this.clearProgress();
    }
  }

  private checkExistingProgress(): void {
    this.processingService.getProgress().subscribe({
      next: (progress) => {
        if (progress) {
          // Solo mostrar si el proceso aún está en curso
          if (progress.status !== 'Completado') {
            this.progress = progress;
            this.isProcessing = true;
            this.startPolling();
          } else {
            // Si ya está completado, lo limpiamos automáticamente
            this.clearProgress();
          }
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        if (err.status !== 404) {
          console.error('Error al verificar progreso', err);
        }
      }
    });
  }

  private loadSelectedDocuments(): void {
    this.selectionService.getSelectedDocuments().subscribe(ids => {
      this.selectedDocumentIds = ids;
      this.selectedCount = ids.length;
      this.cdr.detectChanges();
    });
  }

  startProcessing(): void {
    if (this.isProcessing) {
      console.log('Ya hay un proceso en curso');
      return;
    }

    if (this.selectedDocumentIds.length === 0) {
      this.snackBar.open('No hay documentos seleccionados', 'Cerrar', { duration: 3000 });
      return;
    }

    const request: ProcessRequest = {
      documentIds: this.selectedDocumentIds,
    };

    this.isProcessing = true;
    this.progress = null;
    this.cdr.detectChanges();

    this.processingService.startProcessing(request).subscribe({
      next: (response) => {
        this.sessionId = response.sessionId;
        this.snackBar.open('Procesamiento iniciado', 'OK', { duration: 2000 });
        this.startPolling();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isProcessing = false;
        this.snackBar.open('Error al iniciar procesamiento', 'Cerrar', { duration: 3000 });
        console.error(err);
        this.cdr.detectChanges();
      }
    });
  }

  private startPolling(): void {
    console.log('Iniciando polling cada 2 segundos');
    this.pollingSubscription = interval(2000).pipe(
      tap(() => console.log('Enviando petición de progreso...')),
      switchMap(() => this.processingService.getProgress()),
      tap(progress => {
        console.log('Progreso recibido:', progress);
        if (progress) {
          this.progress = progress;
          if (progress.status === 'Completado' || progress.processed >= progress.total) {
            console.log('Procesamiento completado');
            this.stopPolling();
            this.isProcessing = false;
            this.snackBar.open('Procesamiento completado', 'OK', { duration: 3000 });
          }
          this.cdr.detectChanges();
        }
      }),
      catchError(err => {
        console.error('Error al obtener progreso', err);
        if (err.status === 404) {
          console.log('Progreso no encontrado (404), reintentando...');
          return of(null);
        }
        this.stopPolling();
        this.isProcessing = false;
        this.cdr.detectChanges();
        return of(null);
      })
    ).subscribe();
  }

  private stopPolling(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = undefined;
    }
  }

  cancelProcessing(): void {
    this.processingService.cancelProcessing().subscribe({
      next: () => {
        this.snackBar.open('Cancelación solicitada', 'OK', { duration: 2000 });
        setTimeout(() => {
          this.stopPolling();
          this.isProcessing = false;
          this.progress = null;
          this.cdr.detectChanges();
        }, 1000);
      },
      error: (err) => {
        this.snackBar.open('Error al cancelar', 'Cerrar', { duration: 3000 });
        console.error(err);
      }
    });
  }

  clearProgress(): void {
    this.processingService.clearProgress().subscribe({
      next: () => {
        this.progress = null;
        this.isProcessing = false;
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  get percentComplete(): number {
    if (!this.progress || this.progress.total === 0) return 0;
    const completed = this.progress.processed + this.progress.errors + this.progress.skipped;
    return (completed / this.progress.total) * 100;
  }
}