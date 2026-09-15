import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

interface LoginResponse {
  token: string;
  rol: string;
}

interface AuthState {
  token: string | null;
  rol: string | null;
  usuario: string | null;
  personaId: number | null;
  clienteId: number | null;
  fincaIds: number[];
}

const TOKEN_KEY      = 'fc_token';
const ROL_KEY        = 'fc_rol';
const USER_KEY       = 'fc_user';
const PERSONA_ID_KEY = 'fc_persona_id';
const CLIENTE_ID_KEY = 'fc_cliente_id';
const FINCA_IDS_KEY  = 'fc_finca_ids';
const API_URL        = 'http://localhost:5279';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _http   = inject(HttpClient);
  private readonly _router = inject(Router);

  private readonly _state = signal<AuthState>({
    token:     localStorage.getItem(TOKEN_KEY),
    rol:       localStorage.getItem(ROL_KEY),
    usuario:   localStorage.getItem(USER_KEY),
    personaId: Number(localStorage.getItem(PERSONA_ID_KEY)) || null,
    clienteId: Number(localStorage.getItem(CLIENTE_ID_KEY)) || null,
    fincaIds:  this._parseFincaIdsCsv(localStorage.getItem(FINCA_IDS_KEY)),
  });

  readonly isAuthenticated = computed(() => !!this._state().token);
  readonly rol             = computed(() => this._state().rol);
  readonly usuario         = computed(() => this._state().usuario);
  readonly isAdmin         = computed(() => this._state().rol === 'Admin');
  readonly isSocio         = computed(() => this._state().rol === 'Socio');
  readonly isSuperAdmin    = computed(() => this._state().rol === 'SuperAdmin');
  readonly isJornalero     = computed(() => this._state().rol === 'Jornalero');
  readonly personaId       = computed(() => this._state().personaId);
  readonly clienteId       = computed(() => this._state().clienteId);
  readonly fincaIds        = computed(() => this._state().fincaIds);
  readonly token           = computed(() => this._state().token);

  login(usuario: string, contrasena: string) {
    return this._http.post<LoginResponse>(`${API_URL}/auth/login`, { usuario, contrasena });
  }

  cambiarContrasena(contrasenaActual: string, contrasenaNueva: string) {
    return this._http.put<void>(`${API_URL}/auth/contrasena`, { contrasenaActual, contrasenaNueva });
  }

  getPreferenciasDashboard() {
    return this._http.get<{ preferenciasJson: string | null }>(`${API_URL}/auth/preferencias-dashboard`);
  }

  guardarPreferenciasDashboard(preferenciasJson: string) {
    return this._http.put<void>(`${API_URL}/auth/preferencias-dashboard`, { preferenciasJson });
  }

  setSession(token: string, rol: string, usuario: string): void {
    const personaId = this._parseJwtClaim(token, 'personaId');
    const personaIdNum = personaId ? Number(personaId) : null;

    const clienteId = this._parseJwtClaim(token, 'clienteId');
    const clienteIdNum = clienteId ? Number(clienteId) : null;

    const fincaIdsCsv = this._parseJwtClaim(token, 'fincaIds');
    const fincaIds = this._parseFincaIdsCsv(fincaIdsCsv);

    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(ROL_KEY, rol);
    localStorage.setItem(USER_KEY, usuario);

    if (personaIdNum !== null) {
      localStorage.setItem(PERSONA_ID_KEY, String(personaIdNum));
    } else {
      localStorage.removeItem(PERSONA_ID_KEY);
    }

    if (clienteIdNum !== null) {
      localStorage.setItem(CLIENTE_ID_KEY, String(clienteIdNum));
    } else {
      localStorage.removeItem(CLIENTE_ID_KEY);
    }

    if (fincaIds.length > 0) {
      localStorage.setItem(FINCA_IDS_KEY, fincaIds.join(','));
    } else {
      localStorage.removeItem(FINCA_IDS_KEY);
    }

    this._state.set({ token, rol, usuario, personaId: personaIdNum, clienteId: clienteIdNum, fincaIds });
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(ROL_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(PERSONA_ID_KEY);
    localStorage.removeItem(CLIENTE_ID_KEY);
    localStorage.removeItem(FINCA_IDS_KEY);
    this._state.set({ token: null, rol: null, usuario: null, personaId: null, clienteId: null, fincaIds: [] });
    this._router.navigate(['/']);
  }

  private _parseJwtClaim(token: string, claim: string): string | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload[claim] ?? null;
    } catch {
      return null;
    }
  }

  private _parseFincaIdsCsv(csv: string | null): number[] {
    let fincaIds: number[];
    if (!csv) {
      fincaIds = [];
    } else {
      fincaIds = csv.split(',')
        .map(id => Number(id))
        .filter(id => !Number.isNaN(id));
    }
    return fincaIds;
  }
}
