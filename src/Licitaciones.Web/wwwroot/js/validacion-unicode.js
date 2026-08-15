/*
  Corrección de la validación de expresiones regulares en el navegador.

  El patrón del nombre de proveedor es ^[\p{L}\p{N} .,\(\)]+$, con clases Unicode, tal
  como lo exige el enunciado. .NET las entiende de forma nativa, pero una expresión
  regular de JavaScript solo reconoce \p{...} cuando se compila con la marca «u». Sin
  ella, \p se interpreta como una «p» literal y la clase pasa a admitir apenas los
  caracteres p, L, N, llaves, espacio, punto, coma y paréntesis: cualquier nombre real
  quedaba rechazado en el cliente aunque el servidor lo aceptara sin problema.

  jquery-validation-unobtrusive compila el patrón sin esa marca, así que aquí se
  reemplaza su método por uno que sí la usa. Se conserva la semántica original de
  coincidencia total; lo único que cambia es la marca de compilación.
*/
(function corregirValidacionDeExpresionesRegulares() {
    'use strict';

    if (!window.jQuery || !window.jQuery.validator || !window.jQuery.validator.methods) {
        return;
    }

    function compilar(patron) {
        try {
            return new RegExp(patron, 'u');
        } catch (errorUnicode) {
            try {
                // Patrón no válido en modo Unicode: se conserva el comportamiento anterior.
                return new RegExp(patron);
            } catch (errorSimple) {
                // Patrón que el navegador no entiende. El servidor lo sigue validando,
                // de modo que no se pierde la comprobación, solo el aviso inmediato.
                return null;
            }
        }
    }

    window.jQuery.validator.methods.regex = function validarConUnicode(valor, elemento, patron) {
        if (this.optional(elemento)) {
            return true;
        }

        var expresion = compilar(patron);

        if (expresion === null) {
            return true;
        }

        // Coincidencia total, igual que el método original: el patrón debe cubrir todo el valor.
        var coincidencia = expresion.exec(valor);
        return coincidencia !== null
            && coincidencia.index === 0
            && coincidencia[0].length === valor.length;
    };
})();
