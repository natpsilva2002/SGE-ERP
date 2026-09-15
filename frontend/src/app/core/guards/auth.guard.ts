import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (authService.getCurrentUser()) {
    return true;
  }

  return authService.refreshCurrentUser().pipe(
    map(() => true),
    catchError(() => {
      authService.logout(false);
      return of(router.createUrlTree(['/login']));
    })
  );
};
