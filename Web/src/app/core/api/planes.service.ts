import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Plan } from '../models';
import { API_URL } from './api.config';

export interface CrearPlanRequest {
	nombre: string;
	descripcion: string | null;
	maxFincas: number;
	maxUsuarios: number;
}

@Injectable({ providedIn: 'root' })
export class PlanesService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/planes`;

	getAll() {
		return this._http.get<Plan[]>(this._base);
	}

	getPublicos() {
		return this._http.get<Plan[]>(`${this._base}/publicos`);
	}

	create(data: CrearPlanRequest) {
		return this._http.post<Plan>(this._base, data);
	}

	update(id: number, data: CrearPlanRequest) {
		return this._http.put<Plan>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
