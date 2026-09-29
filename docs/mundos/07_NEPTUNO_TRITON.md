# Mundo 7 — NEPTUNO → TRITÓN · "Saltar sobre géiseres"

> Neptuno es gas y tiene los vientos más rápidos del Sistema Solar. Pero su luna **Tritón** tiene suelo, y **géiseres** que lanzan nitrógeno a 8 km de altura. Casi no tiene aire y su gravedad es tan baja que un rover no se agarra al suelo, así que aquí se usa un **saltador**: un vehículo que brinca. Es el lugar más frío medido en el Sistema Solar.

## 1. Por qué Tritón y no Neptuno

Neptuno (gravedad 11,15 m/s²; vientos de hasta **2.100 km/h**) no tiene superficie. Tritón sí, y es especial: gira **al revés** que el planeta, porque probablemente es un objeto del cinturón de Kuiper que Neptuno capturó. **Neptuno domina el cielo**: una bola azul enorme en el horizonte.

## 2. Ficha real (Tritón)

| Dato | Valor real |
|---|---|
| Gravedad | **0,78 m/s² (0,08 g)**: saltas **12,6 veces** más alto que en la Tierra |
| Atmósfera | Casi nada: 14 millonésimas de bar de nitrógeno (no sirve para paracaídas) |
| Temperatura | **−235 °C**, la superficie más fría medida en el Sistema Solar |
| Superficie | Escarcha de nitrógeno rosada, terreno de "**piel de melón**" (cantaloupe), manchas oscuras de los géiseres |
| Géiseres | Columnas de nitrógeno de hasta **8 km** de alto, con polvo oscuro que el viento tenue arrastra |
| Misión real | **Voyager 2** (agosto de 1989): la única visita. Descubrió los géiseres |

## 3. Cómo se ve

| Elemento | Detalle |
|---|---|
| Cielo | **Negro** con estrellas; **Neptuno gigante y azul** `#3F66C8` en el horizonte; el Sol es solo una estrella muy brillante |
| Suelo | Escarcha rosada y blanca `#E8D5CF`, zonas gris azuladas de "piel de melón" `#9AA3B5`, **vetas oscuras** `#3B3530` que marcan los géiseres |
| Terreno | Llanuras heladas, colinas bajas con hoyuelos, respiraderos oscuros |
| Géiseres | Columnas grises que suben recto y se doblan arriba (partículas), que se activan y se apagan |
| Luz | Muy tenue, fría, azulada (reflejo de Neptuno) |
| Partículas | Escarcha que salta al aterrizar y cae **muy despacio** |

## 4. Vehículos

| Fase | Vehículo | Modelo |
|---|---|---|
| Descenso | Módulo con cohetes y patas | `VH-01_Lander` (compartido con Mercurio) |
| Misión | **Saltador**: cuerpo compacto, 3 patas con resortes, propulsores de nitrógeno | `VH-10_Hopper` (nuevo) |
| Objetivo | Respiradero del géiser | `PR-09_GeyserVent` (nuevo) |
| Cielo | Neptuno | `PL-07_Neptune` (**ya existe**) |

## 5. Fases y controles

| Fase | Qué pasa | Stick izq. | Stick der. | A (grande) | B | C | Cómo se falla |
|---|---|---|---|---|---|---|---|
| **1. Descenso** | Sin aire: solo cohetes. Con tan poca gravedad bajas **lento**, pero cada corrección gasta combustible | **Inclinar** | Mirar a Neptuno | **EMPUJE** | **PATAS** | ESCANEAR (géiseres activos) | Posarse en un respiradero activo; quedarse sin combustible |
| **2. Despliegue** | El saltador baja. **Cambia la vista** | — | — | — | — | — | — |
| **3. Saltador** | Te mueves a saltos; se ve una **línea con el arco del salto** | **Apuntar** la dirección y la distancia del salto | Mirar 360° | **SALTAR** (mantener para cargar, soltar para saltar) | **PROPULSOR** (corrige en el aire, gasta nitrógeno) | **MUESTREAR** (dentro de la columna del géiser) | Caer en escarcha blanda y resbalar a un respiradero; quedarse sin nitrógeno |

### Los géiseres (la mecánica central)

- Cada géiser **erupciona cada cierto tiempo** (unos 20 s en el juego) y dura unos segundos.
- Hay que **saltar hacia la columna mientras está activa** y apretar **MUESTREAR** estando dentro.
- El **mapeador infrarrojo** muestra cuál está por erupcionar, porque se calienta un poco antes.
- Si caes justo sobre el respiradero cuando erupciona, sales disparado (no es fatal, pero pierdes el control).

## 6. Física de este mundo

| Qué | Valor en el juego |
|---|---|
| Gravedad | 0,78 m/s²: un salto dura varios segundos en el aire |
| Arrastre | Cero |
| Agarre | Muy bajo: las ruedas patinarían (por eso el saltador); la escarcha blanca resbala |
| Géiser | Fuerza hacia arriba dentro de la columna |
| Tiempo | Real, sin acelerar: en esta gravedad todo ya es lento |

## 7. La misión y las estrellas

1. Aterrizar lejos de los respiraderos.
2. Muestrear **3 columnas de géiser** activas.
3. Volver al módulo para transmitir.

| Estrellas | Condición |
|---|---|
| ★ | Aterrizar sano y salvo |
| ★★ | 3 muestras de géiser y regreso al módulo |
| ★★★ | Además, sin usar más de la mitad del nitrógeno del propulsor |

## 8. Lo que el diseño cambia aquí

| Decisión | Efecto |
|---|---|
| **Solar** | Inútil: a Neptuno llega 900 veces menos luz que a la Tierra |
| **RTG** | Obligatorio para funcionar; su calor evita que el saltador se congele |
| Mapeador infrarrojo | Sin él no sabes cuál géiser va a erupcionar: esperas a ciegas |
| Cámara teleobjetivo | Ve los géiseres lejanos |
| Combustible sobrante | Combustible del descenso **y** nitrógeno del propulsor |
| Antena | Tritón está a 4 horas-luz: el envío es lento; con antena baja casi no llega |

## 9. Eventos

- **Neptuno sale en el horizonte**: la escena se ilumina de azul (visual).
- **Erupción doble**: dos géiseres a la vez, una oportunidad para 2 muestras en un salto.

## 10. Multijugador

Carrera de saltadores: el primero con 3 muestras gana. Se ven los saltos de todos en el cielo negro, con Neptuno detrás. Es la mejor toma para el video.

## 11. Informe "¿y si…?"

- "En la Tierra, ese salto habría sido de 1 metro. En Tritón fue de 12."
- "Sin mapeador infrarrojo esperaste 40 segundos de más junto a géiseres apagados."

## 12. Fuentes

NASA Voyager 2 en Neptuno y Tritón (1989). NASA Triton y Neptune Fact Sheet.
