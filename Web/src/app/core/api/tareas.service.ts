import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TareaRecurrente } from '../models';
import { API_URL } from './api.config';

export interface TareaRecurrenteRequest {
	parcelaId: number;
	descripcion: string;
	frecuenciaDias: number;
	proximaFecha: string;
}

@Injectable({ providedIn: 'root' })
export class TareasService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/tareas-recurrentes`;

	getAll(parcelaId?: number) {
		if (parcelaId) {
			return this._http.get<TareaRecurrente[]>(this._base, { params: { parcelaId: parcelaId.toString() } });
		}
		return this._http.get<TareaRecurrente[]>(this._base);
	}

	create(data: TareaRecurrenteRequest) {
		return this._http.post<TareaRecurrente>(this._base, data);
	}

	completar(id: number) {
		return this._http.post<TareaRecurrente>(`${this._base}/${id}/completar`, {});
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
