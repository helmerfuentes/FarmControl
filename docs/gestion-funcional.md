# Gestión funcional de FarmControl

Documento de contexto sobre **qué gestiona la aplicación**: procesos de negocio, entidades, reglas y flujos que soporta el backend (`FarmControlAPI/`) y consume el frontend (`Web/`). Complementa a `docs/contexto-aplicacion.md` (contexto técnico/arquitectónico).

## Propósito general

FarmControl es un sistema de administración agrícola para llevar el control operativo y financiero de una o varias **fincas**: su división en parcelas, los cultivos que se siembran en ellas, la mano de obra y compras asociadas, y las ventas de la producción — hasta llegar a un resumen de rentabilidad por finca y por parcela.

## Roles y autenticación

Login vía `POST /auth/login` (`Auth/LoginCommand.cs`), con dos vías de autenticación:

1. **Usuarios de sistema hardcodeados** en el propio código (`_USUARIOS_SISTEMA`):
   - `admin` / `admin123` → rol **Admin**
   - `auditor` / `auditor123` → rol **Auditor**
   - ⚠️ Nota: credenciales en texto plano embebidas en el código fuente (`LoginCommandHandler`), no en configuración segura. Es un riesgo a corregir (no se modificó, solo se documenta).
2. **Personas registradas** (`Persona.NombreUsuario` + `PasswordHash` con BCrypt) — su rol de sesión es su `TipoPersona` (`Socio`, `Jornalero`, `Comprador`, `Otro`).

El token generado (`ITokenService.GenerateToken`) incluye usuario, rol y, si aplica, `personaId` — usado por endpoints como `/socio/mis-procesos` para filtrar por la persona autenticada.

La mayoría de endpoints requieren autenticación (`RequireAuthorization()`); las operaciones de **creación/edición/borrado** exigen además el rol **Admin** (`RequireAuthorization("Admin")`). Las consultas (GET) son accesibles a cualquier usuario autenticado.

## Entidades y su rol en la gestión

| Entidad | Gestiona |
|---|---|
| **Finca** | Unidad principal: nombre, ubicación, área total, costo del terreno. Contiene parcelas y compras. |
| **Parcela** | Subdivisión de una finca (nombre, área). Sobre ella ocurren actividades, movimientos de insumo, ventas y procesos de cultivo. |
| **ProcesoCultivo** | Un ciclo productivo sobre una parcela: producto sembrado, socio asociado, costo inicial, fecha de inicio, fecha estimada de cosecha, fecha de cierre y estado (`Activo`, `Cosechado`, `Cerrado`). |
| **Producto** | Catálogo de productos cultivables (p. ej. tipo de cultivo), con sus clasificaciones. |
| **ClasificacionProducto** | Variante de venta de un producto (nombre, unidad de medida — Bulto/Kilo/Canastilla/Caja — y peso por unidad en kg). |
| **Actividad** | Labor realizada en una parcela (tipo, fechas, persona a cargo, valor día usado). Puede asociarse a un proceso de cultivo. Es la base del costeo de mano de obra. |
| **RegistroManoObra** | Detalle de jornal dentro de una actividad: jornalero, socio que paga, valor hora, número de horas, hora de entrada/salida. |
| **Persona** | Registro único de personas con `TipoPersona`: Socio, Jornalero, Comprador u Otro. Reutilizada como socio de proceso, jornalero, pagador, comprador o cliente. Tiene `ValorDia` (tarifa de jornal) y, opcionalmente, credenciales para login. |
| **TipoInsumo / Insumo** | Catálogo de insumos agrícolas (tipo, nombre, marca, precio unitario, unidad de medida). |
| **MovimientoInsumo** | Entradas y salidas de inventario de insumos por parcela (kardex). |
| **Compra** | Egreso asociado a una finca (y opcionalmente a una parcela/proceso de cultivo): insumo, maquinaria u otro. Incluye valor, fecha, socio que compra y adjunto (comprobante). |
| **Venta** | Venta de producción de una parcela a un comprador (persona), con valor de transporte, total y, opcionalmente, proceso de cultivo asociado. |
| **DetalleVenta** | Líneas de una venta: clasificación del producto, cantidad, unidad de medida, precio unitario y subtotal. |
| **HistorialPrecioVenta** | Registro histórico de precios por producto/clasificación derivado de cada venta, usado para reportes de tendencia de precios. |

## Procesos de negocio (por feature)

### Fincas y parcelas (`/fincas`, `/parcelas`)
CRUD de fincas y, anidado, de sus parcelas. Es la base jerárquica sobre la que se cuelga todo lo demás (cultivos, compras, ventas, insumos, actividades).

### Procesos de cultivo (`/procesos-cultivo`)
Registra el ciclo de vida de un cultivo en una parcela: se crea con producto, costo inicial y fecha de inicio; se puede actualizar; y se **cierra** explícitamente vía `POST /{id}/cerrar` indicando el estado final (`Cosechado` o `Cerrado`) y la fecha de cierre. Puede filtrarse por parcela y por estado. Un socio autenticado puede consultar únicamente **sus propios procesos** vía `GET /socio/mis-procesos` (filtra por `personaId` del token).

### Actividades y mano de obra (`/actividades`, `/mano-obra`)
Las actividades documentan trabajo realizado en una parcela (opcionalmente ligado a un proceso de cultivo), con un responsable y un valor-día de referencia. Sobre cada actividad se registran uno o más jornales (`RegistroManoObra`) con horas trabajadas y valor hora, permitiendo calcular el costo real de mano de obra. El reporte de resumen de finca deriva el costo de mano de obra a partir de la duración de la actividad (`FechaFin - FechaInicio`) y el valor-día, no de los registros de jornal detallados — ver limitación abajo.

### Insumos e inventario (`/insumos`, `/tipos-insumo`, `/movimientos-insumo`)
Catálogo de tipos de insumo e insumos con su precio y unidad. Los `MovimientoInsumo` (Entrada/Salida) llevan el control de inventario por parcela — es el kardex de insumos aplicados o recibidos.

### Compras (`/compras`)
Registra egresos de una finca: insumos, maquinaria u otros gastos, opcionalmente asociados a una parcela o proceso de cultivo específico, con soporte de adjunto (URL de comprobante). Alimenta directamente el cálculo de utilidad en el reporte de finca.

### Productos (`/productos`)
Catálogo de productos cultivables y sus clasificaciones de venta (unidad de medida y peso por unidad), usado tanto por procesos de cultivo como por el detalle de ventas.

### Ventas (`/ventas`)
Registra la venta de producción de una parcela a un comprador, con una o más líneas de detalle (clasificación, cantidad, unidad, precio unitario, subtotal) y costo de transporte. Cada venta genera automáticamente entradas en `HistorialPrecioVenta` para trazabilidad de precios.

### Personas (`/personas`)
Gestión unificada de todos los actores humanos del sistema (socios, jornaleros, compradores, otros), filtrable por `TipoPersona`. Es la entidad reutilizada como responsable de actividad, jornalero, pagador de jornal, comprador y socio de compra/proceso.

### Reportes (`/reportes`)
Dos reportes de solo lectura:

- **`GET /reportes/finca/{id}/resumen`** — Resumen financiero de una finca: ingresos totales (suma de subtotales de venta), compras totales, mano de obra total y **utilidad** (`ingresos − compras − mano de obra`), desglosado por parcela (incluyendo el producto del proceso de cultivo activo).
- **`GET /reportes/precios?productoId=&clasificacion=&anio=`** — Historial de precios de un producto/clasificación, agregado por mes (promedio, mínimo, máximo y número de ventas), útil para analizar tendencias de precio de venta a lo largo del tiempo.

## Flujo típico de uso

1. Se crea una **Finca** y sus **Parcelas**.
2. Se registran **Personas** (socios, jornaleros, compradores).
3. Se abre un **ProcesoCultivo** en una parcela con un **Producto** y costo inicial.
4. Durante el ciclo:
   - Se registran **Actividades** (labores) con **RegistroManoObra** (jornales).
   - Se registran **Compras** (insumos, maquinaria, otros gastos) asociadas a la finca/parcela/proceso.
   - Se aplican **MovimientoInsumo** (entradas/salidas de inventario) por parcela.
5. Se registran **Ventas** de la producción con su **DetalleVenta**, generando historial de precios.
6. Se **cierra** el proceso de cultivo (`Cosechado` o `Cerrado`).
7. Se consulta el **reporte de resumen de finca** para ver ingresos, egresos y utilidad, y el **historial de precios** para análisis de mercado.

## Limitaciones / notas observadas

- El cálculo de costo de mano de obra en el reporte de resumen de finca (`GetResumenFincaQuery`) usa la duración total de la actividad y el `ValorDiaUsado`, **no** la suma de los `RegistroManoObra` individuales (horas × valor hora por jornalero) — puede no reflejar el costo real si varios jornaleros con tarifas distintas participan en la misma actividad.
- Las credenciales de los usuarios de sistema (`admin`, `auditor`) están hardcodeadas en el código fuente en texto plano, no gestionadas como secretos.
- No existe endpoint de `GET /ventas/{id}` ni `GET /compras/{id}` (solo listado con filtros); tampoco `UpdateVenta` (solo creación y borrado).
