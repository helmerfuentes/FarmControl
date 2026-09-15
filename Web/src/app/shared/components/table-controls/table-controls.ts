import { Component, input, output } from '@angular/core';

@Component({
	selector: 'app-table-controls',
	templateUrl: './table-controls.html',
	styleUrl: './table-controls.scss',
})
export class TableControlsComponent {
	readonly total    = input.required<number>();
	readonly page     = input.required<number>();
	readonly pageSize = input.required<number>();

	readonly searchChange   = output<string>();
	readonly pageChange     = output<number>();
	readonly pageSizeChange = output<number>();

	protected readonly pageSizes = [10, 25, 50];

	protected get totalPages(): number {
		return Math.max(1, Math.ceil(this.total() / this.pageSize()));
	}

	protected get from(): number {
		return this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1;
	}

	protected get to(): number {
		return Math.min(this.page() * this.pageSize(), this.total());
	}

	protected get pages(): number[] {
		const tp = this.totalPages;
		const p  = this.page();
		const start = Math.max(1, p - 2);
		const end   = Math.min(tp, start + 4);
		return Array.from({ length: end - start + 1 }, (_, i) => start + i);
	}

	protected onSearch(event: Event): void {
		this.searchChange.emit((event.target as HTMLInputElement).value);
	}

	protected onPageSize(event: Event): void {
		this.pageSizeChange.emit(Number((event.target as HTMLSelectElement).value));
	}

	protected goTo(p: number): void {
		if (p >= 1 && p <= this.totalPages) { this.pageChange.emit(p); }
	}
}
