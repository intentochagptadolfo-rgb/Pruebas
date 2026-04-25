/* ==========================================================================
   MENU DRAWER
   - Toggle por botón hamburguesa
   - Cierra con: overlay, Escape, clic en link (solo mobile), cambio a desktop
   - Focus trap cuando está abierto
   - Bloquea scroll del body (no-scroll en <html>)
   - Aplica inert al <main> para que el lector de pantalla no lea detrás
   ========================================================================== */

(() => {
  const toggle  = document.querySelector('.nav-toggle');
  const menu    = document.querySelector('.menu');
  const overlay = document.querySelector('.menu-overlay');
  const main    = document.querySelector('main');

  if (!toggle || !menu || !overlay) return;

  const mqDesktop = matchMedia('(min-width: 960px)');
  const isOpen = () => menu.classList.contains('is-open');

  const focusables = () => menu.querySelectorAll(
    'a, button, [tabindex]:not([tabindex="-1"])'
  );

  const open = () => {
    toggle.classList.add('is-active');
    toggle.setAttribute('aria-expanded', 'true');
    menu.classList.add('is-open');
    overlay.classList.add('is-open');
    document.documentElement.classList.add('no-scroll');
    if (main) main.setAttribute('inert', '');
    focusables()[0]?.focus();
  };

  const close = () => {
    toggle.classList.remove('is-active');
    toggle.setAttribute('aria-expanded', 'false');
    menu.classList.remove('is-open');
    overlay.classList.remove('is-open');
    document.documentElement.classList.remove('no-scroll');
    if (main) main.removeAttribute('inert');
    toggle.focus();
  };

  toggle.addEventListener('click', () => isOpen() ? close() : open());
  overlay.addEventListener('click', close);

  document.addEventListener('keydown', e => {
    if (!isOpen()) return;

    if (e.key === 'Escape') { close(); return; }

    if (e.key === 'Tab') {
      const list = [...focusables()];
      if (!list.length) return;
      const first = list[0];
      const last  = list[list.length - 1];
      if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
      else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }
  });

  // Cierra al hacer clic en un link del menú (solo mobile)
  menu.addEventListener('click', e => {
    if (e.target.tagName === 'A' && !mqDesktop.matches) close();
  });

  // Si el usuario cambia a desktop con el menú abierto, limpiar estado
  mqDesktop.addEventListener('change', e => {
    if (e.matches && isOpen()) close();
  });
})();
