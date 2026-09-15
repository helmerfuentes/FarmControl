import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Asistencia } from '../models';
import { API_URL } from './api.config';

export interface AsistenciaRequest {
	personaId: number;
	fincaId: number;
	fecha: string;
	horaEntrada: string | null;
	horaSalida: string | null;
	observacion: string | null;
}

export interface AsistenciaFiltro {
	personaId?: number;
	fincaId?: number;
	desde?: string;
	hasta?: string;
}

@Injectable({ providedIn: 'root' })
export class AsistenciasService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/asistencias`;

	getAll(filtro: AsistenciaFiltro = {}) {
		const params: Record<string, string> = {};
		if (filtro.personaId) { params['personaId'] = filtro.personaId.toString(); }
		if (filtro.fincaId) { params['fincaId'] = filtro.fincaId.toString(); }
		if (filtro.desde) { params['desde'] = filtro.desde; }
		if (filtro.hasta) { params['hasta'] = filtro.hasta; }
		return this._http.get<Asistencia[]>(this._base, { params });
	}

	create(data: AsistenciaRequest) {
		return this._http.post<Asistencia>(this._base, data);
	}

	update(id: number, data: { horaEntrada: string | null; horaSalida: string | null; observacion: string | null }) {
		return this._http.put<Asistencia>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
