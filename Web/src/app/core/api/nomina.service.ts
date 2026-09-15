import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LiquidacionNomina, PendientesLiquidacion } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class NominaService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/nomina`;

	getPendientes(jornaleroId: number, fechaInicio: string, fechaFin: string) {
		return this._http.get<PendientesLiquidacion>(`${this._base}/pendientes`, {
			params: { jornaleroId: jornaleroId.toString(), fechaInicio, fechaFin },
		});
	}

	crearLiquidacion(data: { jornaleroId: number; fechaInicio: string; fechaFin: string; observacion: string | null }) {
		return this._http.post<LiquidacionNomina>(`${this._base}/liquidaciones`, data);
	}

	getLiquidaciones(jornaleroId?: number) {
		if (jornaleroId) {
			return this._http.get<LiquidacionNomina[]>(`${this._base}/liquidaciones`, { params: { jornaleroId: jornaleroId.toString() } });
		}
		return this._http.get<LiquidacionNomina[]>(`${this._base}/liquidaciones`);
	}
}
