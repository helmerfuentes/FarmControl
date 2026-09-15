export type TipoPersona = 'Socio' | 'Jornalero' | 'Comprador' | 'Otro' | 'Admin' | 'Contador';
export type EstadoProceso = 'Activo' | 'Cosechado' | 'Cerrado';
export type TipoMovimientoInsumo = 'Entrada' | 'Salida';
export type TipoCompra = 'Insumo' | 'Maquinaria' | 'Otro';

export interface TipoInsumo {
	id: number;
	nombre: string;
	descripcion: string;
}

export interface ClasificacionProducto {
	id: number;
	nombre: string;
	unidadMedida: string;
	pesoUnidadKg: number;
}

export interface Producto {
	id: number;
	nombre: string;
	clasificaciones: ClasificacionProducto[];
}

export interface Persona {
	id: number;
	nombre: string;
	documento: string;
	telefono: string;
	email: string;
	tipoPersona: TipoPersona;
	valorDia: number;
}

export interface Finca {
	id: number;
	nombre: string;
	ubicacion: string;
	areaTotal: number;
	costoTerreno: number;
}

export interface Cliente {
	id: number;
	razonSocial: string;
	nit: string;
	email: string;
	telefono: string | null;
	activo: boolean;
	fechaAlta: string;
	fechaVencimientoContrato: string | null;
	planId: number | null;
	planNombre: string | null;
	maxFincas: number | null;
	maxUsuarios: number | null;
	numFincas: number;
	numUsuarios: number;
}

export interface Plan {
	id: number;
	nombre: string;
	descripcion: string | null;
	maxFincas: number;
	maxUsuarios: number;
}

export interface Parcela {
	id: number;
	fincaId: number;
	nombre: string;
	area: number;
}

export interface ProcesoCultivo {
	id: number;
	parcelaId: number;
	productoId: number;
	socioId?: number | null;
	costoInicial: number;
	fechaInicio: string;
	fechaEstimadaCosecha?: string | null;
	fechaCierre?: string | null;
	estado: EstadoProceso;
	parcelaNombre?: string;
	productoNombre?: string;
	socioNombre?: string;
}

export interface Insumo {
	id: number;
	tipoInsumoId: number;
	nombre: string;
	marca: string;
	descripcion: string;
	precioUnitario: number;
	unidadMedida: string;
	stockMinimo: number | null;
	fechaVencimiento: string | null;
	tipoInsumoNombre?: string;
}

export interface MovimientoInsumo {
	id: number;
	insumoId: number;
	parcelaId: number;
	cantidad: number;
	tipoMovimiento: TipoMovimientoInsumo;
	fecha: string;
	observacion: string;
	precioUnitario?: number | null;
	insumoNombre?: string;
	parcelaNombre?: string;
}

export interface Actividad {
	id: number;
	parcelaId: number;
	tipoActividad: string;
	fechaInicio: string;
	fechaFin?: string | null;
	personaACargoId?: number | null;
	personaACargoNombre?: string | null;
	descripcion: string;
	parcelaNombre?: string;
	fincaNombre?: string;
	valorDiaUsado: number;
	costoCalculado: number;
	procesoCultivoId?: number | null;
	confirmada?: boolean;
	fechaConfirmacion?: string | null;
	confirmadaPor?: string | null;
}

export interface RegistroManoObra {
	id: number;
	actividadId: number;
	jornaleroId: number;
	socioId: number;
	valorHora: number;
	numHoras: number;
	horaInicio: string;
	horaSalida: string;
	jornaleroNombre?: string;
	socioNombre?: string;
}

export interface Compra {
	id: number;
	fincaId: number;
	socioId: number;
	parcelaId?: number | null;
	parcelaNombre?: string | null;
	procesoCultivoId?: number | null;
	descripcion: string;
	valor: number;
	fecha: string;
	tipoCompra: TipoCompra;
	adjuntoUrl: string;
	fincaNombre?: string;
	socioNombre?: string;
	proveedor?: string | null;
	totalPagado?: number;
	saldoPendiente?: number;
}

export interface PagoCompra {
	id: number;
	compraId: number;
	monto: number;
	fecha: string;
	observacion: string | null;
}

export interface DetalleVenta {
	id: number;
	ventaId: number;
	clasificacion: string;
	cantidad: number;
	unidadMedida: string;
	precioUnitario: number;
	subtotal: number;
}

export interface Venta {
	id: number;
	parcelaId: number;
	compradorId: number;
	fecha: string;
	valorTransporte: number;
	total: number;
	detalles: DetalleVenta[];
	parcelaNombre?: string;
	fincaNombre?: string;
	compradorNombre?: string;
	procesoCultivoId?: number | null;
	totalPagado?: number;
	saldoPendiente?: number;
}

export interface TareaRecurrente {
	id: number;
	parcelaId: number;
	parcelaNombre?: string;
	descripcion: string;
	frecuenciaDias: number;
	proximaFecha: string;
	ultimaEjecucion: string | null;
	activa: boolean;
}

export interface PagoVenta {
	id: number;
	ventaId: number;
	monto: number;
	fecha: string;
	observacion: string | null;
}

export interface CompradorFrecuente {
	compradorId: number;
	compradorNombre: string;
	numVentas: number;
	volumenTotal: number;
	ultimaVenta: string;
}

export interface Asistencia {
	id: number;
	personaId: number;
	personaNombre?: string;
	fincaId: number;
	fincaNombre?: string;
	fecha: string;
	horaEntrada: string | null;
	horaSalida: string | null;
	observacion: string | null;
}

export interface RegistroPendienteLiquidacion {
	registroId: number;
	fechaActividad: string;
	numHoras: number;
	valorHora: number;
	subtotal: number;
}

export interface PendientesLiquidacion {
	registros: RegistroPendienteLiquidacion[];
	totalHoras: number;
	totalPagar: number;
}

export interface LiquidacionNomina {
	id: number;
	jornaleroId: number;
	jornaleroNombre?: string;
	fechaInicio: string;
	fechaFin: string;
	totalHoras: number;
	totalPagar: number;
	fechaLiquidacion: string;
	observacion: string | null;
}

export type TipoEntidadComentario = 'Actividad' | 'Compra' | 'Venta';

export interface Comentario {
	id: number;
	tipoEntidad: TipoEntidadComentario;
	entidadId: number;
	autorPersonaId: number;
	autorNombre: string;
	texto: string;
	fecha: string;
}

export interface AnalisisSuelo {
	id: number;
	parcelaId: number;
	fecha: string;
	ph: number | null;
	materiaOrganica: number | null;
	nitrogeno: number | null;
	fosforo: number | null;
	potasio: number | null;
	observacion: string | null;
}
