import { test, expect } from '@playwright/test';
import { login, logout, nombreUnico, drawerAbierto, campo } from './helpers';

/**
 * Cubre el flujo de onboarding vigente: el SuperAdmin solo crea el Cliente + su primer
 * usuario Admin (sin fincas); el Admin del cliente inicia sesión y registra su propia
 * finca y parcela (autoservicio). Ver conversación: "el super admin, solo crea el
 * cliente, el usuario admin de la finca crea sus finca".
 */
test('SuperAdmin crea cliente + admin; el admin crea su propia finca y parcela', async ({ page }) => {
	const sufijo = nombreUnico('');
	const razonSocial = `E2E Finca Test ${sufijo}`;
	const nombreUsuario = `e2eadmin${sufijo}`;
	const contrasena = 'e2epass123';

	// --- SuperAdmin: crear Cliente + primer usuario Admin ---
	await login(page, 'admin', 'admin123');
	await expect(page).toHaveURL(/\/app\/clientes/);

	await page.getByRole('button', { name: 'Nuevo cliente' }).click();
	await campo(page, 'razonSocial').fill(razonSocial);
	await campo(page, 'nit').fill(`900${sufijo}`);
	await campo(page, 'email').fill(`${nombreUsuario}@e2e.test`);
	await drawerAbierto(page).getByRole('button', { name: 'Continuar' }).click();

	// Paso 2: usuario administrador inicial (sin fincas — se crean en autoservicio)
	await expect(page.getByText('Crea el usuario administrador de')).toBeVisible();
	await campo(page, 'nombre').fill('Admin E2E');
	await campo(page, 'documento').fill(`doc${sufijo}`);
	await campo(page, 'telefono').fill('3000000000');
	await campo(page, 'nombreUsuario').fill(nombreUsuario);
	await campo(page, 'contrasena').fill(contrasena);
	await drawerAbierto(page).getByRole('button', { name: 'Finalizar' }).click();

	// Paso 3: resumen
	await expect(page.getByText('Alta completada')).toBeVisible();
	// El botón "X" del encabezado del drawer también tiene aria-label "Cerrar" — acotar a .form-actions
	await drawerAbierto(page).locator('.form-actions').getByRole('button', { name: 'Cerrar' }).click();
	await expect(page.getByText(razonSocial)).toBeVisible();

	await logout(page);

	// --- Admin del cliente: crear su propia finca ---
	await login(page, nombreUsuario, contrasena);
	await page.goto('/app/fincas');
	await expect(page.getByText('No hay fincas registradas.')).toBeVisible();

	await page.getByRole('button', { name: 'Nueva finca' }).click();
	await campo(page, 'nombre').fill('Finca E2E');
	await campo(page, 'ubicacion').fill('Vereda E2E');
	await campo(page, 'areaTotal').fill('12.5');
	await campo(page, 'costoTerreno').fill('8000000');
	await drawerAbierto(page).getByRole('button', { name: 'Guardar' }).click();

	// La finca debe aparecer SIN recargar la página (regresión: bug de token/lista desactualizada)
	await expect(page.locator('.finca-item-name', { hasText: 'Finca E2E' })).toBeVisible({ timeout: 10000 });

	// --- Crear una parcela dentro de la finca recién creada ---
	await page.locator('.finca-item', { hasText: 'Finca E2E' }).click();
	await page.getByRole('button', { name: 'Nueva parcela' }).click();
	await campo(page, 'nombre').fill('Parcela 1');
	await campo(page, 'area').fill('3');
	await drawerAbierto(page).getByRole('button', { name: 'Guardar' }).click();
	await expect(page.getByText('Parcela 1')).toBeVisible();

	// --- Navegación básica sin errores ---
	await page.goto('/app/inicio');
	await expect(page.locator('.dashboard-grid, .dash-card').first()).toBeVisible();

	await page.goto('/app/reportes');
	await expect(page.locator('body')).not.toContainText('Cannot read properties');

	await page.goto('/app/mi-cuenta');
	await expect(page.locator('body')).not.toContainText('Cannot read properties');
});
