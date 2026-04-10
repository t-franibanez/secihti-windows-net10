import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { UserClaims } from "../models/user-claims.model";
import { BehaviorSubject, Observable } from "rxjs";
import { ApiResponse } from "../models/response-api.model";

@Injectable({
  providedIn: "root",
})
export class SessionService {
  private usuarioSubject: BehaviorSubject<UserClaims>;
  private EMPTY!: UserClaims;
  private http = inject(HttpClient);

  constructor() {
    this.usuarioSubject = new BehaviorSubject<UserClaims>(
      JSON.parse(sessionStorage.getItem("sessionUser") ?? "{}"),
    );
  }

  public getUserClaims() {
    const url = `Home/GetUserClaims/`;
    return this.http.get<UserClaims>(url);
  }

  public logout() {
    sessionStorage.removeItem("sessionUser");
    this.usuarioSubject.next(this.EMPTY);
    const url = `Auth/Logout/`;
    window.location.href = url;
  }

  public getSessionUser(): Observable<ApiResponse<UserClaims>> {
    return this.http.get<ApiResponse<UserClaims>>(`Home/GetUserClaims`);
  }

  public get sessionData(): UserClaims {
    return this.usuarioSubject.value;
  }

  public createSessionUser(response: ApiResponse<UserClaims>): boolean {
    try {
      const user: UserClaims = response.data;
      sessionStorage.setItem("sessionUser", JSON.stringify(user));
      this.usuarioSubject.next(user);
      return true;
    } catch (error) {
      console.log(error);
      return false;
    }
  }
}
