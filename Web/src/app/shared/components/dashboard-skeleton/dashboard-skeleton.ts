import { Component } from '@angular/core';

@Component({
	selector: 'fc-dashboard-skeleton',
	template: `
		<div class="skel-kpi-grid">
			@for (i of [0, 1, 2, 3]; track i) {
				<div class="skel-kpi">
					<span class="skel skel-kpi-label"></span>
					<span class="skel skel-kpi-value"></span>
				</div>
			}
		</div>
		<div class="skel-charts-row">
			<span class="skel skel-chart"></span>
			<span class="skel skel-chart"></span>
		</div>
		<div class="skel-table-block">
			@for (i of [0, 1, 2, 3]; track i) {
				<div class="skel-row">
					<span class="skel"></span>
					<span class="skel"></span>
					<span class="skel"></span>
					<span class="skel"></span>
				</div>
			}
		</div>
	`,
	styleUrl: './dashboard-skeleton.scss',
})
export class DashboardSkeletonComponent {}
