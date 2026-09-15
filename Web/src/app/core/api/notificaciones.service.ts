import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api.config';

export interface Notificacion {
	tipo: string;
	titulo: string;
	mensaje: string;
	rutaDestino: string;
}

@Injectable({ providedIn: 'root' })
export class NotificacionesService {
	private readonly _http = inject(HttpClient);

	getAll() {
		return this._http.get<Notificacion[]>(`${API_URL}/notificaciones`);
	}
}
