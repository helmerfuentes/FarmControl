import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { PersonasService } from '../../core/api/personas.service';
import { AuthService } from '../../core/auth/auth.service';
import { Persona, TipoPersona } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

const ROLES_CON_ACCESO: TipoPersona[] = ['Socio'];

@Component({
	selector: 'app-personas',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './personas.html',
	styleUrl: './personas.scss',
})
export class PersonasComponent implements OnInit {
	private readonly _svc  = inject(PersonasService);
	private readonly _fb   = inject(FormBuilder);
	protected readonly isAdmin = inject(AuthService).isAdmin;

	protected readonly items      = signal<Persona[]>([]);
	protected readonly loading    = signal(false);
	protected readonly saving     = signal(false);
	protected readonly panelOpen  = signal(false);
	protected readonly editId     = signal<number | null>(null);
	protected readonly deleteId   = signal<number | null>(null);
	protected readonly filtroTipo = signal<TipoPersona | ''>('');

	protected readonly tiposPersona: { value: TipoPersona; label: string }[] = [
		{ value: 'Socio',     label: 'Socio' },
		{ value: 'Jornalero', label: 'Jornalero' },
		{ value: 'Comprador', label: 'Comprador' },
		{ value: 'Otro',      label: 'Otro' },
	];

	protected readonly form = this._fb.group({
		nombre:        ['', Validators.required],
		documento:     [''],
		telefono:      [''],
		email:         ['', Validators.email],
		tipoPersona:   ['Socio' as TipoPersona, Validators.required],
		valorDia:      [0, [Validators.required, Validators.min(0)]],
		nombreUsuario: [''],
		password:      [''],
	});

	protected readonly tieneAcceso = computed(() => {
		const tipo = this.form.get('tipoPersona')?.value as TipoPersona;
		return ROLES_CON_ACCESO.includes(tipo);
	});

	ngOnInit(): void {
		this.load();
	}

	protected load(): void {
		this.loading.set(true);
		const tipo = this.filtroTipo() || undefined;
		this._svc.getAll(tipo).subscribe({
			next:  items => { this.items.set(items); this.loading.set(false); },
			error: ()    => this.loading.set(false),
		});
	}

	protected setFiltro(tipo: TipoPersona | ''): void {
		this.filtroTipo.set(tipo);
		this.load();
	}

	protected openNew(): void {
		this.editId.set(null);
		this.form.reset({ tipoPersona: 'Socio' });
		this.panelOpen.set(true);
	}

	protected openEdit(item: Persona): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(item.id);
		this.form.setValue({
			nombre:        item.nombre,
			documento:     item.documento,
			telefono:      item.telefono,
			email:         item.email ?? '',
			tipoPersona:   item.tipoPersona,
			valorDia:      item.valorDia ?? 0,
			nombreUsuario: '',
			password:      '',
		});
		this.panelOpen.set(true);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected save(): void {
		if (this.form.invalid || this.saving()) { return; }
		const raw = this.form.value;
		const value = {
			nombre:        raw.nombre!,
			documento:     raw.documento ?? '',
			telefono:      raw.telefono ?? '',
			email:         raw.email ?? '',
			tipoPersona:   raw.tipoPersona as TipoPersona,
			valorDia:      Number(raw.valorDia ?? 0),
			nombreUsuario: raw.nombreUsuario || null,
			password:      raw.password || null,
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
				this.deleteErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar la persona.');
			},
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected tipoBadgeClass(tipo: TipoPersona): string {
		const map: Record<TipoPersona, string> = {
			Socio:     'badge-socio',
			Jornalero: 'badge-jornalero',
			Comprador: 'badge-comprador',
			Otro:      'badge-otro',
			Admin:     'badge-admin',
			Contador:  'badge-admin',
		};
		return `badge ${map[tipo]}`;
	}

	protected drawerTitle(): string {
		return this.editId() ? 'Editar persona' : 'Nueva persona';
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
