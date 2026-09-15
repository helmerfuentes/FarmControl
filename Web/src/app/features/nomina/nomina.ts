import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DatePipe, CurrencyPipe, DecimalPipe } from '@angular/common';
import { NominaService } from '../../core/api/nomina.service';
import { PersonasService } from '../../core/api/personas.service';
import { LiquidacionNomina, PendientesLiquidacion, Persona } from '../../core/models';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

@Component({
	selector: 'app-nomina',
	imports: [ReactiveFormsModule, DatePipe, CurrencyPipe, DecimalPipe, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './nomina.html',
	styleUrl: './nomina.scss',
})
export class NominaComponent implements OnInit {
	private readonly _svc = inject(NominaService);
	private readonly _personaSvc = inject(PersonasService);
	private readonly _fb = inject(FormBuilder);

	protected readonly jornaleros = signal<Persona[]>([]);
	protected readonly pendientes = signal<PendientesLiquidacion | null>(null);
	protected readonly liquidaciones = signal<LiquidacionNomina[]>([]);
	protected readonly loadingPendientes = signal(false);
	protected readonly loadingLiquidaciones = signal(false);
	protected readonly liquidando = signal(false);
	protected readonly errorMsg = signal<string | null>(null);

	protected readonly filtroForm = this._fb.group({
		jornaleroId: [0, [Validators.required, Validators.min(1)]],
		fechaInicio: ['', Validators.required],
		fechaFin: ['', Validators.required],
		observacion: [''],
	});

	ngOnInit(): void {
		this._personaSvc.getAll('Jornalero').subscribe(p => this.jornaleros.set(p));
		this.loadLiquidaciones();
	}

	protected loadLiquidaciones(): void {
		this.loadingLiquidaciones.set(true);
		const jornaleroId = Number(this.filtroForm.value.jornaleroId) || undefined;
		this._svc.getLiquidaciones(jornaleroId).subscribe({
			next: l => { this.liquidaciones.set(l); this.loadingLiquidaciones.set(false); },
			error: () => this.loadingLiquidaciones.set(false),
		});
	}

	protected verPendientes(): void {
		const raw = this.filtroForm.value;
		if (!raw.jornaleroId || !raw.fechaInicio || !raw.fechaFin) { return; }
		this.errorMsg.set(null);
		this.loadingPendientes.set(true);
		this._svc.getPendientes(Number(raw.jornaleroId), raw.fechaInicio, raw.fechaFin).subscribe({
			next: p => { this.pendientes.set(p); this.loadingPendientes.set(false); },
			error: () => { this.loadingPendientes.set(false); this.pendientes.set(null); },
		});
	}

	protected liquidar(): void {
		const raw = this.filtroForm.value;
		if (!raw.jornaleroId || !raw.fechaInicio || !raw.fechaFin || this.liquidando()) { return; }
		this.liquidando.set(true);
		this.errorMsg.set(null);
		this._svc.crearLiquidacion({
			jornaleroId: Number(raw.jornaleroId),
			fechaInicio: raw.fechaInicio,
			fechaFin: raw.fechaFin,
			observacion: raw.observacion || null,
		}).subscribe({
			next: () => {
				this.liquidando.set(false);
				this.pendientes.set(null);
				this.loadLiquidaciones();
			},
			error: err => {
				this.liquidando.set(false);
				this.errorMsg.set(err?.error?.error ?? 'No se pudo crear la liquidación.');
			},
		});
	}
}
