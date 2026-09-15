# Frontend Angular 18

## Reglas estrictas
- Solo componentes standalone
- Nunca usar `any` en TypeScript
- inject() para dependencias, no constructor
- Lazy loading en todas las rutas
- Lógica en servicios, no en templates ni componentes

## Estructura de features
src/app/
├── core/          → guards, interceptors, servicios singleton
├── shared/        → componentes y pipes reutilizables
└── features/      → un directorio por feature

## Estilos
- SCSS con variables globales en styles/variables.scss
- No estilos inline en componentes
- para cualquier tarea de UI, lee y sigue las guías en:
.claude/skills/frontend-design/SKILL.md

## Reglas Angular 22
- Zoneless por defecto, no usar Zone.js en proyectos nuevos
- Signal Forms para todos los formularios (ya estable)
- httpResource() para llamadas HTTP, no subscribe directo
- @Service() en lugar de @Injectable({ providedIn: 'root' })
- Componentes selectorless cuando sea posible
- OnPush es el default, no declararlo explícitamente
- Nunca mutar estado directamente, siempre a través de signals