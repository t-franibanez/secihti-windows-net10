import { Routes } from "@angular/router";
import { FederationComponent } from "./components/federation/federation.component";
import { LayoutComponent } from "./components/layout/layout.component";
import { NotFoundPageComponent } from "./components/not-found-page/not-found-page.component";
import { userSessionGuard } from "./guard/user-session.guard";

export const routes: Routes = [
  { path: "", component: FederationComponent, pathMatch: "full" },
  {
    path: "",
    component: LayoutComponent,
    canActivate: [userSessionGuard],
    children: [ 
    ],
  },
  {
    path: "**",
    component: NotFoundPageComponent,
  },
];
