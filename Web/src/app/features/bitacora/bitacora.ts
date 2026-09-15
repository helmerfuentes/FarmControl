import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BitacoraService, BitacoraEntry } from '../../core/api/bitacora.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

@Component({
	selector: 'app-bitacora',
	imports: [DatePipe, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './bitacora.html',
	styleUrl: './bitacora.scss',
})
export class BitacoraComponent implements OnInit {
	private readonly _svc = inject(BitacoraService);

	protected readonly entradas = signal<BitacoraEntry[]>([]);
	protected readonly loading = signal(false);

	ngOnInit(): void {
		this.loading.set(true);
		this._svc.getAll().subscribe({
			next:  e => { this.entradas.set(e); this.loading.set(false); },
			error: () => this.loading.set(false),
		});
	}

	protected exportarCsv(): void {
		const filas: string[] = ['Fecha,Quién,Acción,Detalle'];
		for (const e of this.entradas()) {
			filas.push([
				this._csvValor(e.fechaHora),
				this._csvValor(e.actorNombre),
				this._csvValor(e.accion),
				this._csvValor(e.detalle),
			].join(','));
		}
		this._descargarCsv(filas, 'bitacora');
	}

	private _csvValor(valor: string): string {
		return `"${(valor ?? '').replace(/"/g, '""')}"`;
	}

	private _descargarCsv(filas: string[], nombreArchivo: string): void {
		const contenido = '﻿' + filas.join('\n');
		const blob = new Blob([contenido], { type: 'text/csv;charset=utf-8;' });
		const url = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `${nombreArchivo}.csv`;
		link.click();
		URL.revokeObjectURL(url);
	}
}
