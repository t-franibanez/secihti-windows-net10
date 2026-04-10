import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { SessionService } from "../services/session.service";

export function roleGuard(...allowedRoles: string[]): CanActivateFn {
  return () => {
    const session = inject(SessionService).sessionData;
    const router = inject(Router);

    if (session && allowedRoles.includes(session.userType)) {
      return true;
    }

    return router.parseUrl("/dashboard");
  };
}

// Shortcut para mantener compatibilidad con rutas existentes
export const adminGuard: CanActivateFn = roleGuard("Administrador");
