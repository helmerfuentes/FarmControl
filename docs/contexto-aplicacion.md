# Contexto de la aplicación — FarmControl

## Qué es

App full-stack de gestión agrícola/finca ("FarmControl"), con frontend Angular 22 y backend .NET 8 en Clean Architecture.

## Dominio (entidades)

Finca, Parcela, Persona, ProcesoCultivo, Actividad, RegistroManoObra, Producto, ClasificacionProducto, Insumo, TipoInsumo, MovimientoInsumo, Compra, Venta, DetalleVenta, HistorialPrecioVenta.

En conjunto describe el ciclo de una finca: fincas divididas en parcelas, procesos de cultivo con actividades y mano de obra, manejo de insumos (inventario, movimientos, compras) y productos con ventas/historial de precios.

## Backend (`FarmControlAPI/`) — Clean Architecture

- `Domain` → entidades, enums, interfaces de repositorio
- `Application` → CQRS con MediatR (Commands/Queries por feature: Actividades, Compras, Fincas, Insumos, Personas, ProcesosCultivo, Productos, Reportes, TiposInsumo, Ventas, Auth)
- `Infrastructure` → EF Core, persistencia, migraciones, servicios
- `API` → Minimal API Endpoints (uno por feature) + Auth + Socio

Reglas del proyecto (`FarmControlAPI/CLAUDE.md`):
- Cero lógica de negocio en Controllers
- Siempre async/await en toda la cadena
- Validaciones con FluentValidation en Application/
- No usar excepciones para flujo normal, usar Result<T>
- Repositorios solo en Infrastructure, interfaces en Domain
- Dependencias entre capas: Domain ← Application ← Infrastructure / API
- Tests: xUnit + Moq, un archivo de test por cada Handler

## Frontend (`Web/`) — Angular 22

Arquitectura por features (`core/`, `shared/`, `features/`): actividades, compras, fincas, inventario, landing, login, mis-cultivos, personas, procesos-cultivo, productos, reportes, shell, tipos-insumo, ventas.

Reglas del proyecto (`Web/CLAUDE.md`):
- Solo componentes standalone
- Nunca usar `any` en TypeScript
- `inject()` para dependencias, no constructor
- Lazy loading en todas las rutas
- Lógica en servicios, no en templates ni componentes
- Zoneless por defecto, sin Zone.js
- Signal Forms para todos los formularios
- `httpResource()` para llamadas HTTP, no subscribe directo
- `@Service()` en lugar de `@Injectable({ providedIn: 'root' })`
- Componentes selectorless cuando sea posible
- OnPush por defecto (no declararlo explícitamente)
- Nunca mutar estado directamente, siempre a través de signals
- SCSS con variables globales en `styles/variables.scss`, sin estilos inline
- Tests con Vitest

## Estado del repo (al momento de esta nota)

Rama `master`. Cambios iniciales sin commitear: `.gitignore`, `.claude/`, `CLAUDE.md`, `FarmControlAPI/`, `Web/` (untracked) — aún no hay historial de commits.
