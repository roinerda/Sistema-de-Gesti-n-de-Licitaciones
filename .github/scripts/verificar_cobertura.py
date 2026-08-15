#!/usr/bin/env python3
"""Verifica los umbrales de cobertura que exige el enunciado (seccion 12.5).

    - Licitaciones.Domain y Licitaciones.Application: 80 % o mas de lineas.
    - Cobertura total de la solucion: 70 % o mas de lineas.

Se ejecuta sobre el informe Cobertura combinado que produce ReportGenerator.
Devuelve codigo 1 si algun umbral no se cumple, de modo que la integracion se detiene.
"""

import sys
import xml.etree.ElementTree as ET

UMBRAL_NUCLEO = 0.80
UMBRAL_TOTAL = 0.70
ENSAMBLADOS_NUCLEO = ("Licitaciones.Domain", "Licitaciones.Application")


def porcentaje(valor: float) -> str:
    return f"{valor * 100:.2f} %"


def main(ruta_informe: str) -> int:
    arbol = ET.parse(ruta_informe)
    raiz = arbol.getroot()

    total = float(raiz.get("line-rate", 0.0))
    paquetes = {
        paquete.get("name", ""): float(paquete.get("line-rate", 0.0))
        for paquete in raiz.iter("package")
    }

    print("Cobertura de lineas por ensamblado:")
    for nombre in sorted(paquetes):
        print(f"  {nombre:<34} {porcentaje(paquetes[nombre])}")
    print(f"  {'TOTAL':<34} {porcentaje(total)}")
    print()

    incumplimientos = []

    for nombre in ENSAMBLADOS_NUCLEO:
        if nombre not in paquetes:
            incumplimientos.append(f"No hay datos de cobertura para {nombre}.")
            continue
        if paquetes[nombre] < UMBRAL_NUCLEO:
            incumplimientos.append(
                f"{nombre} cubre {porcentaje(paquetes[nombre])} "
                f"y el minimo exigido es {porcentaje(UMBRAL_NUCLEO)}."
            )

    if total < UMBRAL_TOTAL:
        incumplimientos.append(
            f"La cobertura total es {porcentaje(total)} "
            f"y el minimo exigido es {porcentaje(UMBRAL_TOTAL)}."
        )

    if incumplimientos:
        for detalle in incumplimientos:
            print(f"::error::{detalle}")
        return 1

    print("Se cumplen todos los umbrales de cobertura.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Uso: verificar_cobertura.py <ruta-del-informe-cobertura>")
        sys.exit(2)

    sys.exit(main(sys.argv[1]))
