import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Finca, Parcela } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class FincasService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/fincas`;

	getAll() {
		return this._http.get<Finca[]>(this._base);
	}

	getById(id: number) {
		return this._http.get<Finca>(`${this._base}/${id}`);
	}

	getParcelas(fincaId: number) {
		return this._http.get<Parcela[]>(`${this._base}/${fincaId}/parcelas`);
	}

	update(id: number, data: Omit<Finca, 'id'>) {
		return this._http.put<Finca>(`${this._base}/${id}`, data);
	}

	createParcela(data: Omit<Parcela, 'id'>) {
		return this._http.post<Parcela>(`${this._base}/${data.fincaId}/parcelas`, data);
	}

	updateParcela(id: number, data: Omit<Parcela, 'id'>) {
		return this._http.put<Parcela>(`${API_URL}/parcelas/${id}`, data);
	}

	deleteParcela(id: number) {
		return this._http.delete<void>(`${API_URL}/parcelas/${id}`);
	}
}
