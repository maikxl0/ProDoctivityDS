import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatTabsModule],
  template: `
    <mat-toolbar color="primary">
      <span>ProDoctivity Processor</span>
    </mat-toolbar>
    <nav mat-tab-nav-bar [tabPanel]="tabPanel">
      <a mat-tab-link routerLink="/documents" routerLinkActive #rla1="routerLinkActive" [active]="rla1.isActive">
        Buscar Documentos
      </a>
      <a mat-tab-link routerLink="/processing" routerLinkActive #rla2="routerLinkActive" [active]="rla2.isActive">
        Procesar
      </a>
      <a mat-tab-link routerLink="/configuration" routerLinkActive #rla3="routerLinkActive" [active]="rla3.isActive">
        Configuración
      </a>
    </nav>
    <mat-tab-nav-panel #tabPanel></mat-tab-nav-panel>
    <main>
      <router-outlet></router-outlet>
    </main>
  `,
  styles: [`
    main { padding: 20px; }
  `]
})
export class AppComponent { }