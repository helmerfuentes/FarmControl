import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api.config';
import { EstadoProceso } from '../models';

export interface ResumenParcela {
	id: number;
	nombre: string;
	producto: string;
	area: number;
	totalIngresos: number;
	totalManoObra: number;
}

export interface ResumenFinca {
	fincaId: number;
	nombre: string;
	ubicacion: string | null;
	areaTotal: number;
	totalIngresos: number;
	totalCompras: number;
	totalManoObra: number;
	utilidad: number;
	parcelas: ResumenParcela[];
}

export interface MesResumen {
	anio: number;
	mes: number;
	totalIngresos: number;
	totalCompras: number;
	totalManoObra: number;
	utilidad: number;
}

export interface HistoricoFinca {
	fincaId: number;
	nombre: string;
	meses: MesResumen[];
}

export interface PrecioPorMes {
	mes: number;
	anio: number;
	precioPromedio: number;
	precioMin: number;
	precioMax: number;
	numVentas: number;
}

export interface HistorialPrecios {
	productoId: number;
	productoNombre: string;
	clasificacion: string;
	unidadMedida: string;
	porMes: PrecioPorMes[];
}

export interface ResumenProcesoCultivo {
	procesoCultivoId: number;
	parcelaNombre: string;
	productoNombre: string;
	estado: EstadoProceso;
	fechaInicio: string;
	fechaEstimadaCosecha: string | null;
	fechaCierre: string | null;
	costoInicial: number;
	totalIngresos: number;
	totalCompras: number;
	totalManoObra: number;
	utilidad: number;
}

export interface ResumenConsolidadoFinca {
	fincaId: number;
	nombre: string;
	totalIngresos: number;
	totalCompras: number;
	totalManoObra: number;
	utilidad: number;
}

export interface ResumenConsolidado {
	totalIngresos: number;
	totalCompras: number;
	totalManoObra: number;
	utilidad: number;
	fincas: ResumenConsolidadoFinca[];
}

@Injectable({ providedIn: 'root' })
export class ReportesService {
	private readonly _http = inject(HttpClient);

	getResumenFinca(fincaId: number) {
		return this._http.get<ResumenFinca>(`${API_URL}/reportes/finca/${fincaId}/resumen`);
	}

	getResumenConsolidado() {
		return this._http.get<ResumenConsolidado>(`${API_URL}/reportes/consolidado`);
	}

	getResumenProceso(procesoCultivoId: number) {
		return this._http.get<ResumenProcesoCultivo>(`${API_URL}/reportes/proceso-cultivo/${procesoCultivoId}/resumen`);
	}

	getHistoricoFinca(fincaId: number, meses = 12) {
		return this._http.get<HistoricoFinca>(`${API_URL}/reportes/finca/${fincaId}/historico`, { params: { meses } });
	}

	getHistorialPrecios(productoId: number, clasificacion?: string, anio?: number) {
		let params: Record<string, string | number> = { productoId };
		if (clasificacion) { params = { ...params, clasificacion }; }
		if (anio) { params = { ...params, anio }; }
		return this._http.get<HistorialPrecios[]>(`${API_URL}/reportes/precios`, { params });
	}

	getEstacionalidadPrecios(productoId: number, clasificacion?: string) {
		let params: Record<string, string | number> = { productoId };
		if (clasificacion) { params = { ...params, clasificacion }; }
		return this._http.get<EstacionalidadPrecios>(`${API_URL}/reportes/estacionalidad`, { params });
	}

	compartir(fincaId: number, diasValidez?: number) {
		return this._http.post<ReporteCompartido>(`${API_URL}/reportes/finca/${fincaId}/compartir`, { diasValidez: diasValidez ?? null });
	}

	getReporteCompartido(token: string) {
		return this._http.get<ResumenFinca>(`${API_URL}/reportes/compartido/${token}`);
	}
}

export interface ReporteCompartido {
	token: string;
	fincaId: number;
	fechaCreacion: string;
	fechaExpiracion: string | null;
}

export interface EstacionalidadMes {
	mes: number;
	precioPromedio: number | null;
	precioMin: number | null;
	precioMax: number | null;
	numVentas: number;
}

export interface EstacionalidadPrecios {
	productoId: number;
	productoNombre: string;
	clasificacion: string | null;
	porMes: EstacionalidadMes[];
}
