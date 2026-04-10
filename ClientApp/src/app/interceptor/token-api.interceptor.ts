import {
  HttpRequest,
  HttpInterceptorFn,
  HttpHandlerFn,
  HttpErrorResponse,
} from "@angular/common/http";
import { inject } from "@angular/core";
import { SessionRefreshService } from "../services/session-refresh.service";
import { catchError, throwError } from "rxjs";

export const tokenApiInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) => {
  const sessionRefreshService = inject(SessionRefreshService);

  const modifiedReq = req.clone({
    setHeaders: {
      "Cache-Control": "no-cache",
      Pragma: "no-cache",
    },
  });

  return next(modifiedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 || error.status === 403) {
        sessionRefreshService.forceRefreshSession().subscribe();
      }

      return throwError(() => error);
    }),
  );
};
