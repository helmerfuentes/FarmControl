import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { ProcesosCultivoService } from '../../core/api/procesos-cultivo.service';
import { ReportesService, ResumenProcesoCultivo } from '../../core/api/reportes.service';
import { FincasService } from '../../core/api/fincas.service';
import { PersonasService } from '../../core/api/personas.service';
import { ProductosService } from '../../core/api/productos.service';
import { AuthService } from '../../core/auth/auth.service';
import { FincaContextService } from '../../core/finca/finca-context.service';
import { ProcesoCultivo, Parcela, Persona, Producto, EstadoProceso } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

@Component({
	selector: 'app-procesos-cultivo',
	standalone: true,
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, DatePipe, CurrencyPipe, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './procesos-cultivo.html',
	styleUrl: './procesos-cultivo.scss',
})
export class ProcesosCultivoComponent implements OnInit {
	private readonly _svc        = inject(ProcesosCultivoService);
	private readonly _reportesSvc = inject(ReportesService);
	private readonly _fincasSvc  = inject(FincasService);
	private readonly _personaSvc = inject(PersonasService);
	private readonly _prodSvc    = inject(ProductosService);
	private readonly _fb         = inject(FormBuilder);
	protected readonly isAdmin   = inject(AuthService).isAdmin;
	protected readonly fincaContext = inject(FincaContextService);

	protected readonly procesos   = signal<ProcesoCultivo[]>([]);
	protected readonly parcelas   = signal<Parcela[]>([]);
	protected readonly socios     = signal<Persona[]>([]);
	protected readonly productos  = signal<Producto[]>([]);
	protected readonly loading    = signal(false);
	protected readonly saving     = signal(false);
	protected readonly drawerOpen = signal(false);
	protected readonly editId     = signal<number | null>(null);
	protected readonly deleteId   = signal<number | null>(null);
	protected readonly errorMsg   = signal('');
	protected readonly cierreId   = signal<number | null>(null);
	protected readonly resumenPanelOpen = signal(false);
	protected readonly resumenProceso = signal<ResumenProcesoCultivo | null>(null);
	protected readonly resumenLoading = signal(false);
	protected readonly resumenError   = signal<string | null>(null);

	protected readonly vistaModo = signal<'tabla' | 'linea-tiempo'>('tabla');

	protected readonly seleccionParaComparar = signal<Set<number>>(new Set());
	protected readonly comparacionPanelOpen  = signal(false);
	protected readonly comparacion           = signal<ResumenProcesoCultivo[]>([]);
	protected readonly comparacionLoading    = signal(false);
	protected readonly comparacionError      = signal<string | null>(null);

	protected readonly filtroParcelaId = signal<number | null>(null);
	protected readonly filtroEstado    = signal<EstadoProceso | null>(null);

	protected readonly estadoOpciones: EstadoProceso[] = ['Activo', 'Cosechado', 'Cerrado'];

	protected readonly form = this._fb.group({
		parcelaId:            [0, [Validators.required, Validators.min(1)]],
		productoId:           [0, [Validators.required, Validators.min(1)]],
		socioId:              [null as number | null],
		costoInicial:         [0, [Validators.required, Validators.min(0)]],
		fechaInicio:          ['', Validators.required],
		fechaEstimadaCosecha: [null as string | null],
	});

	protected readonly cierreForm = this._fb.group({
		estado:      ['Cosechado' as EstadoProceso, Validators.required],
		fechaCierre: [new Date().toISOString().substring(0, 10), Validators.required],
	});

	ngOnInit(): void {
		this.load();
		this._prodSvc.getAll().subscribe(p => this.productos.set(p));
		this._personaSvc.getAll().subscribe(personas =>
			this.socios.set(personas.filter(p => p.tipoPersona === 'Socio' || p.tipoPersona === 'Admin')));
		this._fincasSvc.getAll().subscribe(fincas => {
			const todas: Parcela[] = fincas.flatMap(f =>
				(f as any).parcelas?.map((p: Parcela) => ({ ...p, fincaId: f.id, fincaNombre: f.nombre })) ?? []
			);
			this.parcelas.set(todas);
		});
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getAll(
			this.filtroParcelaId() ?? undefined,
			this.filtroEstado() ?? undefined
		).subscribe({
			next:  p => { this.procesos.set(p); this.loading.set(false); },
			error: () => this.loading.set(false),
		});
	}

	protected openNew(): void {
		this.editId.set(null);
		this.errorMsg.set('');
		this.form.reset({ productoId: 0, parcelaId: 0, costoInicial: 0, socioId: null, fechaEstimadaCosecha: null });
		this.drawerOpen.set(true);
	}

	protected openEdit(p: ProcesoCultivo): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(p.id);
		this.errorMsg.set('');
		this.form.setValue({
			parcelaId:            p.parcelaId,
			productoId:           p.productoId,
			socioId:              p.socioId ?? null,
			costoInicial:         p.costoInicial,
			fechaInicio:          p.fechaInicio.substring(0, 10),
			fechaEstimadaCosecha: p.fechaEstimadaCosecha ? p.fechaEstimadaCosecha.substring(0, 10) : null,
		});
		this.drawerOpen.set(true);
	}

	protected verResumen(id: number): void {
		this.resumenPanelOpen.set(true);
		this.resumenProceso.set(null);
		this.resumenError.set(null);
		this.resumenLoading.set(true);
		this._reportesSvc.getResumenProceso(id).subscribe({
			next:  r => { this.resumenProceso.set(r); this.resumenLoading.set(false); },
			error: () => { this.resumenError.set('No se pudo cargar el resumen de este ciclo.'); this.resumenLoading.set(false); },
		});
	}

	protected cerrarResumen(): void {
		this.resumenPanelOpen.set(false);
		this.resumenProceso.set(null);
		this.resumenError.set(null);
	}

	protected setVistaModo(modo: 'tabla' | 'linea-tiempo'): void {
		this.vistaModo.set(modo);
	}

	protected readonly lineaTiempoItems = computed(() =>
		[...this.filtered()].sort((a, b) => a.fechaInicio.localeCompare(b.fechaInicio)));

	protected toggleSeleccionParaComparar(id: number, checked: boolean): void {
		this.seleccionParaComparar.update(set => {
			const nuevo = new Set(set);
			if (checked) { nuevo.add(id); } else { nuevo.delete(id); }
			return nuevo;
		});
	}

	protected estaSeleccionadoParaComparar(id: number): boolean {
		return this.seleccionParaComparar().has(id);
	}

	protected compararSeleccionados(): void {
		const ids = Array.from(this.seleccionParaComparar());
		if (ids.length < 2) { return; }
		this.comparacionPanelOpen.set(true);
		this.comparacion.set([]);
		this.comparacionError.set(null);
		this.comparacionLoading.set(true);
		forkJoin(ids.map(id => this._reportesSvc.getResumenProceso(id))).subscribe({
			next:  resultados => { this.comparacion.set(resultados); this.comparacionLoading.set(false); },
			error: () => { this.comparacionError.set('No se pudo cargar la comparación.'); this.comparacionLoading.set(false); },
		});
	}

	protected cerrarComparacion(): void {
		this.comparacionPanelOpen.set(false);
		this.comparacion.set([]);
		this.comparacionError.set(null);
	}

	protected openCierre(id: number): void {
		this.cierreForm.reset({
			estado: 'Cosechado',
			fechaCierre: new Date().toISOString().substring(0, 10),
		});
		this.cierreId.set(id);
	}

	protected cancelCierre(): void {
		this.cierreId.set(null);
	}

	protected doCierre(): void {
		const id = this.cierreId();
		if (id === null || this.cierreForm.invalid) { return; }
		const raw = this.cierreForm.value;
		this._svc.cerrar(id, { estado: raw.estado as EstadoProceso, fechaCierre: raw.fechaCierre! }).subscribe({
			next: () => { this.cierreId.set(null); this.load(); },
		});
	}

	protected save(): void {
		if (this.form.invalid || this.saving()) { return; }
		const raw = this.form.value;
		this.saving.set(true);
		this.errorMsg.set('');
		const id = this.editId();

		if (id) {
			const existing = this.procesos().find(p => p.id === id);
			const payload = {
				id,
				parcelaId:            Number(raw.parcelaId),
				productoId:           Number(raw.productoId),
				socioId:              raw.socioId ? Number(raw.socioId) : null,
				costoInicial:         Number(raw.costoInicial),
				fechaInicio:          raw.fechaInicio!,
				fechaEstimadaCosecha: raw.fechaEstimadaCosecha ?? null,
				fechaCierre:          existing?.fechaCierre ?? null,
				estado:               existing?.estado ?? 'Activo' as EstadoProceso,
			};
			this._svc.update(id, payload).subscribe({
				next:  () => { this.saving.set(false); this.drawerOpen.set(false); this.load(); },
				error: () => this.saving.set(false),
			});
		} else {
			const payload = {
				parcelaId:            Number(raw.parcelaId),
				productoId:           Number(raw.productoId),
				socioId:              raw.socioId ? Number(raw.socioId) : null,
				costoInicial:         Number(raw.costoInicial),
				fechaInicio:          raw.fechaInicio!,
				fechaEstimadaCosecha: raw.fechaEstimadaCosecha ?? null,
			};
			this._svc.create(payload).subscribe({
				next:  () => { this.saving.set(false); this.drawerOpen.set(false); this.load(); },
				error: (err) => {
					this.saving.set(false);
					const msg = err?.error?.message ?? err?.error ?? 'Error al crear el proceso.';
					this.errorMsg.set(typeof msg === 'string' ? msg : 'La parcela ya tiene un proceso activo. Ciérralo antes de abrir uno nuevo.');
				},
			});
		}
	}

	protected readonly deleteErrorMsg = signal<string | null>(null);

	protected confirmDelete(id: number): void {
		this.deleteErrorMsg.set(null);
		this.deleteId.set(id);
	}

	protected doDelete(): void {
		const id = this.deleteId();
		if (id === null) { return; }
		this._svc.delete(id).subscribe({
			next:  () => { this.deleteId.set(null); this.load(); },
			error: err => {
				this.deleteId.set(null);
				this.deleteErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar el proceso de cultivo.');
			},
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected estadoBadgeClass(estado: EstadoProceso): string {
		const map: Record<EstadoProceso, string> = {
			Activo:    'badge-activo',
			Cosechado: 'badge-cosechado',
			Cerrado:   'badge-cerrado',
		};
		return map[estado];
	}

	protected applyFiltro(parcelaId: number | null, estado: EstadoProceso | null): void {
		this.filtroParcelaId.set(parcelaId);
		this.filtroEstado.set(estado);
		this.load();
	}

	protected readonly search   = signal('');
	protected readonly page     = signal(1);
	protected readonly pageSize = signal(10);

	protected readonly parcelasDeFincaActiva = computed(() => {
		const fincaId = this.fincaContext.selectedFincaId();
		if (fincaId === null) { return this.parcelas(); }
		return this.parcelas().filter(p => p.fincaId === fincaId);
	});

	protected readonly parcelaIdsFincaActiva = computed(() => {
		const fincaId = this.fincaContext.selectedFincaId();
		if (fincaId === null) { return null; }
		return new Set(this.parcelas().filter(p => p.fincaId === fincaId).map(p => p.id));
	});

	protected readonly filtered = computed(() => {
		const ids = this.parcelaIdsFincaActiva();
		const base = ids ? this.procesos().filter(item => ids.has(item.parcelaId)) : this.procesos();
		const term = this.search().toLowerCase().trim();
		if (!term) { return base; }
		return base.filter(item => JSON.stringify(item).toLowerCase().includes(term));
	});

	protected readonly totalFiltered = computed(() => this.filtered().length);

	protected readonly pageItems = computed(() => {
		const start = (this.page() - 1) * this.pageSize();
		return this.filtered().slice(start, start + this.pageSize());
	});

	protected onSearch(term: string): void {
		this.search.set(term);
		this.page.set(1);
	}

	protected onPageChange(p: number): void {
		this.page.set(p);
	}

	protected onPageSizeChange(s: number): void {
		this.pageSize.set(s);
		this.page.set(1);
	}
}
