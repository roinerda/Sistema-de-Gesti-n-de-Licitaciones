#!/usr/bin/env python3
"""Convierte los resultados .trx en anotaciones de GitHub Actions.

Los registros de una ejecucion solo se pueden descargar con autenticacion, pero las
anotaciones de las comprobaciones son publicas. Emitir aqui cada prueba fallida, con su
mensaje y su traza recortada, permite diagnosticar una ejecucion roja sin credenciales.

Uso:
    resumir_fallos.py <carpeta-con-archivos-trx>
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET

ESPACIO = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
LIMITE_MENSAJE = 1200
LIMITE_TRAZA = 1200


def texto_de(nodo, etiqueta):
    hijo = nodo.find(f"{ESPACIO}{etiqueta}")
    return (hijo.text or "").strip() if hijo is not None else ""


def aplanar(texto, limite):
    """Las anotaciones son de una sola linea: se sustituyen los saltos por barras."""
    plano = " / ".join(linea.strip() for linea in texto.splitlines() if linea.strip())
    return plano[:limite] + (" [...]" if len(plano) > limite else "")


def resumir(ruta):
    fallidas = []
    raiz = ET.parse(ruta).getroot()

    for resultado in raiz.iter(f"{ESPACIO}UnitTestResult"):
        if resultado.get("outcome") != "Failed":
            continue

        nombre = resultado.get("testName", "(sin nombre)")
        salida = resultado.find(f"{ESPACIO}Output")
        mensaje = traza = ""

        if salida is not None:
            info = salida.find(f"{ESPACIO}ErrorInfo")
            if info is not None:
                mensaje = texto_de(info, "Message")
                traza = texto_de(info, "StackTrace")

        fallidas.append((nombre, mensaje, traza))

    return fallidas


def main(carpeta):
    archivos = glob.glob(os.path.join(carpeta, "**", "*.trx"), recursive=True)

    if not archivos:
        print(f"No se encontraron archivos .trx en {carpeta}.")
        return 0

    total = 0

    for archivo in sorted(archivos):
        try:
            fallidas = resumir(archivo)
        except ET.ParseError as error:
            print(f"::warning::No se pudo leer {archivo}: {error}")
            continue

        if not fallidas:
            continue

        print(f"::group::Fallos en {os.path.basename(archivo)} ({len(fallidas)})")

        for nombre, mensaje, traza in fallidas:
            total += 1
            detalle = aplanar(mensaje, LIMITE_MENSAJE)
            origen = aplanar(traza, LIMITE_TRAZA)
            print(f"::error title={nombre}::{detalle} || {origen}")

        print("::endgroup::")

    print(f"Total de pruebas fallidas: {total}")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Uso: resumir_fallos.py <carpeta-con-archivos-trx>")
        sys.exit(2)

    sys.exit(main(sys.argv[1]))
