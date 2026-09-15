import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MiClienteService, PersonaDelCliente, AccesoFinca } from '../../core/api/mi-cliente.service';
import { NivelAcceso, nivelAccesoDesde, accesoDesdeNivel } from '../../core/api/clientes.service';
import { FincasService } from '../../core/api/fincas.service';
import { Finca } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state';

@Component({
	selector: 'app-usuarios',
	imports: [ReactiveFormsModule, DrawerComponent, EmptyStateComponent],
	templateUrl: './usuarios.html',
	styleUrl: './usuarios.scss',
})
export class UsuariosComponent implements OnInit {
	private readonly _svc = inject(MiClienteService);
	private readonly _fincasSvc = inject(FincasService);
	private readonly _fb = inject(FormBuilder);

	protected readonly usuarios = signal<PersonaDelCliente[]>([]);
	protected readonly fincas = signal<Finca[]>([]);
	protected readonly loading = signal(false);
	protected readonly saving = signal(false);
	protected readonly panelOpen = signal(false);
	protected readonly errorMsg = signal<string | null>(null);

	protected readonly usuarioForm = this._fb.group({
		nombre:        ['', Validators.required],
		documento:     ['', Validators.required],
		telefono:      ['', Validators.required],
		email:         [''],
		tipoPersona:   ['Admin' as 'Admin' | 'Contador', Validators.required],
		nombreUsuario: ['', Validators.required],
		contrasena:    ['', [Validators.required, Validators.minLength(6)]],
	});

	ngOnInit(): void {
		this.load();
		this._fincasSvc.getAll().subscribe(fincas => this.fincas.set(fincas));
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getUsuarios().subscribe({
			next:  u => { this.usuarios.set(u); this.loading.set(false); },
			error: () => this.loading.set(false),
		});
	}

	private readonly _nivelesSeleccionados = signal<Map<number, NivelAcceso>>(new Map());

	protected toggleFincaSeleccionada(fincaId: number, checked: boolean): void {
		this._nivelesSeleccionados.update(map => {
			const nuevo = new Map(map);
			if (checked) { nuevo.set(fincaId, 'total'); } else { nuevo.delete(fincaId); }
			return nuevo;
		});
	}

	protected estaSeleccionada(fincaId: number): boolean {
		return this._nivelesSeleccionados().has(fincaId);
	}

	protected nivelSeleccionado(fincaId: number): NivelAcceso {
		return this._nivelesSeleccionados().get(fincaId) ?? 'total';
	}

	protected onNivelSeleccionadoChange(fincaId: number, nivel: NivelAcceso): void {
		this._nivelesSeleccionados.update(map => {
			const nuevo = new Map(map);
			nuevo.set(fincaId, nivel);
			return nuevo;
		});
	}

	protected openNew(): void {
		this.errorMsg.set(null);
		this.usuarioForm.reset();
		this._nivelesSeleccionados.set(new Map());
		this.panelOpen.set(true);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
	}

	protected guardarUsuario(): void {
		if (this.usuarioForm.invalid || this.saving()) { return; }
		const accesos = Array.from(this._nivelesSeleccionados().entries())
			.map(([fincaId, nivel]) => ({ fincaId, ...accesoDesdeNivel(nivel) }));
		if (accesos.length === 0) {
			this.errorMsg.set('Selecciona al menos una finca.');
			return;
		}
		const raw = this.usuarioForm.value;
		this.saving.set(true);
		this.errorMsg.set(null);
		this._svc.crearUsuario({
			nombre: raw.nombre!,
			documento: raw.documento!,
			telefono: raw.telefono!,
			email: raw.email || null,
			tipoPersona: raw.tipoPersona!,
			nombreUsuario: raw.nombreUsuario!,
			contrasena: raw.contrasena!,
			accesos,
		}).subscribe({
			next: () => { this.saving.set(false); this.closePanel(); this.load(); },
			error: err => { this.saving.set(false); this.errorMsg.set(err?.error?.error ?? 'No se pudo crear el usuario.'); },
		});
	}

	protected nivelAccesoDesde = nivelAccesoDesde;

	protected onCambiarNivelAcceso(personaId: number, acceso: AccesoFinca, nivel: NivelAcceso): void {
		const { soloLectura, puedeEliminar } = accesoDesdeNivel(nivel);
		this._svc.setPermisoFinca(personaId, acceso.fincaId, soloLectura, puedeEliminar).subscribe(() => this.load());
	}

	protected toggleUsuarioActivo(persona: PersonaDelCliente): void {
		this._svc.setUsuarioActivo(persona.id, !persona.activo).subscribe({
			next:  () => this.load(),
			error: err => this.errorMsg.set(err?.error?.error ?? 'No se pudo actualizar el usuario.'),
		});
	}
}
