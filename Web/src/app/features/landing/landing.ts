import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { FormGroup, FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PlanesService } from '../../core/api/planes.service';
import { Plan } from '../../core/models';

@Component({
	selector: 'app-landing',
	standalone: true,
	imports: [ReactiveFormsModule],
	templateUrl: './landing.html',
	styleUrl: './landing.scss',
})
export class LandingComponent implements OnInit, OnDestroy {
	private readonly _authSvc  = inject(AuthService);
	private readonly _router   = inject(Router);
	private readonly _planesSvc = inject(PlanesService);

	protected readonly showModal    = signal(false);
	protected readonly isLoading    = signal(false);
	protected readonly errorMsg     = signal('');
	protected readonly contactSent  = signal(false);
	protected readonly mobileMenuOpen = signal(false);

	protected readonly planes        = signal<Plan[]>([]);
	protected readonly loadingPlanes = signal(false);

	protected readonly loginForm = new FormGroup({
		usuario:    new FormControl('', { nonNullable: true, validators: [Validators.required] }),
		contrasena: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
	});

	protected readonly contactForm = new FormGroup({
		nombre:  new FormControl('', { nonNullable: true, validators: [Validators.required] }),
		email:   new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
		mensaje: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
	});

	private readonly _scrollFn = () => {
		const nav = document.getElementById('fc-nav');
		if (nav) { nav.classList.toggle('scrolled', window.scrollY > 50); }
	};

	ngOnInit(): void {
		window.addEventListener('scroll', this._scrollFn, { passive: true });
		this.loadPlanes();
	}

	ngOnDestroy(): void {
		window.removeEventListener('scroll', this._scrollFn);
	}

	private loadPlanes(): void {
		this.loadingPlanes.set(true);
		this._planesSvc.getPublicos().subscribe({
			next:  planes => { this.planes.set(planes); this.loadingPlanes.set(false); },
			error: ()     => this.loadingPlanes.set(false),
		});
	}

	protected limiteTexto(valor: number): string {
		return valor === -1 ? 'Sin límite' : String(valor);
	}

	protected toggleMobileMenu(): void {
		this.mobileMenuOpen.update(v => !v);
	}

	protected closeMobileMenu(): void {
		this.mobileMenuOpen.set(false);
	}

	protected openModal(): void {
		this.showModal.set(true);
		this.mobileMenuOpen.set(false);
		this.loginForm.reset();
		this.errorMsg.set('');
	}

	protected closeModal(): void {
		this.showModal.set(false);
	}

	protected onLogin(): void {
		if (this.loginForm.invalid || this.isLoading()) { return; }
		this.isLoading.set(true);
		this.errorMsg.set('');
		const { usuario, contrasena } = this.loginForm.getRawValue();
		this._authSvc.login(usuario, contrasena).subscribe({
			next: (res) => {
				this._authSvc.setSession(res.token, res.rol, usuario);
				const route = res.rol === 'Socio' ? '/app/mis-cultivos'
					: res.rol === 'SuperAdmin' ? '/app/clientes'
					: '/app/inicio';
				this._router.navigate([route]);
			},
			error: () => {
				this.errorMsg.set('Usuario o contraseña incorrectos.');
				this.isLoading.set(false);
			},
		});
	}

	protected onContact(): void {
		if (this.contactForm.invalid) { return; }
		this.contactSent.set(true);
		this.contactForm.reset();
	}
}
