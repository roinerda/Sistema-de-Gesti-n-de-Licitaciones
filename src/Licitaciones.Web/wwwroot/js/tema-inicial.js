/*
  Aplica el tema guardado antes de que el navegador pinte la página.
  Se carga de forma síncrona en el <head> justamente para evitar el destello de tema claro
  que se vería si el cambio ocurriera después de renderizar el cuerpo.
*/
(function aplicarTemaInicial() {
    'use strict';

    var CLAVE_TEMA = 'licitaciones.tema';
    var guardado = null;

    try {
        guardado = window.localStorage.getItem(CLAVE_TEMA);
    } catch (error) {
        // Si el almacenamiento local no está disponible se usa la preferencia del sistema.
        guardado = null;
    }

    var prefiereOscuro = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    var tema = guardado || (prefiereOscuro ? 'dark' : 'light');

    document.documentElement.setAttribute('data-bs-theme', tema);
})();
