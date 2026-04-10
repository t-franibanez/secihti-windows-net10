const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

// Extract the port from command line arguments
const args = process.argv.slice(2);
const portArgIndex = args.indexOf('--port');
let port = portArgIndex !== -1 ? args[portArgIndex + 1] : '4200';

console.log(`Starting Angular dev server adapter on port ${port}...`);

const ng = process.platform === 'win32' ? 'ng.cmd' : 'ng';

// MODIFICACIÓN 1: Agregamos --poll=1000 forzosamente para Docker
// y nos aseguramos de que escuche en todas las interfaces si fuera necesario (aunque el proxy de .net maneja localhost)
const child = spawn(ng, ['serve', '--poll=1000', ...args], {
  stdio: ['inherit', 'pipe', 'pipe'],
  shell: true
});

// MODIFICACIÓN 2: .NET Core Middleware espera específicamente esta cadena
// "open your browser on http://localhost:[port]"
const magicString = `open your browser on http://localhost:${port}`;

child.stdout.on('data', (data) => {
  const output = data.toString();
  process.stdout.write(output);

  // Detectamos el nuevo output de Angular 17+ (Vite/Esbuild)
  // Ejemplo: ➜  Local:   http://localhost:46699/
  if (output.includes('Local:') && output.includes(`http://localhost:${port}`)) {
    console.log(magicString);
  }
});

child.stderr.on('data', (data) => {
  process.stderr.write(data.toString());
});

child.on('close', (code) => {
  process.exit(code);
});
