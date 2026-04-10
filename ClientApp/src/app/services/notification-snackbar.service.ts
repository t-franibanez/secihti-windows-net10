import { Injectable, inject } from "@angular/core";
import { MatSnackBar } from "@angular/material/snack-bar";

@Injectable({
  providedIn: "root",
})
export class NotificationSnackbarService {
  private snackBar = inject(MatSnackBar);

  public openSnackBar(message: string, appearance: string) {
    this.snackBar.open(message, "Cerrar", {
      horizontalPosition: "end",
      verticalPosition: "top",
      duration: 6000,
    });
  }
}
