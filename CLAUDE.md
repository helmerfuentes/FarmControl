# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure

Full-stack application with two independent projects:

- **`Web/`** — Angular 22 frontend (TypeScript, SCSS, Vitest)
- **`FarmControlAPI/`** —  API REST .NET 8 con Clean Architecture

## Commands

### Frontend (`Web/`)

```bash
npm start          # dev server at http://localhost:4200
npm run build      # production build → dist/
npm test           # run tests with Vitest
```

### Backend (`FarmControlAPI/`)

Open `FarmControlAPI.sln` in Visual Studio, or from the `FarmControlAPI/FarmControlAPI/` folder:

```bash
dotnet run         # start API
dotnet build       # build only
dotnet test        # run tests (when test projects are added)
```
## Convenciones generales
- Commits en inglés, Conventional Commits: feat:, fix:, chore:, docs:
- Código en inglés, comentarios de negocio en español