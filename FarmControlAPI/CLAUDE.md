# API .NET - Clean Architecture

## Capas y responsabilidades
- `Domain/` → Entidades, value objects, interfaces de repositorio
- `Application/` → Casos de uso (CQRS con MediatR), DTOs, validaciones
- `Infrastructure/` → EF Core, repositorios, servicios externos
- `API/` → Controllers, middlewares, configuración DI

## Reglas estrictas
- Cero lógica de negocio en Controllers
- Siempre async/await en toda la cadena
- Validaciones con FluentValidation en Application/
- No usar excepciones para flujo normal, usar Result<T>
- Repositorios solo en Infrastructure, interfaces en Domain

## Dependencias entre capas
Domain ← Application ← Infrastructure
Domain ← Application ← API

## Tests
- xUnit + Moq
- Un archivo de test por cada Handler