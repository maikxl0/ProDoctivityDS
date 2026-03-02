import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { Subscription, interval, switchMap, tap, catchError, of } from 'rxjs';
import { ProcessRequest } from '../../core/models/process-request.model';
import { ProcessingService } from '../../data/Services/processing.service';
import { SelectionService } from '../../data/Services/selection.service';
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

  selectedCount = 0;
  selectedDocumentIds: string[] = [];

  // Estado del proceso
  isProcessing = false;
  progress: ProcessProgress | null = null;
  private pollingSubscription?: Subscription;
  private sessionId?: string;

  ngOnInit(): void {
    this.loadSelectedDocuments();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private loadSelectedDocuments(): void {
    this.selectionService.getSelectedDocuments().subscribe(ids => {
      this.selectedDocumentIds = ids;
      this.selectedCount = ids.length;
    });
  }

  startProcessing(): void {
    if (this.selectedDocumentIds.length === 0) {
      this.snackBar.open('No hay documentos seleccionados', 'Cerrar', { duration: 3000 });
      return;
    }

    const request: ProcessRequest = {
      documentIds: this.selectedDocumentIds,
      // Aquí podrías obtener opciones adicionales de configuración si lo deseas
    };

    this.isProcessing = true;
    this.processingService.startProcessing(request).subscribe({
      next: (response) => {
        this.sessionId = response.sessionId;
        this.snackBar.open('Procesamiento iniciado', 'OK', { duration: 2000 });
        this.startPolling();
      },
      error: (err) => {
        this.isProcessing = false;
        this.snackBar.open('Error al iniciar procesamiento', 'Cerrar', { duration: 3000 });
        console.error(err);
      }
    });
  }

  private startPolling(): void {
    this.pollingSubscription = interval(2000).pipe(
      switchMap(() => this.processingService.getProgress()),
      tap(progress => {
        this.progress = progress;
        if (progress.status === 'Completado' || progress.processed >= progress.total) {
          this.stopPolling();
          this.isProcessing = false;
          this.snackBar.open('Procesamiento completado', 'OK', { duration: 3000 });
        }
      }),
      catchError(err => {
        console.error('Error al obtener progreso', err);
        if (err.status === 404) {
          this.stopPolling();
          this.isProcessing = false;
        }
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
      },
      error: (err) => console.error(err)
    });
  }
}