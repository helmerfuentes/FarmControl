import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from './api.config';

export interface BackupInfo {
	nombreArchivo: string;
	fecha: string;
	tamanoBytes: number;
}

@Injectable({ providedIn: 'root' })
export class BackupsService {
	private readonly _http = inject(HttpClient);
	private readonly _base = `${API_URL}/backups`;

	getAll() {
		return this._http.get<BackupInfo[]>(this._base);
	}

	crear() {
		return this._http.post<BackupInfo>(this._base, {});
	}

	descargar(nombreArchivo: string) {
		return this._http.get(`${this._base}/${nombreArchivo}/descargar`, { responseType: 'blob' });
	}
}
