import { Component } from "@angular/core";
import { RouterModule } from "@angular/router";
import { BmbButtonDirective } from "@ti-tecnologico-de-monterrey-oficial/ds-ng";

@Component({
  selector: "app-not-found-page",
  templateUrl: "./not-found-page.component.html",
  styleUrl: "./not-found-page.component.css",
  imports: [RouterModule, BmbButtonDirective],
})
export class NotFoundPageComponent {}
