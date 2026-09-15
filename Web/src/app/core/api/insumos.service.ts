import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Insumo, MovimientoInsumo } from '../models';
import { API_URL } from './api.config';

export interface SaldoInsumo {
	insumoId: number;
	insumoNombre: string;
	unidadMedida: string;
	entradas: number;
	salidas: number;
	saldo: number;
	stockMinimo: number | null;
	saldoBajo: boolean;
}

export interface PrecioInsumoPorMes {
	anio: number;
	mes: number;
	precioPromedio: number;
	precioMin: number;
	precioMax: number;
	numCompras: number;
}

export interface HistorialPrecioInsumo {
	insumoId: number;
	insumoNombre: string;
	unidadMedida: string;
	porMes: PrecioInsumoPorMes[];
}

@Injectable({ providedIn: 'root' })
export class InsumosService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/insumos`;

	getAll() {
		return this._http.get<Insumo[]>(this._base);
	}

	create(data: Omit<Insumo, 'id' | 'tipoInsumoNombre'>) {
		return this._http.post<Insumo>(this._base, data);
	}

	update(id: number, data: Omit<Insumo, 'id' | 'tipoInsumoNombre'>) {
		return this._http.put<Insumo>(`${this._base}/${id}`, data);
	}

	delete(id: number) {
		return this._http.delete<void>(`${this._base}/${id}`);
	}

	getMovimientos() {
		return this._http.get<MovimientoInsumo[]>(`${API_URL}/movimientos-insumo`);
	}

	getSaldos() {
		return this._http.get<SaldoInsumo[]>(`${this._base}/saldos`);
	}

	getHistorialPrecios(insumoId: number) {
		return this._http.get<HistorialPrecioInsumo>(`${this._base}/${insumoId}/historial-precios`);
	}

	createMovimiento(data: Omit<MovimientoInsumo, 'id' | 'insumoNombre' | 'parcelaNombre'>) {
		return this._http.post<MovimientoInsumo>(`${API_URL}/movimientos-insumo`, data);
	}

	transferir(data: TransferirInsumoRequest) {
		return this._http.post<TransferirInsumoResult>(`${API_URL}/movimientos-insumo/transferir`, data);
	}
}

export interface TransferirInsumoRequest {
	insumoId: number;
	parcelaOrigenId: number;
	parcelaDestinoId: number;
	cantidad: number;
	fecha: string;
	observacion: string | null;
}

export interface TransferirInsumoResult {
	movimientoSalidaId: number;
	movimientoEntradaId: number;
}
