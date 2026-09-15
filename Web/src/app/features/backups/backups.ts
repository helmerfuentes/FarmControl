import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BackupsService, BackupInfo } from '../../core/api/backups.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';

const _BYTES_POR_KB = 1024;
const _BYTES_POR_MB = 1024 * 1024;

@Component({
	selector: 'app-backups',
	imports: [DatePipe, EmptyStateComponent, TableSkeletonComponent],
	templateUrl: './backups.html',
	styleUrl: './backups.scss',
})
export class BackupsComponent implements OnInit {
	private readonly _svc = inject(BackupsService);

	protected readonly backups = signal<BackupInfo[]>([]);
	protected readonly loading = signal(false);
	protected readonly generando = signal(false);
	protected readonly descargando = signal<string | null>(null);
	protected readonly errorMsg = signal<string | null>(null);

	ngOnInit(): void {
		this.load();
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getAll().subscribe({
			next: b => { this.backups.set(b); this.loading.set(false); },
			error: () => this.loading.set(false),
		});
	}

	protected generar(): void {
		this.generando.set(true);
		this.errorMsg.set(null);
		this._svc.crear().subscribe({
			next: () => { this.generando.set(false); this.load(); },
			error: err => {
				this.generando.set(false);
				this.errorMsg.set(err?.error?.error ?? 'No se pudo generar el backup.');
			},
		});
	}

	protected descargar(backup: BackupInfo): void {
		this.descargando.set(backup.nombreArchivo);
		this._svc.descargar(backup.nombreArchivo).subscribe({
			next: blob => {
				this.descargando.set(null);
				const url = URL.createObjectURL(blob);
				const link = document.createElement('a');
				link.href = url;
				link.download = backup.nombreArchivo;
				link.click();
				URL.revokeObjectURL(url);
			},
			error: () => {
				this.descargando.set(null);
				this.errorMsg.set('No se pudo descargar el backup.');
			},
		});
	}

	protected formatearTamano(bytes: number): string {
		if (bytes >= _BYTES_POR_MB) { return `${(bytes / _BYTES_POR_MB).toFixed(2)} MB`; }
		if (bytes >= _BYTES_POR_KB) { return `${(bytes / _BYTES_POR_KB).toFixed(1)} KB`; }
		return `${bytes} B`;
	}
}
