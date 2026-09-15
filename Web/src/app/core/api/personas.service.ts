import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Persona, TipoPersona } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class PersonasService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/personas`;

	getAll(tipo?: TipoPersona) {
		if (tipo) {
			return this._http.get<Persona[]>(this._base, { params: { tipo } });
		}
		return this._http.get<Persona[]>(this._base);
	}

	getById(id: number) {
		return this._http.get<Persona>(`${this._base}/${id}`);
	}

	create(data: Omit<Persona, 'id'> & { nombreUsuario?: string | null; password?: string | null }) {
		return this._http.post<Persona>(this._base, data);
	}

	update(id: number, data: Omit<Persona, 'id'> & { nombreUsuario?: string | null; password?: string | null }) {
		return this._http.put<Persona>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
