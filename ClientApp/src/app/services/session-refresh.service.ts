import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { BehaviorSubject, Observable, of, timer, Subscription } from "rxjs";
import { catchError, map, tap, take, finalize } from "rxjs/operators";
import { SessionService } from "./session.service";
import { UserClaims } from "../models/user-claims.model";
import { ApiResponse } from "../models/response-api.model";

@Injectable({
  providedIn: "root",
})
export class SessionRefreshService {
  private refreshTimer: Subscription | null = null;
  private isRefreshing = false;
  private refreshInterval = 4 * 60 * 1000;
  private lastRefreshTime = 0;

  private sessionValidSubject = new BehaviorSubject<boolean>(true);
  public sessionValid$ = this.sessionValidSubject.asObservable();

  private http = inject(HttpClient);
  private sessionService = inject(SessionService);

  constructor() {
    this.startAutoRefresh();
  }

  private startAutoRefresh(): void {
    this.refreshTimer = timer(0, 60000).subscribe(() => {
      this.checkAndRefreshSession();
    });
  }

  private checkAndRefreshSession(): void {
    const now = Date.now();
    const timeSinceLastRefresh = now - this.lastRefreshTime;

    if (timeSinceLastRefresh >= this.refreshInterval) {
      this.refreshSession().subscribe();
    }
  }

  private refreshSession(): Observable<boolean> {
    if (this.isRefreshing) {
      return this.sessionValid$.pipe(take(1));
    }

    this.isRefreshing = true;

    return this.http
      .get<ApiResponse<UserClaims>>("Home/GetUserClaims")
      .pipe(
        tap((response) => {
          if (response?.data) {
            this.sessionService.createSessionUser(response);
            this.lastRefreshTime = Date.now();
            this.sessionValidSubject.next(true);
          }
        }),
        map(() => true),
        catchError(() => {
          this.sessionValidSubject.next(false);
          return of(false);
        }),
        finalize(() => {
          this.isRefreshing = false;
        }),
      );
  }

  public forceRefreshSession(): Observable<boolean> {
    return this.refreshSession();
  }

  public stopAutoRefresh(): void {
    if (this.refreshTimer) {
      this.refreshTimer.unsubscribe();
      this.refreshTimer = null;
    }
  }

  public isSessionValid(): boolean {
    return this.sessionValidSubject.value;
  }

  public destroy(): void {
    this.stopAutoRefresh();
    this.sessionValidSubject.complete();
  }
}
