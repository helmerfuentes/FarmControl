import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TiposInsumoService } from '../../core/api/tipos-insumo.service';
import { AuthService } from '../../core/auth/auth.service';
import { TipoInsumo } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

@Component({
	selector: 'app-tipos-insumo',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './tipos-insumo.html',
	styleUrl: './tipos-insumo.scss',
})
export class TiposInsumoComponent implements OnInit {
	private readonly _svc  = inject(TiposInsumoService);
	private readonly _fb   = inject(FormBuilder);
	protected readonly isAdmin = inject(AuthService).isAdmin;

	protected readonly items     = signal<TipoInsumo[]>([]);
	protected readonly loading   = signal(false);
	protected readonly saving    = signal(false);
	protected readonly panelOpen = signal(false);
	protected readonly editId    = signal<number | null>(null);
	protected readonly deleteId  = signal<number | null>(null);

	protected readonly form = this._fb.group({
		nombre:      ['', Validators.required],
		descripcion: ['', Validators.required],
	});

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
		this.form.reset();
		this.panelOpen.set(true);
	}

	protected openEdit(item: TipoInsumo): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(item.id);
		this.form.setValue({ nombre: item.nombre, descripcion: item.descripcion });
		this.panelOpen.set(true);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected save(): void {
		if (this.form.invalid || this.saving()) { return; }
		const value = this.form.value as { nombre: string; descripcion: string };
		this.saving.set(true);
		const id = this.editId();
		const req = id
			? this._svc.update(id, value)
			: this._svc.create(value);
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
				this.deleteErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar el tipo de insumo.');
			},
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected drawerTitle(): string {
		return this.editId() ? 'Editar tipo de insumo' : 'Nuevo tipo de insumo';
	}

	private static readonly _ICONOS: Array<{ claves: string[]; icono: string }> = [
		{ claves: ['fertiliz'], icono: 'M12 2s7 8.5 7 13a7 7 0 01-14 0c0-4.5 7-13 7-13z' },
		{ claves: ['fungicid', 'hongo'], icono: 'M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4z' },
		{ claves: ['herbicid', 'maleza'], icono: 'M12 2a10 10 0 100 20 10 10 0 000-20zM4.9 4.9l14.2 14.2' },
		{ claves: ['insecticid', 'plaga'], icono: 'M12 20v-9M9 7.13v-1a3 3 0 116 0v1M12 20c-3.3 0-6-2.7-6-6v-3a6 6 0 0112 0v3c0 3.3-2.7 6-6 6zM6.53 9H4M20 9h-2.53M6 13H2M22 13h-4M6.53 17H4M20 17h-2.53' },
		{ claves: ['semilla', 'material vegetal', 'plantul'], icono: 'M7 8c0 3 2 5 5 5s5-2 5-5c-2 0-3 1-5 1S9 8 7 8zM12 13v9' },
	];

	private static readonly _ICONO_DEFECTO = 'M20.59 13.41L11 3.83A2 2 0 009.5 3H4a1 1 0 00-1 1v5.5a2 2 0 00.59 1.41l9.58 9.58a2 2 0 002.83 0l6.59-6.59a2 2 0 000-2.83zM7 7h.01';

	protected iconoPara(nombre: string): string {
		const clave = nombre.toLowerCase();
		const match = TiposInsumoComponent._ICONOS.find(entry => entry.claves.some(c => clave.includes(c)));
		return match ? match.icono : TiposInsumoComponent._ICONO_DEFECTO;
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
