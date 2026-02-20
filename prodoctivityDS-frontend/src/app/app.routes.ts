import { Routes } from '@angular/router';
import { DocumentSearchComponent } from './pages/document-search/document-search';

export const routes: Routes = [
  { path: '', redirectTo: '/documents', pathMatch: 'full' },
  { path: 'documents', component: DocumentSearchComponent },
  // otras rutas (configuración, procesamiento, etc.)
];