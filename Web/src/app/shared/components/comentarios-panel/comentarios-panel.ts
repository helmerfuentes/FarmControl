import { Component, OnChanges, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ComentariosService } from '../../../core/api/comentarios.service';
import { Comentario, TipoEntidadComentario } from '../../../core/models';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
	selector: 'fc-comentarios-panel',
	imports: [DatePipe, FormsModule],
	templateUrl: './comentarios-panel.html',
	styleUrl: './comentarios-panel.scss',
})
export class ComentariosPanelComponent implements OnChanges {
	private readonly _svc = inject(ComentariosService);
	protected readonly personaId = inject(AuthService).personaId;

	readonly tipoEntidad = input.required<TipoEntidadComentario>();
	readonly entidadId = input.required<number>();

	protected readonly comentarios = signal<Comentario[]>([]);
	protected readonly loading = signal(false);
	protected readonly nuevoTexto = signal('');
	protected readonly enviando = signal(false);

	ngOnChanges(): void {
		if (this.entidadId()) {
			this.load();
		}
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getAll(this.tipoEntidad(), this.entidadId()).subscribe({
			next:  c => { this.comentarios.set(c); this.loading.set(false); },
			error: () => this.loading.set(false),
		});
	}

	protected enviar(): void {
		const texto = this.nuevoTexto().trim();
		if (!texto || this.enviando()) { return; }
		this.enviando.set(true);
		this._svc.create(this.tipoEntidad(), this.entidadId(), texto).subscribe({
			next:  () => { this.enviando.set(false); this.nuevoTexto.set(''); this.load(); },
			error: () => this.enviando.set(false),
		});
	}

	protected eliminar(id: number): void {
		this._svc.delete(id).subscribe(() => this.load());
	}

	protected esAutor(c: Comentario): boolean {
		return c.autorPersonaId === this.personaId();
	}
}
