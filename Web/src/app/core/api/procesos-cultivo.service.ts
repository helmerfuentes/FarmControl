import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ProcesoCultivo, EstadoProceso } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class ProcesosCultivoService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/procesos-cultivo`;

	getAll(parcelaId?: number, estado?: EstadoProceso) {
		if (parcelaId && estado) {
			return this._http.get<ProcesoCultivo[]>(this._base, { params: { parcelaId, estado } });
		}
		if (parcelaId) {
			return this._http.get<ProcesoCultivo[]>(this._base, { params: { parcelaId } });
		}
		if (estado) {
			return this._http.get<ProcesoCultivo[]>(this._base, { params: { estado } });
		}
		return this._http.get<ProcesoCultivo[]>(this._base);
	}

	create(data: Omit<ProcesoCultivo, 'id' | 'parcelaNombre' | 'productoNombre' | 'socioNombre' | 'fechaCierre' | 'estado'>) {
		return this._http.post<ProcesoCultivo>(this._base, data);
	}

	update(id: number, data: Omit<ProcesoCultivo, 'parcelaNombre' | 'productoNombre' | 'socioNombre'>) {
		return this._http.put<ProcesoCultivo>(`${this._base}/${id}`, data);
	}

	cerrar(id: number, body: { estado: EstadoProceso; fechaCierre: string }) {
		return this._http.post<ProcesoCultivo>(`${this._base}/${id}/cerrar`, body);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}
}
