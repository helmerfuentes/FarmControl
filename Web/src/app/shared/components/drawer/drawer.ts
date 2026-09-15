import { Component, input, output, HostListener } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
	selector: 'fc-drawer',
	imports: [NgClass],
	template: `
		@if (open()) {
			<div class="overlay" (click)="closeOnBackdrop() && closed.emit()"></div>
		}
		<aside class="drawer" [ngClass]="{ open: open() }">
				<div class="drawer-grip" aria-hidden="true"></div>

			<div class="drawer-header">
				<h2 class="drawer-title">{{ title() }}</h2>
				<button class="close-btn" (click)="closed.emit()" aria-label="Cerrar">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M18 6L6 18M6 6l12 12"/>
					</svg>
				</button>
			</div>
			<div class="drawer-body">
				<ng-content/>
			</div>
		</aside>
	`,
	styleUrl: './drawer.scss',
})
export class DrawerComponent {
	readonly title = input.required<string>();
	readonly open = input.required<boolean>();
	readonly closeOnBackdrop = input<boolean>(true);
	readonly closed = output();

	@HostListener('document:keydown.escape')
	protected onEscape(): void {
		if (this.open()) { this.closed.emit(); }
	}
}
