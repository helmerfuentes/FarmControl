import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ActividadesService } from '../../core/api/actividades.service';
import { FincasService } from '../../core/api/fincas.service';
import { PersonasService } from '../../core/api/personas.service';
import { AuthService } from '../../core/auth/auth.service';
import { FincaContextService } from '../../core/finca/finca-context.service';
import { Actividad, Parcela, Persona, Finca } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { TableControlsComponent } from '../../shared/components/table-controls/table-controls';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';
import { ComentariosPanelComponent } from '../../shared/components/comentarios-panel/comentarios-panel';

interface ParcelaConFinca extends Parcela {
	fincaNombre: string;
}

const HORAS_JORNADA = 8;

@Component({
	selector: 'app-actividades',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, DatePipe, CurrencyPipe, DecimalPipe, TableControlsComponent, EmptyStateComponent, TableSkeletonComponent, ComentariosPanelComponent],
	templateUrl: './actividades.html',
	styleUrl: './actividades.scss',
})
export class ActividadesComponent implements OnInit {
	private readonly _svc        = inject(ActividadesService);
	private readonly _fincaSvc   = inject(FincasService);
	private readonly _personaSvc = inject(PersonasService);
	private readonly _fb         = inject(FormBuilder);
	protected readonly isAdmin   = inject(AuthService).isAdmin;
	protected readonly fincaContext = inject(FincaContextService);

	protected readonly items    = signal<Actividad[]>([]);
	protected readonly parcelas = signal<ParcelaConFinca[]>([]);
	protected readonly personas = signal<Persona[]>([]);
	protected readonly loading  = signal(false);
	protected readonly saving   = signal(false);
	protected readonly panelOpen = signal(false);
	protected readonly editId   = signal<number | null>(null);
	protected readonly deleteId = signal<number | null>(null);

	protected readonly personaRef = signal<Persona | null>(null);
	protected readonly modoDia    = signal(true);

	protected readonly horas = Array.from({ length: 24 }, (_, i) => i);

	protected readonly actForm = this._fb.group({
		parcelaId:           [0, [Validators.required, Validators.min(1)]],
		tipoActividad:       ['', Validators.required],
		fecha:               ['', Validators.required],
		inicioFecha:         [''],
		inicioHora:          [7],
		finFecha:            [''],
		finHora:             [15],
		personaACargoId:     [null as number | null],
		valorDiaActividad:   [0, [Validators.required, Validators.min(0)]],
		descripcion:         [''],
	});

	ngOnInit(): void {
		this.load();
		this._personaSvc.getAll().subscribe(p => this.personas.set(p));
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

	protected setModo(dia: boolean): void {
		this.modoDia.set(dia);
		this.actForm.patchValue({ fecha: '', inicioFecha: '', finFecha: '' });
		const fechaCtrl  = this.actForm.get('fecha')!;
		const inicioCtrl = this.actForm.get('inicioFecha')!;
		if (dia) {
			fechaCtrl.setValidators(Validators.required);
			inicioCtrl.clearValidators();
		} else {
			fechaCtrl.clearValidators();
			inicioCtrl.setValidators(Validators.required);
		}
		fechaCtrl.updateValueAndValidity();
		inicioCtrl.updateValueAndValidity();
	}

	protected onPersonaChange(value: string): void {
		const id = Number(value);
		const persona = id ? (this.personas().find(p => p.id === id) ?? null) : null;
		this.personaRef.set(persona);
		this.actForm.patchValue({ valorDiaActividad: persona?.valorDia ?? 0 });
	}

	protected valorHora(): number {
		const vd = Number(this.actForm.get('valorDiaActividad')?.value ?? 0);
		return Math.round(vd / HORAS_JORNADA);
	}

	protected openNew(): void {
		this.editId.set(null);
		this.personaRef.set(null);
		this.setModo(true);
		const today = new Date().toISOString().substring(0, 10);
		this.actForm.patchValue({ fecha: today, inicioHora: 7, finHora: 15, valorDiaActividad: 0 });
		this.panelOpen.set(true);
	}

	protected openEdit(item: Actividad): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(item.id);
		const persona = item.personaACargoId
			? (this.personas().find(p => p.id === item.personaACargoId) ?? null)
			: null;
		this.personaRef.set(persona);
		const inicio = item.fechaInicio.substring(0, 16);
		const fin    = item.fechaFin ? item.fechaFin.substring(0, 16) : '';
		const esDia  = inicio.endsWith('T00:00') && (!fin || fin.endsWith('T08:00'));
		this.setModo(esDia);
		const inicioH = esDia ? 0 : Number(inicio.substring(11, 13));
		const finH    = fin ? Number(fin.substring(11, 13)) : 15;
		this.actForm.setValue({
			parcelaId:         item.parcelaId,
			tipoActividad:     item.tipoActividad,
			fecha:             esDia ? inicio.substring(0, 10) : '',
			inicioFecha:       esDia ? '' : inicio.substring(0, 10),
			inicioHora:        inicioH,
			finFecha:          esDia ? '' : (fin ? fin.substring(0, 10) : ''),
			finHora:           finH,
			personaACargoId:   item.personaACargoId ?? null,
			valorDiaActividad: item.valorDiaUsado ?? 0,
			descripcion:       item.descripcion ?? '',
		});
		this.panelOpen.set(true);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected saveActividad(): void {
		if (this.actForm.invalid || this.saving()) { return; }
		const raw = this.actForm.value;
		let fechaInicio: string;
		let fechaFin: string | null;
		if (this.modoDia()) {
			fechaInicio = `${raw.fecha}T00:00`;
			fechaFin    = `${raw.fecha}T08:00`;
		} else {
			const ih = String(raw.inicioHora ?? 0).padStart(2, '0');
			const fh = String(raw.finHora ?? 0).padStart(2, '0');
			fechaInicio = `${raw.inicioFecha}T${ih}:00`;
			fechaFin    = raw.finFecha ? `${raw.finFecha}T${fh}:00` : null;
		}
		const value = {
			parcelaId:         Number(raw.parcelaId),
			tipoActividad:     raw.tipoActividad!,
			fechaInicio,
			fechaFin,
			personaACargoId:   raw.personaACargoId ? Number(raw.personaACargoId) : null,
			valorDiaActividad: Number(raw.valorDiaActividad ?? 0),
			descripcion:       raw.descripcion ?? '',
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
				this.deleteErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar la actividad.');
			},
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}

	protected confirmar(item: Actividad): void {
		const nombre = window.prompt('Nombre de quien confirma esta actividad (jornalero/socio a cargo):', item.personaACargoNombre ?? '');
		if (!nombre || !nombre.trim()) { return; }
		this._svc.confirmar(item.id, nombre.trim()).subscribe({
			next:  () => this.load(),
			error: () => {},
		});
	}

	protected drawerTitle(): string {
		return this.editId() ? 'Editar actividad' : 'Nueva actividad';
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
}
