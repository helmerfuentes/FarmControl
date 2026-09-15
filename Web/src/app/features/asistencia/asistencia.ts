import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AsistenciasService } from '../../core/api/asistencias.service';
import { PersonasService } from '../../core/api/personas.service';
import { FincasService } from '../../core/api/fincas.service';
import { Asistencia, Persona, Finca } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

@Component({
	selector: 'app-asistencia',
	imports: [ReactiveFormsModule, DatePipe, DrawerComponent, ConfirmDialogComponent, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './asistencia.html',
	styleUrl: './asistencia.scss',
})
export class AsistenciaComponent implements OnInit {
	private readonly _svc = inject(AsistenciasService);
	private readonly _personaSvc = inject(PersonasService);
	private readonly _fincaSvc = inject(FincasService);
	private readonly _fb = inject(FormBuilder);

	protected readonly items = signal<Asistencia[]>([]);
	protected readonly personas = signal<Persona[]>([]);
	protected readonly fincas = signal<Finca[]>([]);
	protected readonly loading = signal(false);
	protected readonly saving = signal(false);
	protected readonly panelOpen = signal(false);
	protected readonly deleteId = signal<number | null>(null);

	protected readonly filtroFecha = signal<string>('');

	protected readonly form = this._fb.group({
		personaId: [0, [Validators.required, Validators.min(1)]],
		fincaId: [0, [Validators.required, Validators.min(1)]],
		fecha: ['', Validators.required],
		horaEntrada: [''],
		horaSalida: [''],
		observacion: [''],
	});

	ngOnInit(): void {
		this.load();
		this._personaSvc.getAll('Jornalero').subscribe(p => this.personas.set(p));
		this._fincaSvc.getAll().subscribe(f => this.fincas.set(f));
	}

	protected load(): void {
		this.loading.set(true);
		const fecha = this.filtroFecha();
		this._svc.getAll(fecha ? { desde: fecha, hasta: fecha } : {}).subscribe({
			next: items => { this.items.set(items); this.loading.set(false); },
			error: () => this.loading.set(false),
		});
	}

	protected onFiltroFechaChange(value: string): void {
		this.filtroFecha.set(value);
		this.load();
	}

	protected openNew(): void {
		const today = new Date().toISOString().substring(0, 10);
		this.form.reset({ personaId: 0, fincaId: 0, fecha: today, horaEntrada: '', horaSalida: '', observacion: '' });
		this.panelOpen.set(true);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected save(): void {
		if (this.form.invalid || this.saving()) { return; }
		const raw = this.form.value;
		this.saving.set(true);
		this._svc.create({
			personaId: Number(raw.personaId),
			fincaId: Number(raw.fincaId),
			fecha: raw.fecha!,
			horaEntrada: raw.horaEntrada || null,
			horaSalida: raw.horaSalida || null,
			observacion: raw.observacion || null,
		}).subscribe({
			next: () => { this.saving.set(false); this.closePanel(); this.load(); },
			error: () => this.saving.set(false),
		});
	}

	protected registrarSalida(item: Asistencia): void {
		const hora = window.prompt('Hora de salida (HH:mm):', item.horaSalida ?? '');
		if (!hora) { return; }
		this._svc.update(item.id, { horaEntrada: item.horaEntrada, horaSalida: hora, observacion: item.observacion }).subscribe({
			next: () => this.load(),
			error: () => {},
		});
	}

	protected confirmDelete(id: number): void {
		this.deleteId.set(id);
	}

	protected doDelete(): void {
		const id = this.deleteId();
		if (id === null) { return; }
		this._svc.delete(id).subscribe({
			next: () => { this.deleteId.set(null); this.load(); },
			error: () => this.deleteId.set(null),
		});
	}

	protected cancelDelete(): void {
		this.deleteId.set(null);
	}
}
