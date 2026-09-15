import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { FincasService } from '../../core/api/fincas.service';
import { ReportesService, ResumenFinca, ResumenConsolidado, HistoricoFinca, HistorialPrecios, EstacionalidadPrecios } from '../../core/api/reportes.service';
import { ProductosService } from '../../core/api/productos.service';
import { FincaContextService } from '../../core/finca/finca-context.service';
import { Finca, Producto } from '../../core/models';
import { DashboardSkeletonComponent } from '../../shared/components/dashboard-skeleton/dashboard-skeleton';

declare const Chart: {
	new(canvas: HTMLCanvasElement, config: object): { destroy(): void };
};

const COLOR_INGRESOS = '#16A34A';
const COLOR_COMPRAS  = '#BA7517';
const COLOR_MANO     = '#534AB7';

const NOMBRES_MES = [
	'Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic',
];

@Component({
	selector: 'app-reportes',
	standalone: true,
	imports: [CurrencyPipe, DecimalPipe, DashboardSkeletonComponent],
	templateUrl: './reportes.html',
	styleUrl: './reportes.scss',
})
export class ReportesComponent implements OnInit {
	private readonly _fincasService    = inject(FincasService);
	private readonly _reportesService  = inject(ReportesService);
	private readonly _productosService = inject(ProductosService);
	protected readonly fincaContext    = inject(FincaContextService);

	readonly fincas          = signal<Finca[]>([]);
	readonly selectedFincaId = this.fincaContext.selectedFincaId;
	readonly resumen         = signal<ResumenFinca | null>(null);
	readonly loading         = signal(false);
	readonly error           = signal<string | null>(null);

	readonly vistaConsolidada  = signal(false);
	readonly consolidado       = signal<ResumenConsolidado | null>(null);
	readonly loadingConsolidado = signal(false);

	readonly historico = signal<HistoricoFinca | null>(null);
	private _evolucionChart: { destroy(): void } | null = null;
	private _consolidadoChart: { destroy(): void } | null = null;

	readonly linkCompartido = signal<string | null>(null);
	readonly compartiendo = signal(false);
	readonly linkCopiado = signal(false);

	readonly productos = signal<Producto[]>([]);
	readonly selectedProductoId = signal<number | null>(null);
	readonly historialPrecios = signal<HistorialPrecios[]>([]);
	readonly loadingPrecios = signal(false);

	private _donutChart: { destroy(): void } | null = null;
	private _barChart:   { destroy(): void } | null = null;

	readonly estacionalidad = signal<EstacionalidadPrecios | null>(null);
	readonly loadingEstacionalidad = signal(false);
	private _estacionalidadChart: { destroy(): void } | null = null;

	readonly margen = computed(() => {
		const r = this.resumen();
		if (!r || r.totalIngresos === 0) { return 0; }
		return (r.utilidad / r.totalIngresos) * 100;
	});

	readonly fincaActivaNombre = computed(() => {
		const id = this.selectedFincaId();
		return this.fincas().find(f => f.id === id)?.nombre ?? '';
	});

	readonly mostrarToggleConsolidado = computed(() => this.fincas().length > 1);

	constructor() {
		effect(() => {
			const id = this.selectedFincaId();
			if (id !== null) {
				this.loadResumen(id);
			} else {
				this.resumen.set(null);
				this.historico.set(null);
				this.destroyCharts();
			}
		});
	}

	ngOnInit(): void {
		this._fincasService.getAll().subscribe({
			next:  fincas => this.fincas.set(fincas),
			error: ()     => {},
		});

		this._productosService.getAll().subscribe({
			next:  productos => this.productos.set(productos),
			error: ()        => {},
		});
	}

	protected onSelectProducto(value: string): void {
		const productoId = Number(value) || null;
		this.selectedProductoId.set(productoId);
		if (productoId === null) {
			this.historialPrecios.set([]);
			this.estacionalidad.set(null);
			this._estacionalidadChart?.destroy();
			this._estacionalidadChart = null;
			return;
		}
		this.loadingPrecios.set(true);
		this._reportesService.getHistorialPrecios(productoId).subscribe({
			next:  data => { this.historialPrecios.set(data); this.loadingPrecios.set(false); },
			error: ()   => { this.historialPrecios.set([]); this.loadingPrecios.set(false); },
		});

		this.loadingEstacionalidad.set(true);
		this._reportesService.getEstacionalidadPrecios(productoId).subscribe({
			next: data => {
				this.estacionalidad.set(data);
				this.loadingEstacionalidad.set(false);
				setTimeout(() => this.renderEstacionalidadChart(data), 0);
			},
			error: () => { this.estacionalidad.set(null); this.loadingEstacionalidad.set(false); },
		});
	}

	protected readonly nombresMes = NOMBRES_MES;

	protected toggleVistaConsolidada(): void {
		this.vistaConsolidada.update(v => !v);
		if (this.vistaConsolidada() && this.consolidado() === null) {
			this.loadConsolidado();
		}
	}

	protected loadConsolidado(): void {
		this.loadingConsolidado.set(true);
		this._reportesService.getResumenConsolidado().subscribe({
			next:  data => {
				this.consolidado.set(data);
				this.loadingConsolidado.set(false);
				setTimeout(() => this.renderConsolidadoChart(data), 0);
			},
			error: () => this.loadingConsolidado.set(false),
		});
	}

	protected compartir(): void {
		const fincaId = this.selectedFincaId();
		if (fincaId === null || this.compartiendo()) { return; }
		this.compartiendo.set(true);
		this.linkCopiado.set(false);
		this._reportesService.compartir(fincaId).subscribe({
			next: resp => {
				this.compartiendo.set(false);
				this.linkCompartido.set(`${window.location.origin}/reporte-compartido/${resp.token}`);
			},
			error: () => this.compartiendo.set(false),
		});
	}

	protected copiarLink(): void {
		const link = this.linkCompartido();
		if (!link) { return; }
		navigator.clipboard.writeText(link).then(() => {
			this.linkCopiado.set(true);
			setTimeout(() => this.linkCopiado.set(false), 2000);
		});
	}

	protected cerrarLinkCompartido(): void {
		this.linkCompartido.set(null);
	}

	private loadResumen(fincaId: number): void {
		this.loading.set(true);
		this.error.set(null);
		this._reportesService.getResumenFinca(fincaId).subscribe({
			next: data => {
				this.resumen.set(data);
				this.loading.set(false);
				setTimeout(() => this.renderCharts(data), 0);
			},
			error: () => {
				this.error.set('No se pudo cargar el resumen de la finca.');
				this.loading.set(false);
			},
		});

		this._reportesService.getHistoricoFinca(fincaId).subscribe({
			next: data => {
				this.historico.set(data);
				setTimeout(() => this.renderEvolucionChart(data), 0);
			},
			error: () => this.historico.set(null),
		});
	}

	private renderCharts(r: ResumenFinca): void {
		const attempt = (tries: number) => {
			this.destroyCharts();
			const donutCanvas = document.getElementById('donutChart') as HTMLCanvasElement | null;
			const barCanvas   = document.getElementById('barChart')   as HTMLCanvasElement | null;
			if (!donutCanvas || !barCanvas) { return; }

			this._donutChart = new Chart(donutCanvas, {
				type: 'doughnut',
				data: {
					labels: ['Compras', 'Mano de obra'],
					datasets: [{
						data: [r.totalCompras, r.totalManoObra],
						backgroundColor: [COLOR_COMPRAS, COLOR_MANO],
						borderWidth: 0,
						hoverOffset: 4,
					}],
				},
				options: {
					responsive: true,
					maintainAspectRatio: false,
					cutout: '68%',
					plugins: {
						legend: { display: false },
						tooltip: { callbacks: { label: (c: { raw: number }) => ' $' + c.raw.toLocaleString('es-CO') } },
					},
				},
			});

			const gastos = r.totalCompras + r.totalManoObra;
			this._barChart = new Chart(barCanvas, {
				type: 'bar',
				data: {
					labels: ['Ingresos', 'Gastos'],
					datasets: [{
						data: [r.totalIngresos, gastos],
						backgroundColor: [COLOR_INGRESOS, COLOR_COMPRAS],
						borderRadius: 6,
						borderSkipped: false,
					}],
				},
				options: {
					responsive: true,
					maintainAspectRatio: false,
					plugins: {
						legend: { display: false },
						tooltip: { callbacks: { label: (c: { raw: number }) => ' $' + c.raw.toLocaleString('es-CO') } },
					},
					scales: {
						x: { grid: { display: false }, ticks: { color: '#888' } },
						y: {
							grid: { color: 'rgba(0,0,0,0.05)' },
							ticks: {
								color: '#888',
								callback: (v: number | string) => {
									const n = Number(v);
									return n >= 1000000 ? '$' + (n / 1000000).toFixed(1) + 'M'
										 : n >= 1000    ? '$' + Math.round(n / 1000) + 'k'
										 : '$' + n;
								},
							},
						},
					},
				},
			});
		};
		attempt(10);
	}

	private destroyCharts(): void {
		this._donutChart?.destroy();
		this._barChart?.destroy();
		this._evolucionChart?.destroy();
		this._consolidadoChart?.destroy();
		this._estacionalidadChart?.destroy();
		this._donutChart      = null;
		this._barChart        = null;
		this._evolucionChart  = null;
		this._consolidadoChart = null;
		this._estacionalidadChart = null;
	}

	private renderEstacionalidadChart(e: EstacionalidadPrecios): void {
		const attempt = (tries: number) => {
			this._estacionalidadChart?.destroy();
			this._estacionalidadChart = null;
			const canvas = document.getElementById('estacionalidadChart') as HTMLCanvasElement | null;
			if (!canvas) {
				if (tries > 0) { setTimeout(() => attempt(tries - 1), 50); }
				return;
			}

			const labels = e.porMes.map(m => NOMBRES_MES[m.mes - 1]);

			this._estacionalidadChart = new Chart(canvas, {
				type: 'bar',
				data: {
					labels,
					datasets: [{
						label: 'Precio promedio',
						data: e.porMes.map(m => m.precioPromedio ?? 0),
						backgroundColor: COLOR_INGRESOS,
						borderRadius: 6,
						borderSkipped: false,
					}],
				},
				options: {
					responsive: true,
					maintainAspectRatio: false,
					plugins: {
						legend: { display: false },
						tooltip: { callbacks: { label: (c: { raw: number }) => ' $' + c.raw.toLocaleString('es-CO') } },
					},
					scales: {
						x: { grid: { display: false }, ticks: { color: '#888' } },
						y: {
							grid: { color: 'rgba(0,0,0,0.05)' },
							ticks: {
								color: '#888',
								callback: (v: number | string) => {
									const n = Number(v);
									return n >= 1000000 ? '$' + (n / 1000000).toFixed(1) + 'M'
										 : n >= 1000    ? '$' + Math.round(n / 1000) + 'k'
										 : '$' + n;
								},
							},
						},
					},
				},
			});
		};
		attempt(10);
	}

	private renderConsolidadoChart(c: ResumenConsolidado): void {
		const attempt = (tries: number) => {
			this._consolidadoChart?.destroy();
			this._consolidadoChart = null;
			const canvas = document.getElementById('consolidadoChart') as HTMLCanvasElement | null;
			if (!canvas) {
				if (tries > 0) { setTimeout(() => attempt(tries - 1), 50); }
				return;
			}

			const labels = c.fincas.map(f => f.nombre);

			this._consolidadoChart = new Chart(canvas, {
				type: 'bar',
				data: {
					labels,
					datasets: [
						{
							label: 'Ingresos',
							data: c.fincas.map(f => f.totalIngresos),
							backgroundColor: COLOR_INGRESOS,
							borderRadius: 6,
							borderSkipped: false,
						},
						{
							label: 'Gastos (compras + mano de obra)',
							data: c.fincas.map(f => f.totalCompras + f.totalManoObra),
							backgroundColor: COLOR_COMPRAS,
							borderRadius: 6,
							borderSkipped: false,
						},
					],
				},
				options: {
					responsive: true,
					maintainAspectRatio: false,
					plugins: {
						legend: { position: 'bottom' },
						tooltip: { callbacks: { label: (c: { dataset: { label: string }; raw: number }) => ` ${c.dataset.label}: $${c.raw.toLocaleString('es-CO')}` } },
					},
					scales: {
						x: { grid: { display: false }, ticks: { color: '#888' } },
						y: {
							grid: { color: 'rgba(0,0,0,0.05)' },
							ticks: {
								color: '#888',
								callback: (v: number | string) => {
									const n = Number(v);
									return n >= 1000000 ? '$' + (n / 1000000).toFixed(1) + 'M'
										 : n >= 1000    ? '$' + Math.round(n / 1000) + 'k'
										 : '$' + n;
								},
							},
						},
					},
				},
			});
		};
		attempt(10);
	}

	private renderEvolucionChart(h: HistoricoFinca): void {
		const attempt = (tries: number) => {
			this._evolucionChart?.destroy();
			this._evolucionChart = null;
			const canvas = document.getElementById('evolucionChart') as HTMLCanvasElement | null;
			if (!canvas) {
				if (tries > 0) { setTimeout(() => attempt(tries - 1), 50); }
				return;
			}

			const labels = h.meses.map(m => `${NOMBRES_MES[m.mes - 1]} ${m.anio}`);

			this._evolucionChart = new Chart(canvas, {
				type: 'line',
				data: {
					labels,
					datasets: [
						{
							label: 'Ingresos',
							data: h.meses.map(m => m.totalIngresos),
							borderColor: COLOR_INGRESOS,
							backgroundColor: COLOR_INGRESOS,
							tension: 0.3,
						},
						{
							label: 'Gastos',
							data: h.meses.map(m => m.totalCompras + m.totalManoObra),
							borderColor: COLOR_COMPRAS,
							backgroundColor: COLOR_COMPRAS,
							tension: 0.3,
						},
					],
				},
				options: {
					responsive: true,
					maintainAspectRatio: false,
					plugins: {
						legend: { position: 'bottom' },
						tooltip: { callbacks: { label: (c: { dataset: { label: string }; raw: number }) => ` ${c.dataset.label}: $${c.raw.toLocaleString('es-CO')}` } },
					},
					scales: {
						x: { grid: { display: false }, ticks: { color: '#888' } },
						y: {
							grid: { color: 'rgba(0,0,0,0.05)' },
							ticks: {
								color: '#888',
								callback: (v: number | string) => {
									const n = Number(v);
									return n >= 1000000 ? '$' + (n / 1000000).toFixed(1) + 'M'
										 : n >= 1000    ? '$' + Math.round(n / 1000) + 'k'
										 : '$' + n;
								},
							},
						},
					},
				},
			});
		};
		attempt(10);
	}

	protected imprimir(): void {
		window.print();
	}

	protected exportarCsv(): void {
		if (this.vistaConsolidada()) {
			this.exportarCsvConsolidado();
		} else {
			this.exportarCsvFinca();
		}
	}

	private exportarCsvFinca(): void {
		const r = this.resumen();
		if (!r) { return; }
		const filas: string[] = [];
		filas.push(`Reporte de finca,${this._csvValor(r.nombre)}`);
		filas.push(`Ingresos,${r.totalIngresos}`);
		filas.push(`Compras / Gastos,${r.totalCompras}`);
		filas.push(`Mano de obra,${r.totalManoObra}`);
		filas.push(`Utilidad neta,${r.utilidad}`);
		filas.push('');
		filas.push('Parcela,Producto,Área (ha),Ingresos,Mano de obra,Utilidad');
		for (const p of r.parcelas) {
			const utilidadParcela = p.totalIngresos - p.totalManoObra;
			filas.push([
				this._csvValor(p.nombre),
				this._csvValor(p.producto ?? '—'),
				p.area,
				p.totalIngresos,
				p.totalManoObra,
				utilidadParcela,
			].join(','));
		}
		this._descargarCsv(filas, `reporte-${r.nombre}`);
	}

	private exportarCsvConsolidado(): void {
		const c = this.consolidado();
		if (!c) { return; }
		const filas: string[] = [];
		filas.push('Reporte consolidado de todas las fincas');
		filas.push(`Ingresos totales,${c.totalIngresos}`);
		filas.push(`Compras / Gastos totales,${c.totalCompras}`);
		filas.push(`Mano de obra total,${c.totalManoObra}`);
		filas.push(`Utilidad neta total,${c.utilidad}`);
		filas.push('');
		filas.push('Finca,Ingresos,Compras / Gastos,Mano de obra,Utilidad');
		for (const f of c.fincas) {
			filas.push([this._csvValor(f.nombre), f.totalIngresos, f.totalCompras, f.totalManoObra, f.utilidad].join(','));
		}
		this._descargarCsv(filas, 'reporte-consolidado');
	}

	private _csvValor(valor: string): string {
		return `"${valor.replace(/"/g, '""')}"`;
	}

	private _descargarCsv(filas: string[], nombreArchivo: string): void {
		const contenido = '﻿' + filas.join('\n');
		const blob = new Blob([contenido], { type: 'text/csv;charset=utf-8;' });
		const url = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `${nombreArchivo}.csv`;
		link.click();
		URL.revokeObjectURL(url);
	}
}
