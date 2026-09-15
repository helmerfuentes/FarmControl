import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FincasService } from '../../core/api/fincas.service';
import { TareasService } from '../../core/api/tareas.service';
import { AnalisisSueloService } from '../../core/api/analisis-suelo.service';
import { AuthService } from '../../core/auth/auth.service';
import { Finca, Parcela, TareaRecurrente, AnalisisSuelo } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { DecimalPipe, DatePipe } from '@angular/common';

@Component({
	selector: 'app-fincas',
	imports: [ReactiveFormsModule, DrawerComponent, ConfirmDialogComponent, DecimalPipe, DatePipe],
	templateUrl: './fincas.html',
	styleUrl: './fincas.scss',
})
export class FincasComponent implements OnInit {
	private readonly _svc   = inject(FincasService);
	private readonly _tareasSvc = inject(TareasService);
	private readonly _analisisSueloSvc = inject(AnalisisSueloService);
	private readonly _fb    = inject(FormBuilder);
	protected readonly isAdmin = inject(AuthService).isAdmin;

	protected readonly fincas   = signal<Finca[]>([]);
	protected readonly parcelas = signal<Parcela[]>([]);
	protected readonly loading  = signal(false);
	protected readonly saving   = signal(false);
	protected readonly panelOpen    = signal(false);
	protected readonly editId       = signal<number | null>(null);
	protected readonly panelMode    = signal<'finca' | 'parcela'>('finca');
	protected readonly selectedFinca    = signal<Finca | null>(null);
	protected readonly editParcelaId    = signal<number | null>(null);

	protected readonly parcelaTareasId = signal<number | null>(null);
	protected readonly tareas = signal<TareaRecurrente[]>([]);
	protected readonly loadingTareas = signal(false);
	protected readonly deleteTareaId = signal<number | null>(null);

	protected readonly parcelaAnalisisId = signal<number | null>(null);
	protected readonly analisisSuelo = signal<AnalisisSuelo[]>([]);
	protected readonly loadingAnalisis = signal(false);
	protected readonly deleteAnalisisId = signal<number | null>(null);

	protected readonly tareaForm = this._fb.group({
		descripcion:    ['', Validators.required],
		frecuenciaDias: [15, [Validators.required, Validators.min(1)]],
		proximaFecha:   ['', Validators.required],
	});

	protected readonly analisisForm = this._fb.group({
		fecha:           ['', Validators.required],
		ph:              [null as number | null],
		materiaOrganica: [null as number | null],
		nitrogeno:       [null as number | null],
		fosforo:         [null as number | null],
		potasio:         [null as number | null],
		observacion:     [''],
	});

	protected readonly fincaForm = this._fb.group({
		nombre:       ['', Validators.required],
		ubicacion:    ['', Validators.required],
		areaTotal:    [0, [Validators.required, Validators.min(0)]],
		costoTerreno: [0, [Validators.required, Validators.min(0)]],
	});

	protected readonly parcelaForm = this._fb.group({
		nombre: ['', Validators.required],
		area:   [0, [Validators.required, Validators.min(0.01)]],
	});

	ngOnInit(): void {
		this.load();
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getAll().subscribe({
			next:  fincas => { this.fincas.set(fincas); this.loading.set(false); },
			error: ()     => this.loading.set(false),
		});
	}

	protected openEditFinca(finca: Finca): void {
		if (!this.isAdmin()) { return; }
		this.editId.set(finca.id);
		this.fincaForm.setValue({
			nombre: finca.nombre, ubicacion: finca.ubicacion,
			areaTotal: finca.areaTotal, costoTerreno: finca.costoTerreno,
		});
		this.panelMode.set('finca');
		this.panelOpen.set(true);
	}

	protected selectFinca(finca: Finca): void {
		this.selectedFinca.set(finca);
		this.parcelaTareasId.set(null);
		this.tareas.set([]);
		this._svc.getParcelas(finca.id).subscribe(p => this.parcelas.set(p));
	}

	protected toggleTareasParcela(parcela: Parcela): void {
		if (this.parcelaTareasId() === parcela.id) {
			this.parcelaTareasId.set(null);
			return;
		}
		this.parcelaTareasId.set(parcela.id);
		this.loadTareas(parcela.id);
	}

	protected loadTareas(parcelaId: number): void {
		this.loadingTareas.set(true);
		this._tareasSvc.getAll(parcelaId).subscribe({
			next:  t => { this.tareas.set(t); this.loadingTareas.set(false); },
			error: () => this.loadingTareas.set(false),
		});
		this.tareaForm.reset({ descripcion: '', frecuenciaDias: 15, proximaFecha: new Date().toISOString().substring(0, 10) });
	}

	protected agregarTarea(): void {
		const parcelaId = this.parcelaTareasId();
		if (parcelaId === null || this.tareaForm.invalid || this.saving()) { return; }
		const raw = this.tareaForm.value;
		this.saving.set(true);
		this._tareasSvc.create({
			parcelaId,
			descripcion: raw.descripcion!,
			frecuenciaDias: Number(raw.frecuenciaDias),
			proximaFecha: raw.proximaFecha!,
		}).subscribe({
			next:  () => { this.saving.set(false); this.loadTareas(parcelaId); },
			error: () => this.saving.set(false),
		});
	}

	protected completarTarea(tareaId: number): void {
		const parcelaId = this.parcelaTareasId();
		if (parcelaId === null) { return; }
		this._tareasSvc.completar(tareaId).subscribe(() => this.loadTareas(parcelaId));
	}

	protected confirmDeleteTarea(id: number): void {
		this.deleteTareaId.set(id);
	}

	protected doDeleteTarea(): void {
		const id = this.deleteTareaId();
		const parcelaId = this.parcelaTareasId();
		if (id === null || parcelaId === null) { return; }
		this._tareasSvc.delete(id).subscribe(() => { this.deleteTareaId.set(null); this.loadTareas(parcelaId); });
	}

	protected cancelDeleteTarea(): void {
		this.deleteTareaId.set(null);
	}

	protected toggleAnalisisParcela(parcela: Parcela): void {
		if (this.parcelaAnalisisId() === parcela.id) {
			this.parcelaAnalisisId.set(null);
			return;
		}
		this.parcelaAnalisisId.set(parcela.id);
		this.loadAnalisisSuelo(parcela.id);
	}

	protected loadAnalisisSuelo(parcelaId: number): void {
		this.loadingAnalisis.set(true);
		this._analisisSueloSvc.getAll(parcelaId).subscribe({
			next:  a => { this.analisisSuelo.set(a); this.loadingAnalisis.set(false); },
			error: () => this.loadingAnalisis.set(false),
		});
		this.analisisForm.reset({
			fecha: new Date().toISOString().substring(0, 10),
			ph: null, materiaOrganica: null, nitrogeno: null, fosforo: null, potasio: null, observacion: '',
		});
	}

	protected agregarAnalisisSuelo(): void {
		const parcelaId = this.parcelaAnalisisId();
		if (parcelaId === null || this.analisisForm.invalid || this.saving()) { return; }
		const raw = this.analisisForm.value;
		this.saving.set(true);
		this._analisisSueloSvc.create({
			parcelaId,
			fecha: raw.fecha!,
			ph: raw.ph !== null && raw.ph !== undefined ? Number(raw.ph) : null,
			materiaOrganica: raw.materiaOrganica !== null && raw.materiaOrganica !== undefined ? Number(raw.materiaOrganica) : null,
			nitrogeno: raw.nitrogeno !== null && raw.nitrogeno !== undefined ? Number(raw.nitrogeno) : null,
			fosforo: raw.fosforo !== null && raw.fosforo !== undefined ? Number(raw.fosforo) : null,
			potasio: raw.potasio !== null && raw.potasio !== undefined ? Number(raw.potasio) : null,
			observacion: raw.observacion || null,
		}).subscribe({
			next:  () => { this.saving.set(false); this.loadAnalisisSuelo(parcelaId); },
			error: () => this.saving.set(false),
		});
	}

	protected confirmDeleteAnalisis(id: number): void {
		this.deleteAnalisisId.set(id);
	}

	protected doDeleteAnalisis(): void {
		const id = this.deleteAnalisisId();
		const parcelaId = this.parcelaAnalisisId();
		if (id === null || parcelaId === null) { return; }
		this._analisisSueloSvc.delete(id).subscribe(() => { this.deleteAnalisisId.set(null); this.loadAnalisisSuelo(parcelaId); });
	}

	protected cancelDeleteAnalisis(): void {
		this.deleteAnalisisId.set(null);
	}

	protected openNewParcela(): void {
		if (!this.selectedFinca()) { return; }
		this.editParcelaId.set(null);
		this.parcelaForm.reset({ area: 0 });
		this.panelMode.set('parcela');
		this.panelOpen.set(true);
	}

	protected openEditParcela(p: Parcela): void {
		if (!this.isAdmin()) { return; }
		this.editParcelaId.set(p.id);
		this.parcelaForm.setValue({ nombre: p.nombre, area: p.area });
		this.panelMode.set('parcela');
		this.panelOpen.set(true);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected saveFinca(): void {
		const id = this.editId();
		if (this.fincaForm.invalid || this.saving() || id === null) { return; }
		const raw = this.fincaForm.value;
		const value = {
			nombre: raw.nombre!, ubicacion: raw.ubicacion!,
			areaTotal: Number(raw.areaTotal), costoTerreno: Number(raw.costoTerreno),
		};
		this.saving.set(true);
		this._svc.update(id, value).subscribe({
			next:  () => { this.saving.set(false); this.closePanel(); this.load(); },
			error: () => this.saving.set(false),
		});
	}

	protected saveParcela(): void {
		if (this.parcelaForm.invalid || this.saving()) { return; }
		const finca = this.selectedFinca();
		if (!finca) { return; }
		const raw = this.parcelaForm.value;
		const value = {
			fincaId: finca.id, nombre: raw.nombre!, area: Number(raw.area),
		};
		this.saving.set(true);
		const id = this.editParcelaId();
		const req = id ? this._svc.updateParcela(id, value) : this._svc.createParcela(value);
		req.subscribe({
			next:  () => {
				this.saving.set(false); this.closePanel();
				this._svc.getParcelas(finca.id).subscribe(p => this.parcelas.set(p));
			},
			error: () => this.saving.set(false),
		});
	}

	protected drawerTitle(): string {
		if (this.panelMode() === 'parcela') {
			return this.editParcelaId() ? 'Editar parcela' : 'Nueva parcela';
		}
		return 'Editar finca';
	}
}
