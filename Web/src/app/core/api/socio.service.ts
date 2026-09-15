import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ProcesoCultivo } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class SocioService {
	private readonly _http = inject(HttpClient);

	getMisProcesos() {
		return this._http.get<ProcesoCultivo[]>(`${API_URL}/socio/mis-procesos`);
	}
}
