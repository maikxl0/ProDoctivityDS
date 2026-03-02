import { Routes } from '@angular/router';
import { DocumentSearchComponent } from './pages/document-search/document-search';
import { ConfigurationComponent } from './pages/configuration/configuration';
import { ProcessingComponent } from './pages/processing/processing';

export const routes: Routes = [
  { path: '', redirectTo: '/documents', pathMatch: 'full' },
  { path: 'documents', component: DocumentSearchComponent },
  { path: 'configuration', component: ConfigurationComponent },
  { path: 'processing', component: ProcessingComponent },
];