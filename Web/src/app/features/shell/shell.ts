import { Component, signal, inject, computed, effect, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { FincaContextService } from '../../core/finca/finca-context.service';
import { FincasService } from '../../core/api/fincas.service';
import { NotificacionesService, Notificacion } from '../../core/api/notificaciones.service';
import { Finca } from '../../core/models';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

const INICIO_NAV: NavItem[] = [
  { label: 'Inicio', path: '/app/inicio', icon: 'M3 12l9-9 9 9M5 10v10a1 1 0 001 1h4a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1h4a1 1 0 001-1V10' },
];

const FINCAS_NAV: NavItem[] = [
  { label: 'Fincas',           path: '/app/fincas',           icon: 'M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z' },
];

const OPS_NAV: NavItem[] = [
  { label: 'Actividades',      path: '/app/actividades',      icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2' },
  { label: 'Asistencia',       path: '/app/asistencia',       icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z' },
  { label: 'Nómina',           path: '/app/nomina',           icon: 'M9 7h6m-6 4h6m-6 4h4M5 3h14a2 2 0 012 2v14a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2z' },
  { label: 'Inventario',       path: '/app/inventario',       icon: 'M20 7l-8-4-8 4m16 0v10l-8 4m-8-4V7m16 10l-8-4m-8 4l8-4' },
  { label: 'Compras',          path: '/app/compras',          icon: 'M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2 9m5-9v9m4-9v9m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16' },
  { label: 'Ventas',           path: '/app/ventas',           icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z' },
  { label: 'Procesos Cultivo', path: '/app/procesos-cultivo', icon: 'M12 2a10 10 0 100 20A10 10 0 0012 2zm0 0v10l6 3' },
  { label: 'Reportes',         path: '/app/reportes',         icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
];

const CAT_NAV: NavItem[] = [
  { label: 'Personas',     path: '/app/personas',     icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z' },
  { label: 'Insumos',      path: '/app/inventario',   icon: 'M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z' },
  { label: 'Productos',    path: '/app/productos',    icon: 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z' },
  { label: 'Tipos Insumo', path: '/app/tipos-insumo', icon: 'M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z' },
];

const ADMIN_NAV: NavItem[] = [
  { label: 'Usuarios',  path: '/app/usuarios',  icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z' },
  { label: 'Bitácora',  path: '/app/bitacora',  icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z' },
];

const SOCIO_NAV: NavItem[] = [
  { label: 'Mis Cultivos', path: '/app/mis-cultivos', icon: 'M12 2a10 10 0 100 20A10 10 0 0012 2zm0 0v10l6 3' },
];

const CONTADOR_NAV: NavItem[] = [
  { label: 'Reportes', path: '/app/reportes', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
];

const JORNALERO_NAV: NavItem[] = [
  { label: 'Mis actividades', path: '/app/mis-actividades', icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2' },
];

const SUPER_ADMIN_NAV: NavItem[] = [
  { label: 'Clientes', path: '/app/clientes', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z' },
  { label: 'Backups', path: '/app/backups', icon: 'M20 7l-8-4-8 4m16 0v10l-8 4m-8-4V7m16 10l-8-4m-8 4l8-4' },
];

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class ShellComponent implements OnInit {
  private readonly _authService  = inject(AuthService);
  private readonly _fincasService = inject(FincasService);
  private readonly _notificacionesService = inject(NotificacionesService);
  protected readonly fincaContext = inject(FincaContextService);

  protected readonly inicioNav     = INICIO_NAV;
  protected readonly fincasNav     = FINCAS_NAV;
  protected readonly adminNav      = ADMIN_NAV;
  protected readonly opsNav        = OPS_NAV;
  protected readonly catNav        = CAT_NAV;
  protected readonly socioNav      = SOCIO_NAV;
  protected readonly contadorNav   = CONTADOR_NAV;
  protected readonly jornaleroNav  = JORNALERO_NAV;
  protected readonly superAdminNav = SUPER_ADMIN_NAV;
  protected readonly manualUrl = '/manual-admin.html';
  protected readonly usuario       = this._authService.usuario;
  protected readonly isAdmin       = this._authService.isAdmin;
  protected readonly isSocio       = this._authService.isSocio;
  protected readonly isSuperAdmin  = this._authService.isSuperAdmin;
  protected readonly isContador    = computed(() => this._authService.rol() === 'Contador');
  protected readonly isJornalero   = this._authService.isJornalero;
  protected readonly sidebarOpen = signal(true);
  protected readonly mobileNavOpen = signal(false);
  protected readonly fincasDisponibles = signal<Finca[]>([]);

  protected readonly notificaciones = signal<Notificacion[]>([]);
  protected readonly notificacionesAbiertas = signal(false);

  private static readonly _INTERVALO_NOTIFICACIONES_MS = 120000;

  constructor() {
    // Recarga la lista de fincas del selector cada vez que cambian las fincas de la sesión
    // (ej. el Admin registra una finca propia) — no solo una vez al entrar al shell.
    effect(() => {
      const ids = this.fincaContext.fincaIds();
      if (ids.length > 0) {
        this._fincasService.getAll().subscribe({
          next:  fincas => this.fincasDisponibles.set(fincas),
          error: () => {},
        });
      } else {
        this.fincasDisponibles.set([]);
      }
    });
  }

  ngOnInit(): void {
    if (this.isAdmin()) {
      this.cargarNotificaciones();
      setInterval(() => this.cargarNotificaciones(), ShellComponent._INTERVALO_NOTIFICACIONES_MS);
    }
  }

  private cargarNotificaciones(): void {
    this._notificacionesService.getAll().subscribe({
      next:  n => this.notificaciones.set(n),
      error: () => {},
    });
  }

  protected toggleNotificaciones(): void {
    this.notificacionesAbiertas.update(v => !v);
  }

  protected cerrarNotificaciones(): void {
    this.notificacionesAbiertas.set(false);
  }

  protected onSeleccionarFinca(event: Event): void {
    const fincaId = Number((event.target as HTMLSelectElement).value);
    this.fincaContext.setSelectedFinca(fincaId);
  }

  protected readonly homePath = computed(() => {
    if (this.isSocio()) { return '/app/mis-cultivos'; }
    if (this.isSuperAdmin()) { return '/app/clientes'; }
    if (this.isContador()) { return '/app/reportes'; }
    if (this.isJornalero()) { return '/app/mis-actividades'; }
    return '/app/inicio';
  });

  protected readonly roleLabel = computed(() => {
    const r = this._authService.rol();
    if (r === 'SuperAdmin') { return 'Super Admin'; }
    if (r === 'Admin') { return 'Administrador'; }
    if (r === 'Auditor') { return 'Auditor'; }
    if (r === 'Socio') { return 'Socio'; }
    if (r === 'Contador') { return 'Contador'; }
    if (r === 'Jornalero') { return 'Jornalero'; }
    return r ?? '';
  });

  protected toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }

  protected toggleMobileNav(): void {
    this.mobileNavOpen.update(v => !v);
  }

  protected closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }

  protected logout(): void {
    this._authService.logout();
  }
}
