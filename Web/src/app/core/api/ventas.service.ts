import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CompradorFrecuente, PagoVenta, Venta } from '../models';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class VentasService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/ventas`;

	getCompradoresFrecuentes() {
		return this._http.get<CompradorFrecuente[]>(`${this._base}/compradores-frecuentes`);
	}

	getAll(parcelaId?: number) {
		if (parcelaId) {
			return this._http.get<Venta[]>(this._base, { params: { parcelaId: parcelaId.toString() } });
		}
		return this._http.get<Venta[]>(this._base);
	}

	create(data: Omit<Venta, 'id' | 'parcelaNombre' | 'compradorNombre'>) {
		return this._http.post<Venta>(this._base, data);
	}

	update(id: number, data: Omit<Venta, 'id' | 'parcelaNombre' | 'compradorNombre'>) {
		return this._http.put<Venta>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}

	getPagos(ventaId: number) {
		return this._http.get<PagoVenta[]>(`${this._base}/${ventaId}/pagos`);
	}

	registrarPago(ventaId: number, data: { monto: number; fecha: string; observacion: string | null }) {
		return this._http.post<PagoVenta>(`${this._base}/${ventaId}/pagos`, data);
	}
}
