import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

// Angular Material
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';

import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

// Servicios y modelos
import { DocumentService } from '../../data/services/document.service';
import { DocumentTypeService } from '../../data/services/document-type.service'; 
import { Document } from '../../core/models/document.model';
import { DocumentType } from '../../core/models/document-type.model';
import { SelectionService } from '../../data/services/selection.service';

@Component({
  selector: 'app-document-search',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    // Material
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatTableModule,
    MatCheckboxModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './document-search.html',
  styleUrls: ['./document-search.css']
})
export class DocumentSearchComponent implements OnInit {
  private fb = inject(FormBuilder);
  private documentService = inject(DocumentService);
  private documentTypeService = inject(DocumentTypeService);
  private selectionService = inject(SelectionService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  // Estado
  documents = signal<Document[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  error = signal<string | null>(null);
  selectedDocuments = signal<Set<string>>(new Set());
  documentTypes = signal<DocumentType[]>([])

  // Filtros
  filterForm: FormGroup = this.fb.group({
    name: [''],
    selectedTypeIds: [[]]
  });

  // Paginación
  pageIndex = signal(0);
  pageSize = signal(100);
  pageSizeOptions = [10, 25, 50, 100];

  // Columnas de la tabla
  displayedColumns = ['select', 'documentId', 'name', 'documentTypeName', 'createdAt'];

  // Getters para selección
  allSelected = computed(() => {
    const docs = this.documents();
    if (docs.length === 0) return false;
    const selected = this.selectedDocuments();
    return docs.every(d => selected.has(d.documentId));
  });

  someSelected = computed(() => {
    const docs = this.documents();
    if (docs.length === 0) return false;
    const selected = this.selectedDocuments();
    const count = docs.filter(d => selected.has(d.documentId)).length;
    return count > 0 && count < docs.length;
  });

  ngOnInit(): void {
    this.loadDocumentTypes();
    this.onSearch();

    // Cargar selección guardada
  this.selectionService.getSelectedDocuments().subscribe({
    next: (selectedIds) => {
      this.selectedDocuments.set(new Set(selectedIds));
    },
    error: (err) => console.error('Error al cargar selección', err)
  });
  }

  loadDocumentTypes(): void {
    this.documentTypeService.getDocumentTypes().subscribe({
      next: (types) => this.documentTypes.set(types),
      error: (err) => console.error('Error cargando tipos', err)
    });
  }

  onSearch(): void {
    const { name, selectedTypeIds } = this.filterForm.value;
    this.loading.set(true);
    this.error.set(null);

    this.documentService.searchDocuments(
      this.pageIndex(),
      this.pageSize(),
      name,
      selectedTypeIds // <-- array de IDs
    ).subscribe({
      next: (result) => {
        this.documents.set(result.documents);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Error al cargar documentos: ' + err.message);
        this.loading.set(false);
      }
    });
  }

  onClear(): void {
    this.filterForm.reset({ name: '', selectedTypeIds: [] });
    this.pageIndex.set(0);
    this.onSearch();
  }


  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.onSearch();
  }

  // Selección
  toggleSelection(docId: string): void {
    this.selectedDocuments.update(set => {
      const newSet = new Set(set);
      if (newSet.has(docId)) {
        newSet.delete(docId);
      } else {
        newSet.add(docId);
      }
      return newSet;
    });
    // Notificar al backend
  const isSelected = this.selectedDocuments().has(docId);
  if (isSelected) {
    this.selectionService.selectDocuments([docId]).subscribe({
      error: (err) => {
        console.error('Error al seleccionar', err);
        // Revertir cambio local si falla
        this.selectedDocuments.update(set => {
          const revert = new Set(set);
          revert.delete(docId);
          return revert;
        });
      }
    });
  } else {
    this.selectionService.clearAllSelection().subscribe({
      error: (err) => {
        console.error('Error al deseleccionar', err);
        this.selectedDocuments.update(set => {
          const revert = new Set(set);
          revert.add(docId);
          return revert;
        });
      }
    });
  }
}

  isSelected(docId: string): boolean {
    return this.selectedDocuments().has(docId);
  }

  selectAll(): void {
  const allIds = this.documents().map(d => d.documentId);
  // Actualizar local
  this.selectedDocuments.update(set => new Set([...set, ...allIds]));
  // Notificar al backend
  this.selectionService.selectDocuments(allIds).subscribe({
    error: (err) => {
      console.error('Error al seleccionar todos', err);
      // Revertir: eliminar los que se intentaron agregar
      this.selectedDocuments.update(set => {
        const revert = new Set(set);
        allIds.forEach(id => revert.delete(id));
        return revert;
      });
    }
  });
}

processSelected(): void {
  if (this.selectedDocuments().size === 0) {
    alert('Selecciona al menos un documento');
    return;
  }
  // Navegar a la ruta de progreso
  this.router.navigate(['/processing']);
}

clearAllSelection(): void {
  const selectedCount = this.selectedDocuments().size;
  if (selectedCount === 0) {
    this.snackBar.open('No hay documentos seleccionados', 'Cerrar', { duration: 2000 });
    return;
  }


  this.selectionService.clearAllSelection().subscribe({
    next: () => {
      this.selectedDocuments.set(new Set()); // Limpia el estado local
      this.snackBar.open('Selección eliminada', 'OK', { duration: 2000 });
    },
    error: (err) => {
      console.error('Error al limpiar selección', err);
      this.snackBar.open('Error al limpiar selección', 'Cerrar', { duration: 3000 });
    }
  });
}

  // Método para el checkbox de cabecera
  toggleAll(): void {
    if (this.allSelected()) {
      this.clearAllSelection();
    } else {
      this.selectAll();
    }
  }
}