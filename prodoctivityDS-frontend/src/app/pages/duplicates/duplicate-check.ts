import { Component, inject, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { firstValueFrom } from 'rxjs';
import { DuplicateService } from '../../data/services/duplicate.service';
import { ProcessingService } from '../../data/services/processing.service';
import { DuplicateCheckResponse, DuplicateGroup } from '../../core/models/duplicate.models';

@Component({
  selector: 'app-duplicate-check',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatExpansionModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './duplicate-check.html',
  styleUrls: ['./duplicate-check.css']
})
export class DuplicateCheckComponent {
  private duplicateService = inject(DuplicateService);
  private processingService = inject(ProcessingService);
  private snackBar = inject(MatSnackBar);
  private cdr = inject(ChangeDetectorRef);
  private ngZone = inject(NgZone);

  cedula = '';
  loading = false;
  deleting = false;
  error: string | null = null;
  result: DuplicateCheckResponse | null = null;
  infoMessage: string | null = null;

  async checkDuplicates(): Promise<void> {
    if (!this.cedula.trim()) {
      this.snackBar.open('Ingrese una cédula', 'Cerrar', { duration: 2000 });
      return;
    }

    this.loading = true;
    this.error = null;
    this.infoMessage = null;
    this.result = null;

    try {
      const response = await firstValueFrom(
        this.duplicateService.checkByCedula({ cedula: this.cedula })
      );
      this.ngZone.run(() => {
        this.result = response;
        if (response.groups.length === 0) {
          this.infoMessage = 'No se encontraron duplicados para la cédula ingresada.';
        }
        this.loading = false;
        this.cdr.detectChanges();
      });
    } catch (err: any) {
      this.ngZone.run(() => {
        this.error = 'Error al buscar duplicados: ' + err.message;
        this.loading = false;
        this.cdr.detectChanges();
      });
    }
  }

  async deleteDocument(docId: string): Promise<void> {
    if (this.deleting) return;
    if (!confirm('¿Estás seguro de que deseas eliminar este documento?')) return;

    this.deleting = true;
    try {
      await firstValueFrom(this.processingService.deleteDocument(docId));
      this.snackBar.open('Documento eliminado', 'OK', { duration: 2000 });
      await this.checkDuplicates(); // Recargar la lista
    } catch (err: any) {
      this.snackBar.open('Error al eliminar: ' + err.message, 'Cerrar', { duration: 3000 });
    } finally {
      this.deleting = false;
    }
  }

  async deleteAllDuplicatesInGroup(group: DuplicateGroup): Promise<void> {
    if (this.deleting) return;
    const docsToDelete = group.documents.slice(1); // Conservar el primero
    if (docsToDelete.length === 0) {
      this.snackBar.open('No hay documentos duplicados para eliminar en este grupo', 'OK', { duration: 2000 });
      return;
    }

    const confirmMessage = `¿Eliminar ${docsToDelete.length} documento(s) duplicado(s) de este grupo? Se conservará "${group.documents[0].name}".`;
    if (!confirm(confirmMessage)) return;

    this.deleting = true;
    let successCount = 0;
    try {
      for (const doc of docsToDelete) {
        await firstValueFrom(this.processingService.deleteDocument(doc.documentId));
        successCount++;
      }
      this.snackBar.open(`${successCount} documento(s) eliminado(s)`, 'OK', { duration: 2000 });
      await this.checkDuplicates(); // Recargar
    } catch (err: any) {
      this.snackBar.open(`Error después de ${successCount} eliminaciones: ${err.message}`, 'Cerrar', { duration: 3000 });
    } finally {
      this.deleting = false;
    }
  }

  async deleteAllDuplicates(): Promise<void> {
    if (this.deleting || !this.result) return;

    // Recolectar todos los documentos a eliminar (todos excepto el primero de cada grupo)
    const allDocsToDelete = this.result.groups.flatMap(group => group.documents.slice(1));
    if (allDocsToDelete.length === 0) {
      this.snackBar.open('No hay documentos duplicados para eliminar', 'OK', { duration: 2000 });
      return;
    }

    const confirmMessage = `¿Eliminar todos los ${allDocsToDelete.length} documentos duplicados? Se conservará uno por grupo.`;
    if (!confirm(confirmMessage)) return;

    this.deleting = true;
    let successCount = 0;
    try {
      for (const doc of allDocsToDelete) {
        await firstValueFrom(this.processingService.deleteDocument(doc.documentId));
        successCount++;
      }
      this.snackBar.open(`${successCount} documento(s) eliminado(s)`, 'OK', { duration: 2000 });
      await this.checkDuplicates(); // Recargar
    } catch (err: any) {
      this.snackBar.open(`Error después de ${successCount} eliminaciones: ${err.message}`, 'Cerrar', { duration: 3000 });
    } finally {
      this.deleting = false;
    }
  }
}