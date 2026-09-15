import { Routes } from '@angular/router';
import { authGuard, adminGuard, socioGuard, superAdminGuard, reportesGuard, jornaleroGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'app',
    loadComponent: () => import('./features/shell/shell').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'inicio',
        loadComponent: () => import('./features/dashboard/dashboard').then(m => m.DashboardComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'fincas',
        loadComponent: () => import('./features/fincas/fincas').then(m => m.FincasComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'inventario',
        loadComponent: () => import('./features/inventario/inventario').then(m => m.InventarioComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'actividades',
        loadComponent: () => import('./features/actividades/actividades').then(m => m.ActividadesComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'compras',
        loadComponent: () => import('./features/compras/compras').then(m => m.ComprasComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'asistencia',
        loadComponent: () => import('./features/asistencia/asistencia').then(m => m.AsistenciaComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'nomina',
        loadComponent: () => import('./features/nomina/nomina').then(m => m.NominaComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'ventas',
        loadComponent: () => import('./features/ventas/ventas').then(m => m.VentasComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'personas',
        loadComponent: () => import('./features/personas/personas').then(m => m.PersonasComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'productos',
        loadComponent: () => import('./features/productos/productos').then(m => m.ProductosComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'tipos-insumo',
        loadComponent: () => import('./features/tipos-insumo/tipos-insumo').then(m => m.TiposInsumoComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'procesos-cultivo',
        loadComponent: () => import('./features/procesos-cultivo/procesos-cultivo').then(m => m.ProcesosCultivoComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'reportes',
        loadComponent: () => import('./features/reportes/reportes').then(m => m.ReportesComponent),
        canActivate: [reportesGuard],
      },
      {
        path: 'usuarios',
        loadComponent: () => import('./features/usuarios/usuarios').then(m => m.UsuariosComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'bitacora',
        loadComponent: () => import('./features/bitacora/bitacora').then(m => m.BitacoraComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'clientes',
        loadComponent: () => import('./features/clientes/clientes').then(m => m.ClientesComponent),
        canActivate: [superAdminGuard],
      },
      {
        path: 'backups',
        loadComponent: () => import('./features/backups/backups').then(m => m.BackupsComponent),
        canActivate: [superAdminGuard],
      },
      {
        path: 'mis-cultivos',
        loadComponent: () => import('./features/mis-cultivos/mis-cultivos').then(m => m.MisCultivosComponent),
        canActivate: [socioGuard],
      },
      {
        path: 'mi-cuenta',
        loadComponent: () => import('./features/mi-cuenta/mi-cuenta').then(m => m.MiCuentaComponent),
      },
      {
        path: 'mis-actividades',
        loadComponent: () => import('./features/mis-actividades/mis-actividades').then(m => m.MisActividadesComponent),
        canActivate: [jornaleroGuard],
      },
      { path: '', redirectTo: 'inicio', pathMatch: 'full' },
      {
        path: '**',
        loadComponent: () => import('./features/not-found/not-found').then(m => m.NotFoundComponent),
      },
    ],
  },
  {
    path: 'reporte-compartido/:token',
    loadComponent: () => import('./features/reporte-publico/reporte-publico').then(m => m.ReportePublicoComponent),
  },
  {
    path: '',
    loadComponent: () => import('./features/landing/landing').then(m => m.LandingComponent),
  },
  { path: '**', redirectTo: '' },
];
