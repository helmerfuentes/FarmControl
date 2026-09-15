import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Cliente, Finca, TipoPersona } from '../models';
import { API_URL } from './api.config';

export interface FincaConAcceso {
	id: number;
	nombre: string;
	ubicacion: string | null;
	cantidadUsuarios: number;
}

export interface ClienteDetalle extends Cliente {
	fincas: FincaConAcceso[];
}

export interface CrearClienteRequest {
	razonSocial: string;
	nit: string;
	email: string;
	telefono: string | null;
	fechaVencimientoContrato: string | null;
	planId: number | null;
}

export interface ActualizarContratoRequest {
	fechaVencimientoContrato: string | null;
	activo: boolean;
}

export interface CrearFincaClienteRequest {
	nombre: string;
	ubicacion: string | null;
	areaTotal: number;
	costoTerreno: number;
}

export type NivelAcceso = 'lectura' | 'escritura' | 'total';

export interface AccesoFincaInput {
	fincaId: number;
	soloLectura: boolean;
	puedeEliminar: boolean;
}

export interface CrearUsuarioClienteRequest {
	nombre: string;
	documento: string;
	telefono: string;
	email: string | null;
	tipoPersona: TipoPersona;
	nombreUsuario: string;
	contrasena: string;
	accesos: AccesoFincaInput[];
}

export interface AccesoFinca {
	fincaId: number;
	fincaNombre: string;
	soloLectura: boolean;
	puedeEliminar: boolean;
}

export function nivelAccesoDesde(acceso: { soloLectura: boolean; puedeEliminar: boolean }): NivelAcceso {
	if (acceso.soloLectura) { return 'lectura'; }
	return acceso.puedeEliminar ? 'total' : 'escritura';
}

export function accesoDesdeNivel(nivel: NivelAcceso): { soloLectura: boolean; puedeEliminar: boolean } {
	switch (nivel) {
		case 'lectura':   return { soloLectura: true, puedeEliminar: false };
		case 'total':     return { soloLectura: false, puedeEliminar: true };
		default:          return { soloLectura: false, puedeEliminar: false };
	}
}

export interface PersonaDelCliente {
	id: number;
	nombre: string;
	tipoPersona: TipoPersona;
	nombreUsuario: string | null;
	activo: boolean;
	fincas: AccesoFinca[];
}

export interface ClienteResumenSalud {
	clienteId: number;
	razonSocial: string;
	activo: boolean;
	fechaAlta: string;
	numFincas: number;
	numPersonas: number;
}

export interface BitacoraResumenSalud {
	fechaHora: string;
	clienteRazonSocial: string | null;
	actorNombre: string;
	accion: string;
	detalle: string;
}

export interface SaludSistema {
	totalClientes: number;
	clientesActivos: number;
	totalFincas: number;
	totalPersonas: number;
	totalParcelas: number;
	tamanoBaseDatosBytes: number;
	clientes: ClienteResumenSalud[];
	bitacoraReciente: BitacoraResumenSalud[];
}

@Injectable({ providedIn: 'root' })
export class ClientesService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/clientes`;

	getAll() {
		return this._http.get<Cliente[]>(this._base);
	}

	getById(id: number) {
		return this._http.get<ClienteDetalle>(`${this._base}/${id}`);
	}

	create(data: CrearClienteRequest) {
		return this._http.post<Cliente>(this._base, data);
	}

	actualizarContrato(clienteId: number, data: ActualizarContratoRequest) {
		return this._http.put<Cliente>(`${this._base}/${clienteId}/contrato`, data);
	}

	asignarPlan(clienteId: number, planId: number | null) {
		return this._http.put<Cliente>(`${this._base}/${clienteId}/plan`, { planId });
	}

	createFinca(clienteId: number, data: CrearFincaClienteRequest) {
		return this._http.post<Finca>(`${this._base}/${clienteId}/fincas`, data);
	}

	createUsuario(clienteId: number, data: CrearUsuarioClienteRequest) {
		return this._http.post(`${this._base}/${clienteId}/usuarios`, data);
	}

	getUsuarios(clienteId: number) {
		return this._http.get<PersonaDelCliente[]>(`${this._base}/${clienteId}/usuarios`);
	}

	setPermisoFinca(personaId: number, fincaId: number, soloLectura: boolean, puedeEliminar: boolean) {
		return this._http.put(`${API_URL}/personas-finca/${personaId}/${fincaId}`, { soloLectura, puedeEliminar });
	}

	setUsuarioActivo(personaId: number, activa: boolean) {
		return this._http.put(`${API_URL}/personas/${personaId}/activo`, { activa });
	}

	exportar(clienteId: number) {
		return this._http.get<Record<string, unknown>>(`${this._base}/${clienteId}/exportar`);
	}

	getSaludSistema() {
		return this._http.get<SaludSistema>(`${API_URL}/sistema/salud`);
	}
}
