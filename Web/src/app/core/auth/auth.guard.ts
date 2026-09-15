import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/']);
};

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isAdmin() || authService.rol() === 'Auditor') {
    return true;
  }

  if (authService.isSuperAdmin()) {
    return router.createUrlTree(['/app/clientes']);
  }

  if (authService.rol() === 'Contador') {
    return router.createUrlTree(['/app/reportes']);
  }

  if (authService.isJornalero()) {
    return router.createUrlTree(['/app/mis-actividades']);
  }

  return router.createUrlTree(['/app/mis-cultivos']);
};

export const reportesGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isAdmin() || authService.rol() === 'Auditor' || authService.rol() === 'Contador') {
    return true;
  }

  if (authService.isSuperAdmin()) {
    return router.createUrlTree(['/app/clientes']);
  }

  if (authService.isJornalero()) {
    return router.createUrlTree(['/app/mis-actividades']);
  }

  return router.createUrlTree(['/app/mis-cultivos']);
};

export const socioGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isSocio()) {
    return true;
  }

  if (authService.isSuperAdmin()) {
    return router.createUrlTree(['/app/clientes']);
  }

  if (authService.isJornalero()) {
    return router.createUrlTree(['/app/mis-actividades']);
  }

  return router.createUrlTree(['/app/fincas']);
};

export const superAdminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isSuperAdmin()) {
    return true;
  }

  if (authService.isSocio()) {
    return router.createUrlTree(['/app/mis-cultivos']);
  }

  if (authService.isJornalero()) {
    return router.createUrlTree(['/app/mis-actividades']);
  }

  return router.createUrlTree(['/app/fincas']);
};

export const jornaleroGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isJornalero()) {
    return true;
  }

  if (authService.isSuperAdmin()) {
    return router.createUrlTree(['/app/clientes']);
  }

  if (authService.isSocio()) {
    return router.createUrlTree(['/app/mis-cultivos']);
  }

  if (authService.rol() === 'Contador') {
    return router.createUrlTree(['/app/reportes']);
  }

  return router.createUrlTree(['/app/fincas']);
};
