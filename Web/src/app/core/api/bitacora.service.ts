import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api.config';

export interface BitacoraEntry {
	id: number;
	fechaHora: string;
	actorNombre: string;
	accion: string;
	detalle: string;
}

@Injectable({ providedIn: 'root' })
export class BitacoraService {
	private readonly _http = inject(HttpClient);

	getAll() {
		return this._http.get<BitacoraEntry[]>(`${API_URL}/bitacora`);
	}
}
