import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AnalisisSuelo } from '../models';
import { API_URL } from './api.config';

export interface AnalisisSueloRequest {
	parcelaId: number;
	fecha: string;
	ph: number | null;
	materiaOrganica: number | null;
	nitrogeno: number | null;
	fosforo: number | null;
	potasio: number | null;
	observacion: string | null;
}

@Injectable({ providedIn: 'root' })
export class AnalisisSueloService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/analisis-suelo`;

	getAll(parcelaId: number) {
		return this._http.get<AnalisisSuelo[]>(this._base, { params: { parcelaId: parcelaId.toString() } });
	}

	create(data: AnalisisSueloRequest) {
		return this._http.post<AnalisisSuelo>(this._base, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
