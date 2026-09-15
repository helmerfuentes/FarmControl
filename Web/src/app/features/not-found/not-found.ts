import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';

@Component({
	selector: 'app-not-found',
	imports: [RouterLink, EmptyStateComponent],
	template: `
		<div class="not-found-page">
			<fc-empty-state title="Esta página no existe." message="El enlace que seguiste no corresponde a ninguna sección de FarmControl.">
				<a class="btn btn-primary" routerLink="/app">Volver al inicio</a>
			</fc-empty-state>
		</div>
	`,
	styles: [`
		.not-found-page {
			display: flex;
			align-items: center;
			justify-content: center;
			min-height: 60vh;
			padding: 2rem;
		}
	`],
})
export class NotFoundComponent {}
