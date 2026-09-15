import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api.config';
import { PersonaDelCliente, AccesoFinca, CrearUsuarioClienteRequest } from './clientes.service';

export type { PersonaDelCliente, AccesoFinca };

@Injectable({ providedIn: 'root' })
export class MiClienteService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/mi-cliente`;

	getUsuarios() {
		return this._http.get<PersonaDelCliente[]>(`${this._base}/usuarios`);
	}

	crearUsuario(data: CrearUsuarioClienteRequest) {
		return this._http.post(`${this._base}/usuarios`, data);
	}

	setPermisoFinca(personaId: number, fincaId: number, soloLectura: boolean, puedeEliminar: boolean) {
		return this._http.put(`${this._base}/usuarios/${personaId}/fincas/${fincaId}`, { soloLectura, puedeEliminar });
	}

	setUsuarioActivo(personaId: number, activa: boolean) {
		return this._http.put(`${this._base}/usuarios/${personaId}/activo`, { activa });
	}

	asignarCredenciales(personaId: number, nombreUsuario: string, contrasena: string) {
		return this._http.put(`${this._base}/personas/${personaId}/credenciales`, { nombreUsuario, contrasena });
	}
}
