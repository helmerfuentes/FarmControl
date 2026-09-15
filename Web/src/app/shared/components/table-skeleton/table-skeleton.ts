import { Component, input, computed } from '@angular/core';

@Component({
	selector: 'fc-table-skeleton',
	template: `
		<div class="data-table-wrap">
			<div class="skel-header">
				@for (c of colsArr(); track c) {
					<span class="skel"></span>
				}
			</div>
			@for (r of rowsArr(); track r) {
				<div class="skel-row">
					@for (c of colsArr(); track c) {
						<span class="skel"></span>
					}
				</div>
			}
		</div>
	`,
	styleUrl: './table-skeleton.scss',
})
export class TableSkeletonComponent {
	readonly rows = input(6);
	readonly cols = input(4);

	protected readonly rowsArr = computed(() => Array.from({ length: this.rows() }));
	protected readonly colsArr = computed(() => Array.from({ length: this.cols() }));
}
