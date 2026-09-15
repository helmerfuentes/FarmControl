import { Page, Locator, expect } from '@playwright/test';

export async function login(page: Page, usuario: string, contrasena: string): Promise<void> {
	await page.goto('/');
	await page.getByRole('button', { name: 'Ingresar', exact: true }).first().click();
	await page.locator('#m-user').fill(usuario);
	await page.locator('#m-pass').fill(contrasena);
	await page.locator('.lp-modal-btn').click();
	await expect(page).toHaveURL(/\/app\//, { timeout: 10000 });
}

export async function logout(page: Page): Promise<void> {
	await page.locator('.btn-logout').click();
	await expect(page).toHaveURL('/');
}

export function nombreUnico(prefijo: string): string {
	return `${prefijo}${Date.now()}`;
}

/**
 * `fc-drawer` mantiene su contenido siempre en el DOM y solo alterna una clase CSS
 * para mostrarlo/ocultarlo (ver drawer.ts) — varias páginas tienen más de un drawer
 * con campos de igual `formcontrolname` (ej. "nombre") montados a la vez. Todo campo
 * de formulario dentro de un drawer debe ubicarse a través de este helper, que acota
 * la búsqueda al `.drawer.open` actualmente visible en vez de a toda la página.
 */
export function drawerAbierto(page: Page): Locator {
	return page.locator('.drawer.open');
}

export function campo(page: Page, formcontrolname: string): Locator {
	return drawerAbierto(page).locator(`[formcontrolname="${formcontrolname}"]`);
}
