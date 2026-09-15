import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { InsumosService, SaldoInsumo, HistorialPrecioInsumo } from '../../core/api/insumos.service';
import { TiposInsumoService } from '../../core/api/tipos-insumo.service';
import { FincasService } from '../../core/api/fincas.service';
import { AuthService } from '../../core/auth/auth.service';
import { FincaContextService } from '../../core/finca/finca-context.service';
import { Insumo, MovimientoInsumo, TipoInsumo, Parcela, Finca } from '../../core/models';

interface ParcelaConFinca extends Parcela {
	fincaNombre: string;
}
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

type ViewTab = 'insumos' | 'movimientos' | 'saldos' | 'precios';

@Component({
	selector: 'app-inventario',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, DatePipe, CurrencyPipe, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './inventario.html',
	styleUrl: './inventario.scss',
})
export class InventarioComponent implements OnInit {
	private readonly _svc     = inject(InsumosService);
	private readonly _tipoSvc = inject(TiposInsumoService);
	private readonly _fincaSvc = inject(FincasService);
	private readonly _fb      = inject(FormBuilder);
	protected readonly isAdmin = inject(AuthService).isAdmin;
	protected readonly fincaContext = inject(FincaContextService);

	protected readonly tab           = signal<ViewTab>('insumos');
	protected readonly insumos       = signal<Insumo[]>([]);
	protected readonly movimientos   = signal<MovimientoInsumo[]>([]);
	protected readonly saldos        = signal<SaldoInsumo[]>([]);
	protected readonly loadingSaldos = signal(false);
	protected readonly tipoMovimientoActual = signal<'Entrada' | 'Salida'>('Entrada');
	protected readonly selectedInsumoIdPrecios = signal<number | null>(null);
	protected readonly historialPrecio = signal<HistorialPrecioInsumo | null>(null);
	protected readonly loadingPrecios  = signal(false);
	protected readonly tipos         = signal<TipoInsumo[]>([]);
	protected readonly parcelas      = signal<ParcelaConFinca[]>([]);
	protected readonly loading       = signal(false);
	protected readonly saving        = signal(false);
	protected readonly panelOpen     = signal(false);
	protected readonly editId        = signal<number | null>(null);
	protected readonly deleteId      = signal<number | null>(null);
	protected readonly panelMode     = signal<'insumo' | 'movimiento' | 'transferencia'>('insumo');

	protected readonly insumoForm = this._fb.group({
		tipoInsumoId:  [0, [Validators.required, Validators.min(1)]],
		nombre:        ['', Validators.required],
		marca:         [''],
		descripcion:   [''],
		precioUnitario:[0, [Validators.required, Validators.min(0)]],
		unidadMedida:  ['', Validators.required],
		stockMinimo:   [null as number | null, Validators.min(0)],
		fechaVencimiento: [null as string | null],
	});

	protected readonly transferForm = this._fb.group({
		insumoId:         [0, [Validators.required, Validators.min(1)]],
		parcelaOrigenId:  [0, [Validators.required, Validators.min(1)]],
		parcelaDestinoId: [0, [Validators.required, Validators.min(1)]],
		cantidad:         [0, [Validators.required, Validators.min(0.01)]],
		fecha:            ['', Validators.required],
		observacion:      [''],
	});

	protected readonly movForm = this._fb.group({
		insumoId:       [0, [Validators.required, Validators.min(1)]],
		parcelaId:      [0, [Validators.required, Validators.min(1)]],
		tipoMovimiento: ['Entrada', Validators.required],
		cantidad:       [0, [Validators.required, Validators.min(0.01)]],
		fecha:          ['', Validators.required],
		observacion:    [''],
		precioUnitario: [null as number | null, Validators.min(0)],
	});

	ngOnInit(): void {
		this._tipoSvc.getAll().subscribe(t => this.tipos.set(t));
		this.loadAll();
	}

	protected loadAll(): void {
		this.loading.set(true);
		this._svc.getAll().subscribe(i => { this.insumos.set(i); this.loading.set(false); });
		this._svc.getMovimientos().subscribe(m => this.movimientos.set(m));
		this.loadSaldos();
		// Load parcelas from all fincas for movement form
		this._fincaSvc.getAll().subscribe((fincas: Finca[]) => {
			const calls = fincas.map(f => ({ finca: f, obs: this._fincaSvc.getParcelas(f.id) }));
			const all: ParcelaConFinca[] = [];
			let pending = calls.length;
			if (pending === 0) { return; }
			calls.forEach(({ finca, obs }) => obs.subscribe(parcelas => {
				parcelas.forEach(p => all.push({ ...p, fincaId: finca.id, fincaNombre: finca.nombre }));
				if (--pending === 0) { this.parcelas.set(all); }
			}));
		});
	}

	protected setTab(t: ViewTab): void {
		this.tab.set(t);
	}

	protected loadSaldos(): void {
		this.loadingSaldos.set(true);
		this._svc.getSaldos().subscribe({
			next:  s => { this.saldos.set(s); this.loadingSaldos.set(false); },
			error: () => this.loadingSaldos.set(false),
		});
	}

	protected onSelectInsumoPrecios(value: string): void {
		const insumoId = Number(value) || null;
		this.selectedInsumoIdPrecios.set(insumoId);
		if (insumoId === null) {
			this.historialPrecio.set(null);
			return;
		}
		this.loadingPrecios.set(true);
		this._svc.getHistorialPrecios(insumoId).subscribe({
			next:  data => { this.historialPrecio.set(data); this.loadingPrecios.set(false); },
			error: ()   => { this.historialPrecio.set(null); this.loadingPrecios.set(false); },
		});
	}

	protected openNewInsumo(): void {
		this.editId.set(null);
		this.insumoForm.reset({ tipoInsumoId: 0, precioUnitario: 0, stockMinimo: null, fechaVencimiento: null });
		this.panelMode.set('insumo');
		this.panelOpen.set(true);
	}

	protected openEditInsumo(item: Insumo): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(item.id);
		this.insumoForm.setValue({
			tipoInsumoId: item.tipoInsumoId, nombre: item.nombre,
			marca: item.marca, descripcion: item.descripcion,
			precioUnitario: item.precioUnitario, unidadMedida: item.unidadMedida,
			stockMinimo: item.stockMinimo,
			fechaVencimiento: item.fechaVencimiento ? item.fechaVencimiento.substring(0, 10) : null,
		});
		this.panelMode.set('insumo');
		this.panelOpen.set(true);
	}

	protected openNewTransferencia(): void {
		const today = new Date().toISOString().substring(0, 10);
		this.transferForm.reset({ cantidad: 0, fecha: today });
		this.panelMode.set('transferencia');
		this.panelOpen.set(true);
	}

	protected openNewMovimiento(): void {
		const today = new Date().toISOString().substring(0, 10);
		this.movForm.reset({ tipoMovimiento: 'Entrada', cantidad: 0, fecha: today, precioUnitario: null });
		this.tipoMovimientoActual.set('Entrada');
		this.panelMode.set('movimiento');
		this.panelOpen.set(true);
	}

	protected onTipoMovimientoChange(value: string): void {
		this.tipoMovimientoActual.set(value as 'Entrada' | 'Salida');
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected saveInsumo(): void {
		if (this.insumoForm.invalid || this.saving()) { return; }
		const raw = this.insumoForm.value;
		const value = {
			tipoInsumoId:   Number(raw.tipoInsumoId),
			nombre:         raw.nombre!,
			marca:          raw.marca ?? '',
			descripcion:    raw.descripcion ?? '',
			precioUnitario: Number(raw.precioUnitario),
			unidadMedida:   raw.unidadMedida!,
			stockMinimo:    raw.stockMinimo == null ? null : Number(raw.stockMinimo),
			fechaVencimiento: raw.fechaVencimiento || null,
		};
		this.saving.set(true);
		const id = this.editId();
		const req = id ? this._svc.update(id, value) : this._svc.create(value);
		req.subscribe({
			next:  () => { this.saving.set(false); this.closePanel(); this.loadAll(); },
			error: () => this.saving.set(false),
		});
	}

	protected readonly transferErrorMsg = signal<string | null>(null);

	protected saveTransferencia(): void {
		if (this.transferForm.invalid || this.saving()) { return; }
		const raw = this.transferForm.value;
		if (Number(raw.parcelaOrigenId) === Number(raw.parcelaDestinoId)) {
			this.transferErrorMsg.set('La parcela de origen y destino deben ser diferentes.');
			return;
		}
		this.transferErrorMsg.set(null);
		const value = {
			insumoId:         Number(raw.insumoId),
			parcelaOrigenId:  Number(raw.parcelaOrigenId),
			parcelaDestinoId: Number(raw.parcelaDestinoId),
			cantidad:         Number(raw.cantidad),
			fecha:            raw.fecha!,
			observacion:      raw.observacion || null,
		};
		this.saving.set(true);
		this._svc.transferir(value).subscribe({
			next:  () => { this.saving.set(false); this.closePanel(); this.loadAll(); },
			error: err => {
				this.saving.set(false);
				this.transferErrorMsg.set(err?.error?.error ?? 'No se pudo transferir el insumo.');
			},
		});
	}

	protected saveMovimiento(): void {
		if (this.movForm.invalid || this.saving()) { return; }
		const raw = this.movForm.value;
		const value = {
			insumoId:       Number(raw.insumoId),
			parcelaId:      Number(raw.parcelaId),
			tipoMovimiento: raw.tipoMovimiento as 'Entrada' | 'Salida',
			cantidad:       Number(raw.cantidad),
			fecha:          raw.fecha!,
			observacion:    raw.observacion ?? '',
			precioUnitario: raw.tipoMovimiento === 'Entrada' && raw.precioUnitario != null ? Number(raw.precioUnitario) : null,
		};
		this.saving.set(true);
		this._svc.createMovimiento(value).subscribe({
			next:  () => { this.saving.set(false); this.closePanel(); this.loadAll(); },
			error: () => this.saving.set(false),
		});
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
			next:  () => { this.deleteId.set(null); this.loadAll(); },
			error: err => {
				this.deleteId.set(null);
				this.deleteErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar el insumo.');
			},
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected drawerTitle(): string {
		if (this.panelMode() === 'movimiento') { return 'Registrar movimiento'; }
		if (this.panelMode() === 'transferencia') { return 'Transferir insumo entre parcelas'; }
		return this.editId() ? 'Editar insumo' : 'Nuevo insumo';
	}

	// --- Insumos pagination ---
	protected readonly searchInsumos   = signal('');
	protected readonly pageInsumos     = signal(1);
	protected readonly pageSizeInsumos = signal(10);

	protected readonly filteredInsumos = computed(() => {
		const term = this.searchInsumos().toLowerCase().trim();
		if (!term) { return this.insumos(); }
		return this.insumos().filter(item => JSON.stringify(item).toLowerCase().includes(term));
	});

	protected readonly totalInsumos = computed(() => this.filteredInsumos().length);

	protected readonly pageInsumosItems = computed(() => {
		const start = (this.pageInsumos() - 1) * this.pageSizeInsumos();
		return this.filteredInsumos().slice(start, start + this.pageSizeInsumos());
	});

	protected onSearchInsumos(term: string): void {
		this.searchInsumos.set(term);
		this.pageInsumos.set(1);
	}

	protected onPageInsumos(p: number): void {
		this.pageInsumos.set(p);
	}

	protected onPageSizeInsumos(s: number): void {
		this.pageSizeInsumos.set(s);
		this.pageInsumos.set(1);
	}

	// --- Movimientos pagination ---
	protected readonly searchMov   = signal('');
	protected readonly pageMov     = signal(1);
	protected readonly pageSizeMov = signal(10);

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

	protected readonly filteredMov = computed(() => {
		const ids = this.parcelaIdsFincaActiva();
		const base = ids ? this.movimientos().filter(item => ids.has(item.parcelaId)) : this.movimientos();
		const term = this.searchMov().toLowerCase().trim();
		if (!term) { return base; }
		return base.filter(item => JSON.stringify(item).toLowerCase().includes(term));
	});

	protected readonly totalMov = computed(() => this.filteredMov().length);

	protected readonly pageMovItems = computed(() => {
		const start = (this.pageMov() - 1) * this.pageSizeMov();
		return this.filteredMov().slice(start, start + this.pageSizeMov());
	});

	protected onSearchMov(term: string): void {
		this.searchMov.set(term);
		this.pageMov.set(1);
	}

	protected onPageMov(p: number): void {
		this.pageMov.set(p);
	}

	protected onPageSizeMov(s: number): void {
		this.pageSizeMov.set(s);
		this.pageMov.set(1);
	}
}
