import { bootstrapApplication } from "@angular/platform-browser";
import { appConfig } from "./app/app.config";
import { AppComponent } from "./app/app.component";
import { environment } from "./environments/environment";
import { enableProdMode } from "@angular/core";

if (environment.production) {
  enableProdMode();
}

// Parche temporal para ocultar advertencias de deprecación (NG0955 / allowSignalWrites)
// que provienen de la directiva @ti-tecnologico-de-monterrey-oficial/ds-ng (Bamboo UI)
const originalWarn = console.warn;
console.warn = (...args) => {
  if (
    typeof args[0] === 'string' &&
    args[0].includes('The \'allowSignalWrites\' flag is deprecated')
  ) {
    return; // Silenciamos esta advertencia específica de Bamboo Server
  }
  originalWarn.apply(console, args);
};

bootstrapApplication(AppComponent, appConfig).catch((err) =>
  console.error(err),
);
