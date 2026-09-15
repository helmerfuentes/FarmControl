import { Component, input } from '@angular/core';

@Component({
	selector: 'fc-empty-state',
	template: `
		<div class="empty-state">
			<svg class="empty-icon" viewBox="0 0 48 48" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true">
				<path d="M8 16l4-8h24l4 8M8 16v20a2 2 0 002 2h28a2 2 0 002-2V16M8 16h32M18 24h12" stroke-linecap="round" stroke-linejoin="round"/>
			</svg>
			<p class="empty-title">{{ title() }}</p>
			@if (message()) {
				<p class="empty-message">{{ message() }}</p>
			}
			<div class="empty-action">
				<ng-content/>
			</div>
		</div>
	`,
	styleUrl: './empty-state.scss',
})
export class EmptyStateComponent {
	readonly title = input.required<string>();
	readonly message = input('');
}
