import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ComprasService } from '../../core/api/compras.service';
import { FincasService } from '../../core/api/fincas.service';
import { PersonasService } from '../../core/api/personas.service';
import { AuthService } from '../../core/auth/auth.service';
import { FincaContextService } from '../../core/finca/finca-context.service';
import { Compra, Finca, Parcela, Persona, TipoCompra, PagoCompra } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';
import { ComentariosPanelComponent } from '../../shared/components/comentarios-panel/comentarios-panel';

@Component({
	selector: 'app-compras',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, DatePipe, CurrencyPipe, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent, ComentariosPanelComponent],
	templateUrl: './compras.html',
	styleUrl: './compras.scss',
})
export class ComprasComponent implements OnInit {
	private readonly _svc     = inject(ComprasService);
	private readonly _fincaSvc = inject(FincasService);
	private readonly _personaSvc = inject(PersonasService);
	private readonly _fb      = inject(FormBuilder);
	protected readonly isAdmin = inject(AuthService).isAdmin;
	protected readonly fincaContext = inject(FincaContextService);

	protected readonly items        = signal<Compra[]>([]);
	protected readonly fincas       = signal<Finca[]>([]);
	protected readonly todasParcelas = signal<(Parcela & { fincaNombre: string })[]>([]);
	protected readonly parcelas     = signal<(Parcela & { fincaNombre: string })[]>([]);
	protected readonly socios       = signal<Persona[]>([]);
	protected readonly loading      = signal(false);
	protected readonly saving       = signal(false);
	protected readonly panelOpen    = signal(false);
	protected readonly editId       = signal<number | null>(null);
	protected readonly deleteId     = signal<number | null>(null);

	protected readonly tiposCompra: { value: TipoCompra; label: string }[] = [
		{ value: 'Insumo',     label: 'Insumo' },
		{ value: 'Maquinaria', label: 'Maquinaria' },
		{ value: 'Otro',       label: 'Otro' },
	];

	protected readonly form = this._fb.group({
		fincaId:    [0, [Validators.required, Validators.min(1)]],
		parcelaId:  [null as number | null],
		socioId:    [0, [Validators.required, Validators.min(1)]],
		descripcion:['', Validators.required],
		valor:      [0, [Validators.required, Validators.min(0)]],
		fecha:      ['', Validators.required],
		tipoCompra: ['Insumo' as TipoCompra, Validators.required],
		adjuntoUrl: [''],
		proveedor:  [''],
	});

	ngOnInit(): void {
		this.load();
		this._fincaSvc.getAll().subscribe(f => {
			this.fincas.set(f);
			const todas = f.flatMap(finca =>
				(finca as any).parcelas?.map((p: Parcela) => ({ ...p, fincaId: finca.id, fincaNombre: finca.nombre })) ?? []
			);
			this.todasParcelas.set(todas);
		});
		this._personaSvc.getAll().subscribe(personas =>
			this.socios.set(personas.filter(p => p.tipoPersona === 'Socio' || p.tipoPersona === 'Admin')));
	}

	protected onFincaChange(fincaId: string): void {
		const id = Number(fincaId);
		this.form.patchValue({ parcelaId: null });
		this.parcelas.set(id ? this.todasParcelas().filter(p => p.fincaId === id) : []);
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getAll().subscribe({
			next:  items => { this.items.set(items); this.loading.set(false); },
			error: ()    => this.loading.set(false),
		});
	}

	protected openNew(): void {
		this.editId.set(null);
		const today = new Date().toISOString().substring(0, 10);
		this.form.reset({ tipoCompra: 'Insumo', valor: 0, fecha: today, parcelaId: null, proveedor: '' });
		this.parcelas.set([]);
		this.panelOpen.set(true);
	}

	protected openEdit(item: Compra): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(item.id);
		const parcelasForFinca = item.fincaId
			? this.todasParcelas().filter(p => p.fincaId === item.fincaId)
			: [];
		this.parcelas.set(parcelasForFinca);
		this.form.setValue({
			fincaId:     item.fincaId,
			parcelaId:   item.parcelaId ?? null,
			socioId:     item.socioId,
			descripcion: item.descripcion,
			valor:       item.valor,
			fecha:       item.fecha.substring(0, 10),
			tipoCompra:  item.tipoCompra,
			adjuntoUrl:  item.adjuntoUrl,
			proveedor:   item.proveedor ?? '',
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
		const value = {
			fincaId:     Number(raw.fincaId),
			parcelaId:   raw.parcelaId ? Number(raw.parcelaId) : null,
			socioId:     Number(raw.socioId),
			descripcion: raw.descripcion!,
			valor:       Number(raw.valor),
			fecha:       raw.fecha!,
			tipoCompra:  raw.tipoCompra as TipoCompra,
			adjuntoUrl:  raw.adjuntoUrl ?? '',
			proveedor:   raw.proveedor || null,
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
				this.deleteErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar la compra.');
			},
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected total(): number {
		return this.filtered().reduce((s, c) => s + c.valor, 0);
	}

	protected drawerTitle(): string {
		return this.editId() ? 'Editar compra' : 'Registrar compra';
	}

	protected readonly search   = signal('');
	protected readonly page     = signal(1);
	protected readonly pageSize = signal(10);

	protected readonly filtered = computed(() => {
		const fincaId = this.fincaContext.selectedFincaId();
		const base = fincaId !== null ? this.items().filter(item => item.fincaId === fincaId) : this.items();
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

	// --- Feature 6: cuentas por pagar (pagos) ---

	protected readonly pagos = signal<PagoCompra[] | null>(null);
	protected readonly loadingPagos = signal(false);
	protected readonly savingPago = signal(false);
	protected readonly pagoErrorMsg = signal<string | null>(null);

	protected readonly pagoForm = this._fb.group({
		monto:       [0, [Validators.required, Validators.min(0.01)]],
		fecha:       [new Date().toISOString().substring(0, 10), Validators.required],
		observacion: [''],
	});

	private loadPagos(compraId: number): void {
		this.loadingPagos.set(true);
		this._svc.getPagos(compraId).subscribe({
			next:  p => { this.pagos.set(p); this.loadingPagos.set(false); },
			error: () => { this.pagos.set([]); this.loadingPagos.set(false); },
		});
	}

	protected saldoPendienteActual(): number {
		const id = this.editId();
		if (id === null) { return 0; }
		const compra = this.items().find(c => c.id === id);
		return compra?.saldoPendiente ?? 0;
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
}
