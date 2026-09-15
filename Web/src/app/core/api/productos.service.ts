import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Producto } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class ProductosService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/productos`;

	getAll() {
		return this._http.get<Producto[]>(this._base);
	}

	getById(id: number) {
		return this._http.get<Producto>(`${this._base}/${id}`);
	}

	create(data: Omit<Producto, 'id'>) {
		return this._http.post<Producto>(this._base, data);
	}

	update(id: number, data: Omit<Producto, 'id'>) {
		return this._http.put<Producto>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
