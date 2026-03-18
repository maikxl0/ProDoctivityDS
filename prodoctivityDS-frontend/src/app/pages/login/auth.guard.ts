import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../data/services/auth.service';
import { map } from 'rxjs';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.checkAuthStatus().pipe(
    map(status => {
      if (status.isAuthenticated) {
        return true;
      } else {
        router.navigate(['/login']);
        return false;
      }
    })
  );
};