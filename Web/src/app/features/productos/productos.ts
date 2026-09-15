import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormArray, Validators } from '@angular/forms';
import { ProductosService } from '../../core/api/productos.service';
import { AuthService } from '../../core/auth/auth.service';
import { Producto } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

const UNIDADES_MEDIDA = ['Bulto', 'Kilo', 'Canastilla', 'Caja'] as const;

@Component({
	selector: 'app-productos',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './productos.html',
	styleUrl: './productos.scss',
})
export class ProductosComponent implements OnInit {
	private readonly _svc  = inject(ProductosService);
	private readonly _fb   = inject(FormBuilder);
	protected readonly isAdmin = inject(AuthService).isAdmin;

	protected readonly unidades = UNIDADES_MEDIDA;

	protected readonly items     = signal<Producto[]>([]);
	protected readonly loading   = signal(false);
	protected readonly saving    = signal(false);
	protected readonly panelOpen = signal(false);
	protected readonly editId    = signal<number | null>(null);
	protected readonly deleteId  = signal<number | null>(null);
	protected readonly errorMsg  = signal('');

	protected readonly form = this._fb.group({
		nombre: ['', Validators.required],
		clasificaciones: this._fb.array([]),
	});

	get clasificacionesArray(): FormArray {
		return this.form.get('clasificaciones') as FormArray;
	}

	ngOnInit(): void {
		this.load();
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
		this.errorMsg.set('');
		this.clasificacionesArray.clear();
		this.form.reset({ nombre: '' });
		this.addClasificacion();
		this.panelOpen.set(true);
	}

	protected openEdit(item: Producto): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(item.id);
		this.errorMsg.set('');
		this.form.patchValue({ nombre: item.nombre });
		this.clasificacionesArray.clear();
		(item.clasificaciones ?? []).forEach(c => {
			this.clasificacionesArray.push(this._fb.group({
				nombre:       [c.nombre,       Validators.required],
				unidadMedida: [c.unidadMedida, Validators.required],
				pesoUnidadKg: [c.pesoUnidadKg, [Validators.required, Validators.min(0.01)]],
			}));
		});
		this.panelOpen.set(true);
	}

	protected addClasificacion(): void {
		this.clasificacionesArray.push(this._fb.group({
			nombre:       ['', Validators.required],
			unidadMedida: ['Bulto', Validators.required],
			pesoUnidadKg: [0, [Validators.required, Validators.min(0.01)]],
		}));
	}

	protected removeClasificacion(i: number): void {
		this.clasificacionesArray.removeAt(i);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected save(): void {
		if (this.form.invalid || this.saving()) { return; }
		const raw = this.form.value;
		const value = {
			nombre: raw.nombre!,
			clasificaciones: (raw.clasificaciones as Array<{ nombre: string; unidadMedida: string; pesoUnidadKg: number }>)
				.map(c => ({
					nombre:       c.nombre,
					unidadMedida: c.unidadMedida,
					pesoUnidadKg: Number(c.pesoUnidadKg),
				})),
		};
		this.saving.set(true);
		const id = this.editId();
		const req = id ? this._svc.update(id, value as never) : this._svc.create(value as never);
		req.subscribe({
			next:  () => { this.saving.set(false); this.closePanel(); this.load(); },
			error: () => { this.saving.set(false); this.errorMsg.set('No se pudo guardar el producto. Verifica los datos e intenta de nuevo.'); },
		});
	}

	protected confirmDelete(id: number): void {
		this.deleteId.set(id);
	}

	protected doDelete(): void {
		const id = this.deleteId();
		if (id === null) { return; }
		this._svc.delete(id).subscribe({
			next:  () => { this.deleteId.set(null); this.load(); },
			error: err => { this.deleteId.set(null); this.errorMsg.set(err?.error?.error ?? 'No se pudo eliminar el producto.'); },
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected drawerTitle(): string {
		return this.editId() ? 'Editar producto' : 'Nuevo producto';
	}

	protected readonly search   = signal('');
	protected readonly page     = signal(1);
	protected readonly pageSize = signal(10);

	protected readonly filtered = computed(() => {
		const term = this.search().toLowerCase().trim();
		if (!term) { return this.items(); }
		return this.items().filter(item => JSON.stringify(item).toLowerCase().includes(term));
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
