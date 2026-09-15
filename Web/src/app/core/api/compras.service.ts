import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Compra, PagoCompra } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class ComprasService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/compras`;

	getAll(fincaId?: number) {
		if (fincaId) {
			return this._http.get<Compra[]>(this._base, { params: { fincaId: fincaId.toString() } });
		}
		return this._http.get<Compra[]>(this._base);
	}

	create(data: Omit<Compra, 'id' | 'fincaNombre' | 'socioNombre'>) {
		return this._http.post<Compra>(this._base, data);
	}

	update(id: number, data: Omit<Compra, 'id' | 'fincaNombre' | 'socioNombre'>) {
		return this._http.put<Compra>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}

	getPagos(compraId: number) {
		return this._http.get<PagoCompra[]>(`${this._base}/${compraId}/pagos`);
	}

	registrarPago(compraId: number, data: { monto: number; fecha: string; observacion: string | null }) {
		return this._http.post<PagoCompra>(`${this._base}/${compraId}/pagos`, data);
	}
}
