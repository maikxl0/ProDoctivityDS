import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from './data/service/auth.service'; 
import { CommonModule } from '@angular/common';
import { map } from 'rxjs';


@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatTabsModule, MatIconModule, MatTooltipModule
  ],
  template: `
    <mat-toolbar color="primary">
      <span>ProDoctivity Cleaner</span>
      <span class="spacer"></span>
      
      <button mat-icon-button routerLink="/logs" matTooltip="Ver Logs">
        <mat-icon>list</mat-icon>
      </button>

      <button mat-icon-button (click)="logout()" *ngIf="isAuthenticated$ | async" matTooltip="Cerrar sesion">
        <mat-icon>exit_to_app</mat-icon>
      </button>
      
    </mat-toolbar>
    <nav mat-tab-nav-bar [tabPanel]="tabPanel">
      <a mat-tab-link routerLink="/documents" routerLinkActive #rla1="routerLinkActive" [active]="rla1.isActive">
        Buscar Documentos
      </a>
      <a mat-tab-link routerLink="/processing" routerLinkActive #rla2="routerLinkActive" [active]="rla2.isActive">
        Procesar
      </a>
      <a mat-tab-link routerLink="/duplicates" routerLinkActive #rla4="routerLinkActive" [active]="rla4.isActive">
        Duplicados
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
    .spacer { flex: 1 1 auto; }
    main { padding: 20px; }
  `]
}
)

export class AppComponent { 
  private authService = inject(AuthService);
  isAuthenticated$ = this.authService.checkAuthStatus().pipe(
    map(status => status.isAuthenticated)
  );

  logout(): void {
    this.authService.logout().subscribe({
      next: () => console.log('Sesión cerrada'),
      error: (err) => console.error('Error al cerrar sesión', err)
    });
  }
}