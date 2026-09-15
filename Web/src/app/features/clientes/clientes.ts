import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ClientesService, FincaConAcceso, PersonaDelCliente, NivelAcceso, nivelAccesoDesde, accesoDesdeNivel, SaludSistema } from '../../core/api/clientes.service';
import { PlanesService } from '../../core/api/planes.service';
import { Cliente, Finca, Plan } from '../../core/models';
import { DrawerComponent } from '../../shared/components/drawer/drawer';

@Component({
	selector: 'app-clientes',
	imports: [ReactiveFormsModule, DrawerComponent],
	templateUrl: './clientes.html',
	styleUrl: './clientes.scss',
})
export class ClientesComponent implements OnInit {
	private readonly _svc = inject(ClientesService);
	private readonly _planesSvc = inject(PlanesService);
	private readonly _fb  = inject(FormBuilder);

	protected readonly clientes = signal<Cliente[]>([]);
	protected readonly loading  = signal(false);
	protected readonly panelOpen = signal(false);
	protected readonly saving    = signal(false);
	protected readonly errorMsg  = signal<string | null>(null);

	// Wizard de alta: 1 = datos del cliente, 2 = fincas contratadas, 3 = usuario inicial, 4 = resumen
	protected readonly step = signal<1 | 2 | 3 | 4>(1);
	protected readonly clienteCreado = signal<Cliente | null>(null);
	protected readonly fincasCreadas = signal<Finca[]>([]);
	protected readonly usuarioCreadoNombre = signal<string | null>(null);

	protected readonly planes = signal<Plan[]>([]);

	// Gestión de un cliente existente: usuarios y fincas
	protected readonly clienteGestion = signal<Cliente | null>(null);
	protected readonly usuariosDelCliente = signal<PersonaDelCliente[]>([]);
	protected readonly cargandoUsuarios = signal(false);
	protected readonly fincasCliente = signal<FincaConAcceso[]>([]);

	protected readonly nivelAccesoWizard = signal<NivelAcceso>('total');
	protected readonly nivelAccesoGestion = signal<NivelAcceso>('total');
	protected readonly nivelAccesoDesde = nivelAccesoDesde;

	protected readonly clienteForm = this._fb.group({
		razonSocial: ['', Validators.required],
		nit:         ['', Validators.required],
		email:       ['', [Validators.required, Validators.email]],
		telefono:    [''],
		fechaVencimientoContrato: [''],
		planId:      [''],
	});

	protected readonly planForm = this._fb.group({
		nombre:       ['', Validators.required],
		descripcion:  [''],
		maxFincas:    [-1, Validators.required],
		maxUsuarios:  [-1, Validators.required],
	});

	protected readonly contratoForm = this._fb.group({
		fechaVencimientoContrato: [''],
		activo: [true],
	});

	protected readonly fincaForm = this._fb.group({
		nombre:       ['', Validators.required],
		ubicacion:    [''],
		areaTotal:    [0, [Validators.required, Validators.min(0)]],
		costoTerreno: [0, [Validators.required, Validators.min(0)]],
	});

	protected readonly usuarioForm = this._fb.group({
		nombre:        ['', Validators.required],
		documento:     ['', Validators.required],
		telefono:      ['', Validators.required],
		email:         [''],
		nombreUsuario: ['', Validators.required],
		contrasena:    ['', [Validators.required, Validators.minLength(6)]],
	});

	protected readonly nuevaFincaGestionForm = this._fb.group({
		nombre:       ['', Validators.required],
		ubicacion:    [''],
		areaTotal:    [0, [Validators.required, Validators.min(0)]],
		costoTerreno: [0, [Validators.required, Validators.min(0)]],
	});

	protected readonly nuevoUsuarioGestionForm = this._fb.group({
		nombre:        ['', Validators.required],
		documento:     ['', Validators.required],
		telefono:      ['', Validators.required],
		email:         [''],
		nombreUsuario: ['', Validators.required],
		contrasena:    ['', [Validators.required, Validators.minLength(6)]],
	});

	// --- Feature 26: planes de suscripción ---
	protected readonly panelPlanesOpen = signal(false);
	protected readonly editingPlanId = signal<number | null>(null);
	protected readonly savingPlan = signal(false);
	protected readonly planErrorMsg = signal<string | null>(null);

	private loadPlanes(): void {
		this._planesSvc.getAll().subscribe(planes => this.planes.set(planes));
	}

	protected openPlanesPanel(): void {
		this.planErrorMsg.set(null);
		this.editingPlanId.set(null);
		this.planForm.reset({ maxFincas: -1, maxUsuarios: -1 });
		this.panelPlanesOpen.set(true);
	}

	protected closePlanesPanel(): void {
		this.panelPlanesOpen.set(false);
		this.loadPlanes();
	}

	protected editarPlan(plan: Plan): void {
		this.planErrorMsg.set(null);
		this.editingPlanId.set(plan.id);
		this.planForm.setValue({
			nombre: plan.nombre,
			descripcion: plan.descripcion ?? '',
			maxFincas: plan.maxFincas,
			maxUsuarios: plan.maxUsuarios,
		});
	}

	protected guardarPlan(): void {
		if (this.planForm.invalid || this.savingPlan()) { return; }
		const raw = this.planForm.value;
		const data = {
			nombre: raw.nombre!,
			descripcion: raw.descripcion || null,
			maxFincas: Number(raw.maxFincas),
			maxUsuarios: Number(raw.maxUsuarios),
		};
		this.savingPlan.set(true);
		this.planErrorMsg.set(null);
		const id = this.editingPlanId();
		const req = id ? this._planesSvc.update(id, data) : this._planesSvc.create(data);
		req.subscribe({
			next: () => {
				this.savingPlan.set(false);
				this.editingPlanId.set(null);
				this.planForm.reset({ maxFincas: -1, maxUsuarios: -1 });
				this.loadPlanes();
			},
			error: err => { this.savingPlan.set(false); this.planErrorMsg.set(err?.error?.error ?? 'No se pudo guardar el plan.'); },
		});
	}

	protected eliminarPlan(plan: Plan): void {
		this._planesSvc.delete(plan.id).subscribe({
			next:  () => this.loadPlanes(),
			error: err => this.planErrorMsg.set(err?.error?.error ?? 'No se pudo eliminar el plan.'),
		});
	}

	// --- Feature 27: onboarding incompleto — retomar alta ---
	protected onboardingIncompleto(cliente: Cliente): boolean {
		return cliente.numFincas === 0 || cliente.numUsuarios === 0;
	}

	protected continuarAlta(cliente: Cliente, event: Event): void {
		event.stopPropagation();
		this.errorMsg.set(null);
		this._svc.getById(cliente.id).subscribe(detalle => {
			this.clienteCreado.set(cliente);
			this.fincasCreadas.set(detalle.fincas.map(f => ({ id: f.id, nombre: f.nombre, ubicacion: f.ubicacion ?? '', areaTotal: 0, costoTerreno: 0 })));
			this.usuarioCreadoNombre.set(null);
			this.step.set(cliente.numFincas === 0 ? 2 : 3);
			this.fincaForm.reset({ areaTotal: 0, costoTerreno: 0 });
			this.usuarioForm.reset();
			this.nivelAccesoWizard.set('total');
			this.panelOpen.set(true);
		});
	}

	// --- Feature 18: panel de salud del sistema ---
	protected readonly salud = signal<SaludSistema | null>(null);
	protected readonly loadingSalud = signal(false);

	ngOnInit(): void {
		this.load();
		this.cargarSalud();
		this.loadPlanes();
	}

	protected cargarSalud(): void {
		this.loadingSalud.set(true);
		this._svc.getSaludSistema().subscribe({
			next:  s => { this.salud.set(s); this.loadingSalud.set(false); },
			error: () => this.loadingSalud.set(false),
		});
	}

	protected tamanoBaseDatosMb(): string {
		const bytes = this.salud()?.tamanoBaseDatosBytes ?? 0;
		return (bytes / (1024 * 1024)).toFixed(2);
	}

	// --- Feature 19: exportar datos completos de un cliente ---
	protected exportarCliente(cliente: Cliente, event: Event): void {
		event.stopPropagation();
		this._svc.exportar(cliente.id).subscribe(data => {
			const contenido = JSON.stringify(data, null, 2);
			const blob = new Blob([contenido], { type: 'application/json;charset=utf-8;' });
			const url = URL.createObjectURL(blob);
			const link = document.createElement('a');
			link.href = url;
			link.download = `cliente-${cliente.razonSocial}-export.json`;
			link.click();
			URL.revokeObjectURL(url);
		});
	}

	protected load(): void {
		this.loading.set(true);
		this._svc.getAll().subscribe({
			next:  clientes => { this.clientes.set(clientes); this.loading.set(false); },
			error: ()       => this.loading.set(false),
		});
	}

	protected openWizard(): void {
		this.errorMsg.set(null);
		this.step.set(1);
		this.clienteCreado.set(null);
		this.fincasCreadas.set([]);
		this.usuarioCreadoNombre.set(null);
		this.clienteForm.reset({ planId: '' });
		this.fincaForm.reset({ areaTotal: 0, costoTerreno: 0 });
		this.usuarioForm.reset();
		this.nivelAccesoWizard.set('total');
		this.panelOpen.set(true);
	}

	protected closePanel(): void {
		this.panelOpen.set(false);
		this.load();
	}

	protected guardarCliente(): void {
		if (this.clienteForm.invalid || this.saving()) { return; }
		const raw = this.clienteForm.value;
		this.saving.set(true);
		this.errorMsg.set(null);
		this._svc.create({
			razonSocial: raw.razonSocial!,
			nit: raw.nit!,
			email: raw.email!,
			telefono: raw.telefono || null,
			fechaVencimientoContrato: raw.fechaVencimientoContrato || null,
			planId: raw.planId ? Number(raw.planId) : null,
		}).subscribe({
			next: cliente => {
				this.saving.set(false);
				this.clienteCreado.set(cliente);
				this.step.set(2);
			},
			error: err => { this.saving.set(false); this.errorMsg.set(err?.error?.error ?? 'No se pudo crear el cliente.'); },
		});
	}

	protected agregarFinca(): void {
		const cliente = this.clienteCreado();
		if (!cliente || this.fincaForm.invalid || this.saving()) { return; }
		const raw = this.fincaForm.value;
		this.saving.set(true);
		this.errorMsg.set(null);
		this._svc.createFinca(cliente.id, {
			nombre: raw.nombre!,
			ubicacion: raw.ubicacion || null,
			areaTotal: Number(raw.areaTotal),
			costoTerreno: Number(raw.costoTerreno),
		}).subscribe({
			next: finca => {
				this.saving.set(false);
				this.fincasCreadas.update(fincas => [...fincas, finca]);
				this.fincaForm.reset({ areaTotal: 0, costoTerreno: 0 });
			},
			error: err => { this.saving.set(false); this.errorMsg.set(err?.error?.error ?? 'No se pudo crear la finca.'); },
		});
	}

	protected irAPasoUsuario(): void {
		if (this.fincasCreadas().length === 0) { return; }
		this.step.set(3);
	}

	protected guardarUsuario(): void {
		const cliente = this.clienteCreado();
		const fincaIds = this.fincasCreadas().map(f => f.id);
		if (!cliente || this.usuarioForm.invalid || fincaIds.length === 0 || this.saving()) { return; }
		const raw = this.usuarioForm.value;
		const acceso = accesoDesdeNivel(this.nivelAccesoWizard());
		this.saving.set(true);
		this.errorMsg.set(null);
		this._svc.createUsuario(cliente.id, {
			nombre: raw.nombre!,
			documento: raw.documento!,
			telefono: raw.telefono!,
			email: raw.email || null,
			tipoPersona: 'Admin',
			nombreUsuario: raw.nombreUsuario!,
			contrasena: raw.contrasena!,
			accesos: fincaIds.map(fincaId => ({ fincaId, ...acceso })),
		}).subscribe({
			next: () => {
				this.saving.set(false);
				this.usuarioCreadoNombre.set(raw.nombreUsuario!);
				this.step.set(4);
			},
			error: err => { this.saving.set(false); this.errorMsg.set(err?.error?.error ?? 'No se pudo crear el usuario.'); },
		});
	}

	protected finalizarWizard(): void {
		this.closePanel();
	}

	protected verGestion(cliente: Cliente): void {
		this.clienteGestion.set(cliente);
		this.errorMsg.set(null);
		this.nuevaFincaGestionForm.reset({ areaTotal: 0, costoTerreno: 0 });
		this.nuevoUsuarioGestionForm.reset();
		this.nivelAccesoGestion.set('total');
		this.contratoForm.reset({
			fechaVencimientoContrato: cliente.fechaVencimientoContrato?.substring(0, 10) ?? '',
			activo: cliente.activo,
		});
		this.cargarUsuarios(cliente.id);
		this.cargarFincasCliente(cliente.id);
	}

	protected guardarContrato(): void {
		const cliente = this.clienteGestion();
		if (!cliente || this.saving()) { return; }
		const raw = this.contratoForm.value;
		this.saving.set(true);
		this.errorMsg.set(null);
		this._svc.actualizarContrato(cliente.id, {
			fechaVencimientoContrato: raw.fechaVencimientoContrato || null,
			activo: !!raw.activo,
		}).subscribe({
			next: actualizado => {
				this.saving.set(false);
				this.clienteGestion.set(actualizado);
				this.load();
			},
			error: err => { this.saving.set(false); this.errorMsg.set(err?.error?.error ?? 'No se pudo actualizar el contrato.'); },
		});
	}

	protected estadoContrato(cliente: Cliente): { label: string; className: string } {
		if (!cliente.activo) {
			return { label: 'Inactivo', className: 'badge badge-estado-inactivo' };
		}
		if (!cliente.fechaVencimientoContrato) {
			return { label: 'Activo', className: 'badge badge-estado-activo' };
		}
		const hoy = new Date();
		const vencimiento = new Date(cliente.fechaVencimientoContrato);
		const diasRestantes = Math.ceil((vencimiento.getTime() - hoy.getTime()) / (1000 * 60 * 60 * 24));
		if (diasRestantes < 0) {
			return { label: 'Contrato vencido', className: 'badge badge-estado-vencido' };
		}
		if (diasRestantes <= 30) {
			return { label: `Vence en ${diasRestantes} días`, className: 'badge badge-estado-por-vencer' };
		}
		return { label: 'Activo', className: 'badge badge-estado-activo' };
	}

	protected onCambiarPlanCliente(planIdRaw: string): void {
		const cliente = this.clienteGestion();
		if (!cliente) { return; }
		const planId = planIdRaw ? Number(planIdRaw) : null;
		this._svc.asignarPlan(cliente.id, planId).subscribe({
			next: actualizado => { this.clienteGestion.set(actualizado); this.load(); },
			error: err => this.errorMsg.set(err?.error?.error ?? 'No se pudo actualizar el plan.'),
		});
	}

	protected cerrarGestion(): void {
		this.clienteGestion.set(null);
		this.usuariosDelCliente.set([]);
		this.fincasCliente.set([]);
	}

	protected crearUsuarioGestion(): void {
		const cliente = this.clienteGestion();
		const fincaIds = this.fincasCliente().map(f => f.id);
		if (!cliente || this.nuevoUsuarioGestionForm.invalid || fincaIds.length === 0 || this.saving()) { return; }
		const raw = this.nuevoUsuarioGestionForm.value;
		const acceso = accesoDesdeNivel(this.nivelAccesoGestion());
		this.saving.set(true);
		this.errorMsg.set(null);
		this._svc.createUsuario(cliente.id, {
			nombre: raw.nombre!,
			documento: raw.documento!,
			telefono: raw.telefono!,
			email: raw.email || null,
			tipoPersona: 'Admin',
			nombreUsuario: raw.nombreUsuario!,
			contrasena: raw.contrasena!,
			accesos: fincaIds.map(fincaId => ({ fincaId, ...acceso })),
		}).subscribe({
			next: () => {
				this.saving.set(false);
				this.nuevoUsuarioGestionForm.reset();
				this.cargarUsuarios(cliente.id);
			},
			error: err => { this.saving.set(false); this.errorMsg.set(err?.error?.error ?? 'No se pudo crear el usuario.'); },
		});
	}

	private cargarFincasCliente(clienteId: number): void {
		this._svc.getById(clienteId).subscribe(detalle => this.fincasCliente.set(detalle.fincas));
	}

	protected agregarFincaAlCliente(): void {
		const cliente = this.clienteGestion();
		if (!cliente || this.nuevaFincaGestionForm.invalid || this.saving()) { return; }
		const raw = this.nuevaFincaGestionForm.value;
		this.saving.set(true);
		this.errorMsg.set(null);
		this._svc.createFinca(cliente.id, {
			nombre: raw.nombre!,
			ubicacion: raw.ubicacion || null,
			areaTotal: Number(raw.areaTotal),
			costoTerreno: Number(raw.costoTerreno),
		}).subscribe({
			next: () => {
				this.saving.set(false);
				this.nuevaFincaGestionForm.reset({ areaTotal: 0, costoTerreno: 0 });
				this.cargarFincasCliente(cliente.id);
			},
			error: err => { this.saving.set(false); this.errorMsg.set(err?.error?.error ?? 'No se pudo crear la finca.'); },
		});
	}

	private cargarUsuarios(clienteId: number): void {
		this.cargandoUsuarios.set(true);
		this._svc.getUsuarios(clienteId).subscribe({
			next:  usuarios => { this.usuariosDelCliente.set(usuarios); this.cargandoUsuarios.set(false); },
			error: ()       => this.cargandoUsuarios.set(false),
		});
	}

	protected onCambiarNivelAcceso(personaId: number, fincaId: number, nivel: NivelAcceso): void {
		const cliente = this.clienteGestion();
		if (!cliente) { return; }
		const { soloLectura, puedeEliminar } = accesoDesdeNivel(nivel);
		this._svc.setPermisoFinca(personaId, fincaId, soloLectura, puedeEliminar).subscribe(() => this.cargarUsuarios(cliente.id));
	}

	protected toggleUsuarioActivo(persona: PersonaDelCliente): void {
		const cliente = this.clienteGestion();
		if (!cliente) { return; }
		this._svc.setUsuarioActivo(persona.id, !persona.activo).subscribe(() => this.cargarUsuarios(cliente.id));
	}
}
