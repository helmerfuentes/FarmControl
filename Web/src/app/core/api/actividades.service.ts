import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Actividad } from '../models';
import { API_URL } from './api.config';

export interface ActividadRequest {
	parcelaId: number;
	tipoActividad: string;
	fechaInicio: string;
	fechaFin: string | null;
	personaACargoId: number | null;
	valorDiaActividad: number;
	descripcion: string;
}

@Injectable({ providedIn: 'root' })
export class ActividadesService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/actividades`;

	getAll(parcelaId?: number) {
		if (parcelaId) {
			return this._http.get<Actividad[]>(this._base, { params: { parcelaId: parcelaId.toString() } });
		}
		return this._http.get<Actividad[]>(this._base);
	}

	create(data: ActividadRequest) {
		return this._http.post<Actividad>(this._base, data);
	}

	update(id: number, data: ActividadRequest) {
		return this._http.put<Actividad>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}

	confirmar(id: number, confirmadaPor: string) {
		return this._http.post<Actividad>(`${this._base}/${id}/confirmar`, { confirmadaPor });
	}

	getMisActividades(fecha?: string) {
		if (fecha) {
			return this._http.get<Actividad[]>(`${this._base}/mis-actividades`, { params: { fecha } });
		}
		return this._http.get<Actividad[]>(`${this._base}/mis-actividades`);
	}
}
