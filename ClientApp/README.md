# ClientApp — Angular Frontend

Angular standalone app con [Bamboo Design System (ds-ng)](https://www.npmjs.com/package/@ti-tecnologico-de-monterrey-oficial/ds-ng).

## Desarrollo local

```bash
npm install --legacy-peer-deps
npm start
```

La app se sirve en `http://localhost:4200/` y se recarga automáticamente con cambios.

## Build por ambiente

```bash
npm run build:DEVL   # Development
npm run build:PPRD   # Pre-produccion
npm run build:PROD   # Produccion
```

Los artefactos se generan en `dist/browser/`.
