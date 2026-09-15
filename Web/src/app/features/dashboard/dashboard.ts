import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ActividadesService } from '../../core/api/actividades.service';
import { ProcesosCultivoService } from '../../core/api/procesos-cultivo.service';
import { InsumosService, SaldoInsumo } from '../../core/api/insumos.service';
import { TareasService } from '../../core/api/tareas.service';
import { VentasService } from '../../core/api/ventas.service';
import { ComprasService } from '../../core/api/compras.service';
import { ReportesService, ResumenConsolidado } from '../../core/api/reportes.service';
import { AuthService } from '../../core/auth/auth.service';
import { Actividad, ProcesoCultivo, TareaRecurrente, Venta, Compra } from '../../core/models';

const DIAS_ALERTA_COSECHA = 15;
const DIAS_VENTAS_RECIENTES = 7;

interface KpiTile {
	id: string;
	label: string;
	tipo: 'moneda' | 'numero';
	valor: () => number;
}

const _HOY_ISO = () => new Date().toISOString().substring(0, 10);

@Component({
	selector: 'app-dashboard',
	imports: [DatePipe, CurrencyPipe, RouterLink],
	templateUrl: './dashboard.html',
	styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit {
	private readonly _actividadesSvc = inject(ActividadesService);
	private readonly _procesosSvc    = inject(ProcesosCultivoService);
	private readonly _insumosSvc     = inject(InsumosService);
	private readonly _tareasSvc      = inject(TareasService);
	private readonly _ventasSvc      = inject(VentasService);
	private readonly _comprasSvc     = inject(ComprasService);
	private readonly _reportesSvc    = inject(ReportesService);
	private readonly _authSvc        = inject(AuthService);
	protected readonly usuario       = this._authSvc.usuario;

	protected readonly loadingActividades = signal(false);
	protected readonly loadingCosechas    = signal(false);
	protected readonly loadingSaldos      = signal(false);
	protected readonly loadingHoy         = signal(false);
	protected readonly loadingKpis        = signal(false);

	private readonly _actividades = signal<Actividad[]>([]);
	private readonly _procesosActivos = signal<ProcesoCultivo[]>([]);
	private readonly _saldos = signal<SaldoInsumo[]>([]);
	private readonly _tareas = signal<TareaRecurrente[]>([]);
	private readonly _ventas = signal<Venta[]>([]);
	private readonly _compras = signal<Compra[]>([]);
	private readonly _consolidado = signal<ResumenConsolidado | null>(null);

	protected readonly ultimasActividades = computed(() => this._actividades().slice(0, 5));

	protected readonly proximasCosechas = computed(() => {
		const hoy = new Date();
		const limite = new Date();
		limite.setDate(hoy.getDate() + DIAS_ALERTA_COSECHA);
		return this._procesosActivos()
			.filter(p => p.fechaEstimadaCosecha)
			.map(p => ({ proceso: p, fecha: new Date(p.fechaEstimadaCosecha!) }))
			.filter(x => x.fecha <= limite)
			.sort((a, b) => a.fecha.getTime() - b.fecha.getTime())
			.map(x => x.proceso);
	});

	protected readonly insumosBajos = computed(() => this._saldos().filter(s => s.saldoBajo));

	// ---- Feature 47: vista de "hoy" ----

	protected readonly actividadesHoy = computed(() =>
		this._actividades().filter(a => a.fechaInicio.substring(0, 10) === _HOY_ISO() && !a.confirmada));

	protected readonly tareasVencidas = computed(() =>
		this._tareas()
			.filter(t => t.activa && t.proximaFecha.substring(0, 10) <= _HOY_ISO())
			.sort((a, b) => a.proximaFecha.localeCompare(b.proximaFecha)));

	protected readonly ventasHoy = computed(() => this._ventas().filter(v => v.fecha.substring(0, 10) === _HOY_ISO()));
	protected readonly comprasHoy = computed(() => this._compras().filter(c => c.fecha.substring(0, 10) === _HOY_ISO()));

	protected readonly totalVentasHoy = computed(() => this.ventasHoy().reduce((s, v) => s + v.total, 0));
	protected readonly totalComprasHoy = computed(() => this.comprasHoy().reduce((s, c) => s + c.valor, 0));

	protected readonly nadaPendienteHoy = computed(() =>
		this.actividadesHoy().length === 0 &&
		this.tareasVencidas().length === 0 &&
		this.ventasHoy().length === 0 &&
		this.comprasHoy().length === 0);

	// ---- Feature 33: KPIs configurables ----

	protected readonly kpiCatalog: KpiTile[] = [
		{ id: 'ingresos-totales', label: 'Ingresos totales', tipo: 'moneda', valor: () => this._consolidado()?.totalIngresos ?? 0 },
		{ id: 'gastos-totales', label: 'Gastos totales', tipo: 'moneda', valor: () => (this._consolidado()?.totalCompras ?? 0) + (this._consolidado()?.totalManoObra ?? 0) },
		{ id: 'utilidad-total', label: 'Utilidad total', tipo: 'moneda', valor: () => this._consolidado()?.utilidad ?? 0 },
		{ id: 'actividades-pendientes', label: 'Actividades pendientes', tipo: 'numero', valor: () => this._actividades().filter(a => !a.confirmada).length },
		{
			id: 'ventas-recientes', label: `Ventas (últimos ${DIAS_VENTAS_RECIENTES} días)`, tipo: 'numero', valor: () => {
				const limite = new Date();
				limite.setDate(limite.getDate() - DIAS_VENTAS_RECIENTES);
				const limiteIso = limite.toISOString().substring(0, 10);
				return this._ventas().filter(v => v.fecha.substring(0, 10) >= limiteIso).length;
			},
		},
		{ id: 'insumos-alerta', label: 'Insumos con alerta', tipo: 'numero', valor: () => this._saldos().filter(s => s.saldoBajo).length },
	];

	private readonly _kpisSeleccionados = signal<Set<string> | null>(null);
	protected readonly personalizarAbierto = signal(false);
	protected readonly kpisEnEdicion = signal<Set<string>>(new Set());

	protected readonly kpisVisibles = computed(() => {
		const seleccion = this._kpisSeleccionados();
		if (seleccion === null || seleccion.size === 0) {
			return this.kpiCatalog;
		}
		return this.kpiCatalog.filter(k => seleccion.has(k.id));
	});

	ngOnInit(): void {
		this.loadingActividades.set(true);
		this._actividadesSvc.getAll().subscribe({
			next:  a => { this._actividades.set(a); this.loadingActividades.set(false); },
			error: () => this.loadingActividades.set(false),
		});

		this.loadingCosechas.set(true);
		this._procesosSvc.getAll(undefined, 'Activo').subscribe({
			next:  p => { this._procesosActivos.set(p); this.loadingCosechas.set(false); },
			error: () => this.loadingCosechas.set(false),
		});

		this.loadingSaldos.set(true);
		this._insumosSvc.getSaldos().subscribe({
			next:  s => { this._saldos.set(s); this.loadingSaldos.set(false); },
			error: () => this.loadingSaldos.set(false),
		});

		this.loadingHoy.set(true);
		this._tareasSvc.getAll().subscribe({
			next:  t => { this._tareas.set(t); this.loadingHoy.set(false); },
			error: () => this.loadingHoy.set(false),
		});
		this._ventasSvc.getAll().subscribe({ next: v => this._ventas.set(v), error: () => {} });
		this._comprasSvc.getAll().subscribe({ next: c => this._compras.set(c), error: () => {} });

		this.loadingKpis.set(true);
		this._reportesSvc.getResumenConsolidado().subscribe({
			next:  c => { this._consolidado.set(c); this.loadingKpis.set(false); },
			error: () => this.loadingKpis.set(false),
		});

		this._authSvc.getPreferenciasDashboard().subscribe({
			next: res => {
				const seleccion = this._parsearPreferencias(res.preferenciasJson);
				this._kpisSeleccionados.set(seleccion);
			},
			error: () => this._kpisSeleccionados.set(null),
		});
	}

	private _parsearPreferencias(json: string | null): Set<string> | null {
		let seleccion: Set<string> | null;
		if (!json) {
			seleccion = null;
		} else {
			try {
				const ids = JSON.parse(json);
				seleccion = Array.isArray(ids) ? new Set(ids as string[]) : null;
			} catch {
				seleccion = null;
			}
		}
		return seleccion;
	}

	protected abrirPersonalizar(): void {
		const actuales = this._kpisSeleccionados();
		this.kpisEnEdicion.set(actuales === null ? new Set(this.kpiCatalog.map(k => k.id)) : new Set(actuales));
		this.personalizarAbierto.set(true);
	}

	protected cerrarPersonalizar(): void {
		this.personalizarAbierto.set(false);
	}

	protected toggleKpiEdicion(id: string, checked: boolean): void {
		this.kpisEnEdicion.update(set => {
			const nuevo = new Set(set);
			if (checked) { nuevo.add(id); } else { nuevo.delete(id); }
			return nuevo;
		});
	}

	protected guardarPersonalizacion(): void {
		const seleccion = this.kpisEnEdicion();
		const json = JSON.stringify(Array.from(seleccion));
		this._authSvc.guardarPreferenciasDashboard(json).subscribe({
			next: () => {
				this._kpisSeleccionados.set(seleccion);
				this.personalizarAbierto.set(false);
			},
			error: () => this.personalizarAbierto.set(false),
		});
	}
}
