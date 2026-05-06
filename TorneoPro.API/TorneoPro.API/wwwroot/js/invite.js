/**
 * Manejo de invitaciones y deep links
 */

let appOpened = false;

/**
 * Abre la aplicación mediante deep link
 */
function openApp() {
    appOpened = false;
    
    const btnText = document.getElementById('btnText');
    const btnLoader = document.getElementById('btnLoader');
    const btn = document.getElementById('openAppBtn');
    
    if (btnText) btnText.classList.add('hidden');
    if (btnLoader) btnLoader.classList.remove('hidden');
    if (btn) btn.disabled = true;
    
    window.location.href = deepLink;
    
    // Si después de 2.5 segundos no se abrió la app, mostrar fallback
    setTimeout(() => {
        if (!appOpened) {
            if (btnText) btnText.classList.remove('hidden');
            if (btnLoader) btnLoader.classList.add('hidden');
            if (btn) btn.disabled = false;
            
            const fallbackBtn = document.getElementById('fallbackLink');
            if (fallbackBtn) fallbackBtn.style.display = 'block';
        }
    }, 2500);
}

/**
 * Detecta si la aplicación se abrió (la ventana pierde foco)
 */
window.addEventListener('blur', () => {
    appOpened = true;
});

/**
 * Configurar el botón de fallback
 */
const fallbackLink = document.getElementById('fallbackLink');
if (fallbackLink) {
    fallbackLink.addEventListener('click', (e) => {
        e.preventDefault();
        window.location.href = fallbackUrl;
    });
}

/**
 * Detectar si es dispositivo móvil
 */
const isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);

// Solo intentar abrir la app automáticamente en móviles
if (isMobile && typeof deepLink !== 'undefined') {
    openApp();
}