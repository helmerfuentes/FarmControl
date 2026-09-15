import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TipoInsumo } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class TiposInsumoService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/tipos-insumo`;

	getAll() {
		return this._http.get<TipoInsumo[]>(this._base);
	}

	create(data: Omit<TipoInsumo, 'id'>) {
		return this._http.post<TipoInsumo>(this._base, data);
	}

	update(id: number, data: Omit<TipoInsumo, 'id'>) {
		return this._http.put<TipoInsumo>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
