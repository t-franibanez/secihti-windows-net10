import { CommonModule } from "@angular/common";
import { Component, inject } from "@angular/core";
import { Router } from "@angular/router";
import { BmbLoaderComponent } from "@ti-tecnologico-de-monterrey-oficial/ds-ng";
import { ApiResponse } from "../../models/response-api.model";
import { UserClaims } from "../../models/user-claims.model";
import { NotificationSnackbarService } from "../../services/notification-snackbar.service";
import { SessionService } from "../../services/session.service";

@Component({
  selector: "app-federation",
  templateUrl: "./federation.component.html",
  styleUrl: "./federation.component.css",
  imports: [CommonModule, BmbLoaderComponent],
})
export class FederationComponent {
  private sessionService = inject(SessionService);
  private notification = inject(NotificationSnackbarService);
  private router: Router = inject(Router);

  constructor() {
    // Se obtiene los userclaims y el jwt del api
    this.sessionService
      .getSessionUser()
      .subscribe((response: ApiResponse<UserClaims>) => {
        if (response.success) {
          const session = this.sessionService.createSessionUser(response);
          if (session) {
            this.router.navigate(["dashboard"]);
          } else {
            this.notification.openSnackBar(
              "Error al crear iniciar sesión",
              "warning",
            );
          }
        } else {
          this.notification.openSnackBar(response.message, "error");
        }
      });
  }

  onButtonPrimary(event: Event) {
    console.log("onButtonPrimary", event);
  }

  onButtonSecondary(event: Event) {
    console.log("onButtonSecondary", event);
  }
}
