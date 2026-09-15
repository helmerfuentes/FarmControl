import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormArray, Validators } from '@angular/forms';
import { VentasService } from '../../core/api/ventas.service';
import { FincasService } from '../../core/api/fincas.service';
import { PersonasService } from '../../core/api/personas.service';
import { ProductosService } from '../../core/api/productos.service';
import { ProcesosCultivoService } from '../../core/api/procesos-cultivo.service';
import { AuthService } from '../../core/auth/auth.service';
import { FincaContextService } from '../../core/finca/finca-context.service';
import { Venta, Parcela, Persona, Producto, Finca, ProcesoCultivo, PagoVenta, CompradorFrecuente } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';
import { ComentariosPanelComponent } from '../../shared/components/comentarios-panel/comentarios-panel';

interface ParcelaConFinca extends Parcela {
	fincaNombre: string;
}

@Component({
	selector: 'app-ventas',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, DatePipe, CurrencyPipe, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent, ComentariosPanelComponent],
	templateUrl: './ventas.html',
	styleUrl: './ventas.scss',
})
export class VentasComponent implements OnInit {
	private readonly _svc        = inject(VentasService);
	private readonly _fincaSvc   = inject(FincasService);
	private readonly _personaSvc = inject(PersonasService);
	private readonly _prodSvc    = inject(ProductosService);
	private readonly _procesoSvc = inject(ProcesosCultivoService);
	private readonly _fb         = inject(FormBuilder);
	protected readonly isAdmin   = inject(AuthService).isAdmin;
	protected readonly fincaContext = inject(FincaContextService);

	protected readonly items         = signal<Venta[]>([]);
	protected readonly parcelas      = signal<ParcelaConFinca[]>([]);
	protected readonly compradores   = signal<Persona[]>([]);
	protected readonly loading       = signal(false);
	protected readonly saving        = signal(false);
	protected readonly panelOpen     = signal(false);
	protected readonly editId        = signal<number | null>(null);
	protected readonly deleteId      = signal<number | null>(null);
	protected readonly clasifOptions = signal<string[]>([]);
	protected readonly activeProcess = signal<ProcesoCultivo | null>(null);
	protected readonly noActiveProcess = signal(false);

	protected readonly form = this._fb.group({
		parcelaId:      [0, [Validators.required, Validators.min(1)]],
		compradorId:    [0, [Validators.required, Validators.min(1)]],
		fecha:          ['', Validators.required],
		valorTransporte:[0, [Validators.required, Validators.min(0)]],
		detalles:       this._fb.array([]),
	});

	get detallesArray(): FormArray {
		return this.form.get('detalles') as FormArray;
	}

	ngOnInit(): void {
		this.load();
		this._personaSvc.getAll('Comprador').subscribe(p => this.compradores.set(p));
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

	protected load(): void {
		this.loading.set(true);
		this._svc.getAll().subscribe({
			next:  items => { this.items.set(items); this.loading.set(false); },
			error: ()    => this.loading.set(false),
		});
	}

	protected onParcelaChange(parcelaId: string): void {
		const id = Number(parcelaId);
		this.activeProcess.set(null);
		this.noActiveProcess.set(false);
		this.detallesArray.clear();
		this.clasifOptions.set([]);
		if (!id) { return; }
		this._procesoSvc.getAll(id, 'Activo').subscribe(procesos => {
			if (!procesos.length) {
				this.noActiveProcess.set(true);
				return;
			}
			const proceso = procesos[0];
			this.activeProcess.set(proceso);
			this._prodSvc.getById(proceso.productoId).subscribe(producto => {
				const clasifs = (producto.clasificaciones ?? []).map(c => c.nombre);
				this.clasifOptions.set(clasifs);
				this.detallesArray.clear();
				(producto.clasificaciones ?? []).forEach(c => this.addDetalle(c.nombre, c.unidadMedida));
			});
		});
	}

	protected addDetalle(clasificacion = '', unidadMedida = ''): void {
		this.detallesArray.push(this._fb.group({
			clasificacion:  [clasificacion, Validators.required],
			cantidad:       [0, [Validators.required, Validators.min(0)]],
			unidadMedida:   [unidadMedida, Validators.required],
			precioUnitario: [0, [Validators.required, Validators.min(0)]],
			subtotal:       [{ value: 0, disabled: true }],
		}));
	}

	protected updateSubtotal(i: number): void {
		const grupo = this.detallesArray.at(i);
		const cantidad = Number(grupo.get('cantidad')?.value ?? 0);
		const precio   = Number(grupo.get('precioUnitario')?.value ?? 0);
		grupo.get('subtotal')?.setValue(cantidad * precio);
	}

	protected totalVenta(): number {
		const subtotales = this.detallesArray.controls.reduce((s, g) => {
			return s + Number(g.get('subtotal')?.value ?? 0);
		}, 0);
		return subtotales - Number(this.form.get('valorTransporte')?.value ?? 0);
	}

	protected openNew(): void {
		this.editId.set(null);
		const today = new Date().toISOString().substring(0, 10);
		this.form.reset({ valorTransporte: 0, fecha: today });
		this.detallesArray.clear();
		this.clasifOptions.set([]);
		this.activeProcess.set(null);
		this.noActiveProcess.set(false);
		this.panelOpen.set(true);
	}

	protected openView(item: Venta): void {
		this.editId.set(item.id);
		this.form.patchValue({
			parcelaId:       item.parcelaId,
			compradorId:     item.compradorId,
			fecha:           item.fecha.substring(0, 10),
			valorTransporte: item.valorTransporte,
		});
		this.detallesArray.clear();
		(item.detalles ?? []).forEach(d => {
			this.detallesArray.push(this._fb.group({
				clasificacion:  [d.clasificacion],
				cantidad:       [d.cantidad],
				unidadMedida:   [d.unidadMedida],
				precioUnitario: [d.precioUnitario],
				subtotal:       [{ value: d.subtotal, disabled: true }],
			}));
		});
		this.panelOpen.set(true);
		this.loadPagos(item.id);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
		this.pagos.set(null);
		this.pagoErrorMsg.set(null);
		this.pagoForm.reset({ monto: 0, fecha: new Date().toISOString().substring(0, 10), observacion: '' });
	}

	protected save(): void {
		if (this.form.invalid || this.saving()) { return; }
		const raw = this.form.value;
		const detalles = (raw.detalles as Array<{
			clasificacion: string; cantidad: number;
			unidadMedida: string; precioUnitario: number; subtotal: number;
		}>).map(d => ({
			clasificacion:  d.clasificacion,
			cantidad:       Number(d.cantidad),
			unidadMedida:   d.unidadMedida,
			precioUnitario: Number(d.precioUnitario),
			subtotal:       Number(d.cantidad) * Number(d.precioUnitario),
			ventaId:        0,
			id:             0,
		}));
		const value = {
			parcelaId:       Number(raw.parcelaId),
			compradorId:     Number(raw.compradorId),
			fecha:           raw.fecha!,
			valorTransporte: Number(raw.valorTransporte ?? 0),
			total:           this.totalVenta(),
			detalles,
		};
		this.saving.set(true);
		const id = this.editId();
		const req = id ? this._svc.update(id, value) : this._svc.create(value);
		req.subscribe({
			next:  () => { this.saving.set(false); this.closePanel(); this.load(); },
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
			next:  () => { this.deleteId.set(null); this.load(); },
			error: err => {
				this.deleteId.set(null);
				this.deleteErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar la venta.');
			},
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected itemSubtotal(venta: Venta): number {
		return (venta.detalles ?? []).reduce((s, d) => s + d.subtotal, 0);
	}

	protected totalGlobal(): number {
		return this.filtered().reduce((s, v) => s + this.itemSubtotal(v), 0);
	}

	protected drawerTitle(): string {
		return this.editId() ? 'Detalle de venta' : 'Registrar venta';
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
		const base = ids ? this.items().filter(item => ids.has(item.parcelaId)) : this.items();
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

	// --- Feature 13: cuentas por cobrar (pagos) ---

	protected readonly pagos = signal<PagoVenta[] | null>(null);
	protected readonly loadingPagos = signal(false);
	protected readonly savingPago = signal(false);
	protected readonly pagoErrorMsg = signal<string | null>(null);

	protected readonly pagoForm = this._fb.group({
		monto:       [0, [Validators.required, Validators.min(0.01)]],
		fecha:       [new Date().toISOString().substring(0, 10), Validators.required],
		observacion: [''],
	});

	private loadPagos(ventaId: number): void {
		this.loadingPagos.set(true);
		this._svc.getPagos(ventaId).subscribe({
			next:  p => { this.pagos.set(p); this.loadingPagos.set(false); },
			error: () => { this.pagos.set([]); this.loadingPagos.set(false); },
		});
	}

	protected saldoPendienteActual(): number {
		const id = this.editId();
		if (id === null) { return 0; }
		const venta = this.items().find(v => v.id === id);
		return venta?.saldoPendiente ?? 0;
	}

	protected registrarPago(): void {
		const id = this.editId();
		if (id === null || this.pagoForm.invalid || this.savingPago()) { return; }
		const raw = this.pagoForm.value;
		const monto = Number(raw.monto ?? 0);
		const saldo = this.saldoPendienteActual();
		if (saldo > 0 && monto > saldo) {
			this.pagoErrorMsg.set('El pago no puede superar el saldo pendiente.');
			return;
		}
		this.pagoErrorMsg.set(null);
		this.savingPago.set(true);
		this._svc.registrarPago(id, {
			monto,
			fecha: raw.fecha!,
			observacion: raw.observacion || null,
		}).subscribe({
			next: () => {
				this.savingPago.set(false);
				this.pagoForm.reset({ monto: 0, fecha: new Date().toISOString().substring(0, 10), observacion: '' });
				this.loadPagos(id);
				this.load();
			},
			error: err => {
				this.savingPago.set(false);
				this.pagoErrorMsg.set(err?.error?.error ?? 'No se pudo registrar el pago.');
			},
		});
	}

	// --- Feature 11: recibo de venta imprimible ---

	protected readonly reciboVenta = signal<Venta | null>(null);

	protected imprimirRecibo(item: Venta): void {
		this.reciboVenta.set(item);
		setTimeout(() => {
			window.print();
			this.reciboVenta.set(null);
		}, 0);
	}

	// --- Feature 14: compradores frecuentes ---

	protected readonly vistaCompradores = signal(false);
	protected readonly compradoresFrecuentes = signal<CompradorFrecuente[]>([]);
	protected readonly loadingCompradores = signal(false);

	protected toggleVistaCompradores(): void {
		this.vistaCompradores.update(v => !v);
		if (this.vistaCompradores() && this.compradoresFrecuentes().length === 0) {
			this.loadCompradoresFrecuentes();
		}
	}

	protected loadCompradoresFrecuentes(): void {
		this.loadingCompradores.set(true);
		this._svc.getCompradoresFrecuentes().subscribe({
			next:  c => { this.compradoresFrecuentes.set(c); this.loadingCompradores.set(false); },
			error: () => this.loadingCompradores.set(false),
		});
	}
}
