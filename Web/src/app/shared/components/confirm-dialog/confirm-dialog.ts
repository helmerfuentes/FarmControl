import { Component, input, output, HostListener } from '@angular/core';

@Component({
	selector: 'fc-confirm-dialog',
	template: `
		@if (open()) {
			<div class="backdrop" (click)="cancelled.emit()">
				<div class="dialog" (click)="$event.stopPropagation()">
					<p class="message">{{ message() }}</p>
					<div class="actions">
						<button class="btn btn-ghost" (click)="cancelled.emit()">Cancelar</button>
						<button class="btn btn-danger" (click)="confirmed.emit()">Eliminar</button>
					</div>
				</div>
			</div>
		}
	`,
	styles: [`
		.backdrop {
			position: fixed;
			inset: 0;
			background: rgba(0,0,0,.55);
			z-index: 200;
			display: grid;
			place-items: center;
		}
		.dialog {
			background: var(--surface-raised);
			border: 1px solid var(--border);
			border-radius: var(--radius-md);
				box-shadow: var(--shadow-lg);
			padding: 1.5rem;
			width: 320px;
			max-width: 90vw;
		}
		.message {
			color: var(--text);
			margin: 0 0 1.5rem;
			font-size: var(--text-sm);
			line-height: 1.6;
		}
		.actions {
			display: flex;
			gap: .75rem;
			justify-content: flex-end;
		}
		.btn {
			padding: .5rem 1rem;
			border-radius: var(--radius-sm);
			font-size: var(--text-sm);
			font-weight: 500;
			cursor: pointer;
			border: none;
			transition: background var(--transition-fast);
		}
		.btn-ghost {
			background: transparent;
			color: var(--text-muted);
			border: 1px solid var(--border);
		}
		.btn-ghost:hover { background: var(--surface); }
		.btn-danger {
			background: #7a2020;
			color: #f8d7d7;
		}
		.btn-danger:hover { background: #9a2a2a; }
	`],
})
export class ConfirmDialogComponent {
	readonly open = input.required<boolean>();
	readonly message = input('¿Estás seguro de que deseas eliminar este registro?');
	readonly confirmed = output();
	readonly cancelled = output();

	@HostListener('document:keydown.escape')
	protected onEscape(): void {
		if (this.open()) { this.cancelled.emit(); }
	}
}
