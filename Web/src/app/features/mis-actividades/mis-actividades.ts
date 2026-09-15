import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActividadesService } from '../../core/api/actividades.service';
import { AuthService } from '../../core/auth/auth.service';
import { Actividad } from '../../core/models';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton';
import { ComentariosPanelComponent } from '../../shared/components/comentarios-panel/comentarios-panel';

function hoyISO(): string {
	return new Date().toISOString().substring(0, 10);
}

@Component({
	selector: 'app-mis-actividades',
	imports: [DatePipe, FormsModule, EmptyStateComponent, TableSkeletonComponent, ComentariosPanelComponent],
	templateUrl: './mis-actividades.html',
	styleUrl: './mis-actividades.scss',
})
export class MisActividadesComponent implements OnInit {
	private readonly _svc = inject(ActividadesService);
	protected readonly usuario = inject(AuthService).usuario;

	protected readonly fecha = signal(hoyISO());
	protected readonly items = signal<Actividad[]>([]);
	protected readonly loading = signal(false);
	protected readonly confirmandoId = signal<number | null>(null);
	protected readonly expandidoId = signal<number | null>(null);

	ngOnInit(): void {
		this.load();
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getMisActividades(this.fecha()).subscribe({
			next:  items => { this.items.set(items); this.loading.set(false); },
			error: ()    => this.loading.set(false),
		});
	}

	protected onFechaChange(value: string): void {
		this.fecha.set(value);
		this.load();
	}

	protected toggleExpandido(id: number): void {
		this.expandidoId.update(actual => actual === id ? null : id);
	}

	protected marcarRealizada(item: Actividad): void {
		if (this.confirmandoId() !== null) { return; }
		this.confirmandoId.set(item.id);
		this._svc.confirmar(item.id, this.usuario() ?? '').subscribe({
			next:  () => { this.confirmandoId.set(null); this.load(); },
			error: () => this.confirmandoId.set(null),
		});
	}
}
