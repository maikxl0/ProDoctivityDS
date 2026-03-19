import { Routes } from '@angular/router';
import { DocumentSearchComponent } from './pages/document-search/document-search';
import { ConfigurationComponent } from './pages/configuration/configuration';
import { ProcessingComponent } from './pages/processing/processing';
import { LogsViewerComponent } from './pages/logs-viewer/logs-viewer';
import { authGuard } from './pages/login/auth.guard';
import { LoginComponent } from './pages/login/login';
import { DuplicateCheckComponent } from './pages/duplicates/duplicate-check';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'documents', component: DocumentSearchComponent, canActivate: [authGuard] },
  { path: 'duplicates', component: DuplicateCheckComponent, canActivate: [authGuard] },
  { path: 'configuration', component: ConfigurationComponent },
  { path: 'processing', component: ProcessingComponent, canActivate: [authGuard] },
  {path: 'logs', component: LogsViewerComponent },
  { path: '', redirectTo: '/documents', pathMatch: 'full' },
  { path: '**', redirectTo: '/documents' }
];
