/*
  Comportamiento de la interfaz: alternancia de tema y de moneda.

  La conversión de montos no se calcula aquí. El servidor emite ambas representaciones
  (colones y dólares) con el mismo redondeo que usa la API, y este script solo decide cuál
  se muestra. Así el valor oficial en colones nunca se recalcula en el navegador.
*/
(function inicializar() {
    'use strict';

    var CLAVE_TEMA = 'licitaciones.tema';
    var CLAVE_MONEDA = 'licitaciones.moneda';

    function leer(clave, porOmision) {
        try {
            return window.localStorage.getItem(clave) || porOmision;
        } catch (error) {
            return porOmision;
        }
    }

    function guardar(clave, valor) {
        try {
            window.localStorage.setItem(clave, valor);
        } catch (error) {
            // La preferencia simplemente no persiste; la interfaz sigue funcionando.
        }
    }

    // --- Tema claro / oscuro -------------------------------------------------
    var botonTema = document.getElementById('botonTema');
    var etiquetaTema = document.getElementById('etiquetaTema');

    function pintarTema(tema) {
        document.documentElement.setAttribute('data-bs-theme', tema);

        if (etiquetaTema) {
            etiquetaTema.textContent = tema === 'dark' ? 'Modo claro' : 'Modo oscuro';
        }

        if (botonTema) {
            botonTema.setAttribute('aria-pressed', tema === 'dark' ? 'true' : 'false');
        }
    }

    pintarTema(document.documentElement.getAttribute('data-bs-theme') || 'light');

    if (botonTema) {
        botonTema.addEventListener('click', function alternarTema() {
            var actual = document.documentElement.getAttribute('data-bs-theme');
            var nuevo = actual === 'dark' ? 'light' : 'dark';

            guardar(CLAVE_TEMA, nuevo);
            pintarTema(nuevo);
        });
    }

    // --- Moneda CRC / USD ----------------------------------------------------
    var botonMoneda = document.getElementById('botonMoneda');
    var etiquetaMoneda = document.getElementById('etiquetaMoneda');

    function pintarMoneda(moneda) {
        var enColones = moneda !== 'USD';

        document.querySelectorAll('.monto-crc').forEach(function alternarColones(elemento) {
            elemento.classList.toggle('d-none', !enColones);
        });

        document.querySelectorAll('.monto-usd').forEach(function alternarDolares(elemento) {
            elemento.classList.toggle('d-none', enColones);
        });

        if (etiquetaMoneda) {
            etiquetaMoneda.textContent = enColones ? 'USD' : 'CRC';
        }

        if (botonMoneda) {
            botonMoneda.setAttribute('aria-pressed', enColones ? 'false' : 'true');
        }

        document.body.setAttribute('data-moneda', enColones ? 'CRC' : 'USD');
    }

    pintarMoneda(leer(CLAVE_MONEDA, 'CRC'));

    if (botonMoneda) {
        botonMoneda.addEventListener('click', function alternarMoneda() {
            var actual = document.body.getAttribute('data-moneda') === 'USD' ? 'USD' : 'CRC';
            var nueva = actual === 'USD' ? 'CRC' : 'USD';

            guardar(CLAVE_MONEDA, nueva);
            pintarMoneda(nueva);
        });
    }

    // --- Confirmación de eliminación ----------------------------------------
    document.querySelectorAll('form[data-confirmar]').forEach(function enlazar(formulario) {
        formulario.addEventListener('submit', function confirmar(evento) {
            if (!window.confirm(formulario.getAttribute('data-confirmar'))) {
                evento.preventDefault();
            }
        });
    });
})();
