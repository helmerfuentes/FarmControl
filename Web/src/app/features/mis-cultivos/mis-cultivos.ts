import { Component, inject, signal, OnInit } from '@angular/core';
import { SocioService } from '../../core/api/socio.service';
import { ProcesoCultivo, EstadoProceso } from '../../core/models';
import { DatePipe } from '@angular/common';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

@Component({
	selector: 'app-mis-cultivos',
	imports: [DatePipe, TableSkeletonComponent],
	templateUrl: './mis-cultivos.html',
	styleUrl: './mis-cultivos.scss',
})
export class MisCultivosComponent implements OnInit {
	private readonly _svc = inject(SocioService);

	protected readonly procesos = signal<ProcesoCultivo[]>([]);
	protected readonly loading  = signal(false);

	ngOnInit(): void {
		this.loading.set(true);
		this._svc.getMisProcesos().subscribe({
			next:  data => { this.procesos.set(data); this.loading.set(false); },
			error: ()   => this.loading.set(false),
		});
	}

	protected estadoBadgeClass(estado: EstadoProceso): string {
		const map: Record<EstadoProceso, string> = {
			Activo:    'badge badge-activo',
			Cosechado: 'badge badge-cosechado',
			Cerrado:   'badge badge-cerrado',
		};
		return map[estado];
	}
}
