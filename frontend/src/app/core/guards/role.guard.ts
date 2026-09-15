import { inject } from '@angular/core';
import { CanActivateChildFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const roleGuard: CanActivateChildFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const roles = route.data['roles'] as readonly string[] | undefined;

  if (!roles || roles.length === 0) {
    return true;
  }

  return authService.hasRole(roles)
    ? true
    : router.createUrlTree(['/forbidden']);
};
