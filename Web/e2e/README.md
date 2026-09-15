# E2E (Playwright)

Pruebas de extremo a extremo contra la app real corriendo en el navegador (no mocks).

## Requisitos antes de correrlas

Ambos servidores deben estar corriendo:

```bash
# Terminal 1 — backend
cd FarmControlAPI/FarmControlAPI
dotnet run --urls http://localhost:5279

# Terminal 2 — frontend
cd Web
npm start   # http://localhost:4200
```

## Correr las pruebas

```bash
npm run e2e            # corre todo el suite (headless)
npx playwright test --ui   # modo interactivo, ver cada paso
npm run e2e:report     # abre el último reporte HTML (screenshots/video de fallos)
```

## Importante: comparten la base de datos con tus pruebas manuales

Las pruebas usan la MISMA base SQLite (`farmcontrol.db`) que usas para probar a mano —
no hay una base de datos de prueba aislada todavía. Cada corrida crea su propio Cliente
de prueba con un sufijo único (timestamp, ej. `E2E Finca Test 1737000000000`) para no
chocar entre corridas, pero ese dato queda en la base después de correr las pruebas.

Si quieres limpiar el ruido de pruebas: borra `FarmControlAPI/FarmControlAPI/farmcontrol.db`
y reinicia la API (recrea el esquema + siembra los planes, arranca solo con el SuperAdmin).

Para aislar de verdad las pruebas E2E de los datos manuales, el siguiente paso sería
apuntar `dotnet run` en modo test a un `appsettings` con otra cadena de conexión
(ej. `farmcontrol.e2e.db`) — no configurado aún.

## Por qué los selectores de formulario van por `campo()` y no `page.locator('[formcontrolname=...]')`

`fc-drawer` (`shared/components/drawer/drawer.ts`) mantiene su contenido siempre montado
en el DOM — solo alterna una clase CSS para mostrarlo u ocultarlo. Varias páginas tienen
más de un `<fc-drawer>` (o más de un formulario dentro del mismo) con campos del mismo
`formcontrolname` (ej. "nombre") montados al mismo tiempo. Buscar por `formcontrolname`
en toda la página produce "strict mode violation" (varios elementos coinciden). El
helper `campo(page, 'nombre')` en `helpers.ts` acota la búsqueda a `.drawer.open`, el
único drawer visible en un momento dado.
