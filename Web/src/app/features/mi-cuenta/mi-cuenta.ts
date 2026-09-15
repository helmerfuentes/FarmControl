import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';

function coincideContrasenaValidator(control: AbstractControl): ValidationErrors | null {
	const nueva = control.get('contrasenaNueva')?.value;
	const confirmacion = control.get('confirmarContrasenaNueva')?.value;
	return nueva && confirmacion && nueva !== confirmacion ? { noCoincide: true } : null;
}

@Component({
	selector: 'app-mi-cuenta',
	imports: [ReactiveFormsModule],
	templateUrl: './mi-cuenta.html',
	styleUrl: './mi-cuenta.scss',
})
export class MiCuentaComponent {
	private readonly _authService = inject(AuthService);
	private readonly _fb = inject(FormBuilder);

	protected readonly usuario = this._authService.usuario;
	protected readonly saving = signal(false);
	protected readonly errorMsg = signal<string | null>(null);
	protected readonly successMsg = signal<string | null>(null);

	protected readonly form = this._fb.group({
		contrasenaActual: ['', Validators.required],
		contrasenaNueva: ['', [Validators.required, Validators.minLength(6)]],
		confirmarContrasenaNueva: ['', Validators.required],
	}, { validators: coincideContrasenaValidator });

	protected guardar(): void {
		if (this.form.invalid || this.saving()) { return; }
		this.errorMsg.set(null);
		this.successMsg.set(null);
		this.saving.set(true);
		const raw = this.form.value;
		this._authService.cambiarContrasena(raw.contrasenaActual!, raw.contrasenaNueva!).subscribe({
			next: () => {
				this.saving.set(false);
				this.successMsg.set('Contraseña actualizada correctamente.');
				this.form.reset();
			},
			error: err => {
				this.saving.set(false);
				this.errorMsg.set(err?.error?.error ?? 'No se pudo cambiar la contraseña.');
			},
		});
	}
}
