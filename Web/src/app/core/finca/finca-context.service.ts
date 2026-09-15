import { Injectable, inject, signal, computed } from '@angular/core';
import { AuthService } from '../auth/auth.service';

const SELECTED_FINCA_KEY = 'fc_selected_finca_id';

@Injectable({ providedIn: 'root' })
export class FincaContextService {
	private readonly _authService = inject(AuthService);

	private readonly _selectedFincaIdOverride = signal<number | null>(this._restoreSelection());

	readonly fincaIds = this._authService.fincaIds;
	readonly requiereSelector = computed(() => this.fincaIds().length > 1);

	readonly selectedFincaId = computed(() => {
		const ids = this.fincaIds();
		const override = this._selectedFincaIdOverride();

		let resuelto: number | null;
		if (override !== null && ids.includes(override)) {
			resuelto = override;
		} else if (ids.length > 0) {
			resuelto = ids[0];
		} else {
			resuelto = null;
		}

		return resuelto;
	});

	setSelectedFinca(fincaId: number): void {
		if (!this.fincaIds().includes(fincaId)) {
			return;
		}

		this._selectedFincaIdOverride.set(fincaId);

		try {
			sessionStorage.setItem(SELECTED_FINCA_KEY, String(fincaId));
		} catch {
			// sessionStorage no disponible (modo privado, etc.) — la selección solo dura la sesión de memoria
		}
	}

	private _restoreSelection(): number | null {
		let seleccion: number | null;

		try {
			const guardado = sessionStorage.getItem(SELECTED_FINCA_KEY);
			seleccion = guardado ? Number(guardado) : null;
		} catch {
			seleccion = null;
		}

		return seleccion;
	}
}
