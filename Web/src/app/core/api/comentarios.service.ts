import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Comentario, TipoEntidadComentario } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class ComentariosService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/comentarios`;

	getAll(tipoEntidad: TipoEntidadComentario, entidadId: number) {
		return this._http.get<Comentario[]>(this._base, { params: { tipoEntidad, entidadId: entidadId.toString() } });
	}

	create(tipoEntidad: TipoEntidadComentario, entidadId: number, texto: string) {
		return this._http.post<Comentario>(this._base, { tipoEntidad, entidadId, texto });
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
