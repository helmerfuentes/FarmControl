import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ReportesService, ResumenFinca } from '../../core/api/reportes.service';

@Component({
	selector: 'app-reporte-publico',
	imports: [CurrencyPipe, DecimalPipe],
	templateUrl: './reporte-publico.html',
	styleUrl: './reporte-publico.scss',
})
export class ReportePublicoComponent implements OnInit {
	private readonly _route = inject(ActivatedRoute);
	private readonly _reportesService = inject(ReportesService);

	protected readonly resumen = signal<ResumenFinca | null>(null);
	protected readonly loading = signal(true);
	protected readonly error   = signal<string | null>(null);

	ngOnInit(): void {
		const token = this._route.snapshot.paramMap.get('token');
		if (!token) {
			this.error.set('Este reporte no está disponible o ha expirado.');
			this.loading.set(false);
			return;
		}
		this._reportesService.getReporteCompartido(token).subscribe({
			next:  data => { this.resumen.set(data); this.loading.set(false); },
			error: ()   => {
				this.error.set('Este reporte no está disponible o ha expirado.');
				this.loading.set(false);
			},
		});
	}
}
