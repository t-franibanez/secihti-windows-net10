import { ChangeDetectionStrategy, Component } from "@angular/core";
import { RouterModule } from "@angular/router";
import {
  BmbContainerComponent,
  SidebarElement,
} from "@ti-tecnologico-de-monterrey-oficial/ds-ng";

@Component({
  selector: "app-layout",
  standalone: true,
  templateUrl: "./layout.component.html",
  styleUrl: "./layout.component.css",
  imports: [
    RouterModule,
    BmbContainerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LayoutComponent {
  // Configura los elementos del sidebar para tu proyecto
  sidebarElements: SidebarElement[][] = [
    [
      { id: 1, icon: "dashboard", title: "Dashboard", link: "/dashboard" },
    ],
  ];
}