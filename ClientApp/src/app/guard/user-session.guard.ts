import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { SessionService } from "../services/session.service";

export const userSessionGuard: CanActivateFn = () => {
  const sessionService = inject(SessionService);
  const router = inject(Router);
  const user = sessionService.sessionData;
  if (user !== undefined && Object.entries(user).length !== 0) {
    return true;
  }
  return router.parseUrl("/");
};
