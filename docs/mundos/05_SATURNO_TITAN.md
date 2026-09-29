# Mundo 5 — SATURNO → TITÁN · "Volar entre lagos de metano"

> Saturno es gas: no tiene suelo. Pero su luna **Titán** sí, y es el único lugar del Sistema Solar exterior donde la humanidad ha aterrizado (Huygens, 2005). Tiene aire más denso que la Tierra, lluvia y **lagos de metano** y dunas de moléculas orgánicas. Bajas en paracaídas y sueltas un **dron** que vuela entre sitios para buscar la química de la vida.

## 1. Por qué Titán y no Saturno

Saturno (gravedad 10,44 m/s², hidrógeno y helio) se ve en el descenso con sus anillos enormes. Luego la cámara baja a Titán. El juego le explica al jugador que en Saturno no se puede aterrizar y que por eso la NASA eligió Titán, como Huygens y Dragonfly.

## 2. Ficha real (Titán)

| Dato | Valor real |
|---|---|
| Gravedad | **1,35 m/s² (0,14 g)**: saltas **7 veces** más alto |
| Atmósfera | **1,5 bar** (más que la Tierra), 95 % nitrógeno y 5 % metano |
| Temperatura | **−179 °C** |
| Luz | Unas mil veces menos que un día en la Tierra, y naranja por la neblina |
| Paisaje | Dunas de granos orgánicos, **lagos y mares de metano líquido** (Kraken Mare), guijarros de hielo |
| Misión real | **Huygens** (14 ene. 2005): bajó 2 h 27 min en paracaídas y tocó suelo a ~4,5 m/s. **Dragonfly** (NASA, sale en 2028, llega en 2034): un **dron** de 8 hélices que volará al cráter Selk |
| Dato genial | Con tan poca gravedad y tanto aire, **volar es muy fácil**: una persona con alas podría volar batiendo los brazos |

## 3. Cómo se ve

| Elemento | Detalle |
|---|---|
| Descenso alto | Saturno y sus anillos gigantes en el cielo, y abajo Titán como una bola naranja (`MN-03_Titan`) |
| Cielo en la superficie | Neblina **naranja** `#D8A24A`; no se ve ni el Sol ni Saturno |
| Suelo | Café `#6E4B2A`, dunas oscuras `#4E3620`, guijarros de hielo claros `#CFC7B8` |
| Lagos | **Negros como espejo** `#0E0E10`, lisos, que reflejan la neblina |
| Terreno | Campo de dunas, el cráter Selk (borde y fondo), orilla de un lago |
| Partículas | **Lluvia de metano** (gotas grandes que caen despacio); niebla baja |
| Luz | Tenue y cálida, sin sombras duras |

## 4. Vehículos

| Fase | Vehículo | Modelo |
|---|---|---|
| Entrada y descenso | Cápsula Huygens con 2 paracaídas | `VH-03_EntryCapsule` + `VH-09_Parachute` (compartidos) |
| Misión | **Dron Dragonfly**: 8 hélices, patas de esquí, RTG | `VH-07_Dragonfly` (nuevo) |
| Muestras | Guijarros de hielo y arena orgánica | `PR-07_TitanPebbles` (nuevo) |

## 5. Fases y controles

| Fase | Qué pasa | Stick izq. | Stick der. | A (grande) | B | C | Cómo se falla |
|---|---|---|---|---|---|---|---|
| **1. Entrada** | Escudo contra el aire alto | Mantener el ángulo | Mirar | — | — | — | Escudo quemado |
| **2. Paracaídas grande** | Frena fuerte | — | Mirar Saturno | — | **PARACAÍDAS** | ESCANEAR la neblina | Fuera de la ventana: se rompe |
| **3. Cambio a paracaídas chico** | Huygens lo cambió para no tardar horas | — | Mirar | — | **PARACAÍDAS CHICO** | ESCANEAR | Si no lo cambias, el descenso tarda tanto que gastas batería |
| **4. Deriva y contacto** | El viento te lleva; el suelo aparece entre la niebla | **Inclinar** para derivar hacia la zona buena (no al lago) | Mirar | — | — | — | Caer en el lago |
| **5. Despliegue** | La cápsula se abre y el dron despega. **Cambia la vista** | — | — | — | — | — | — |
| **6. Dron** | Vuelas entre los 3 sitios | **Volar** adelante, atrás y a los lados | Mirar 360° | **SUBIR** (mantener; si sueltas, baja despacio) | **ATERRIZAR / MUESTRA** (posado en el sitio) | **ESCANEAR** (orgánicos) | Batería en 0 en el aire (cae); posarse en el lago |

## 6. Física de este mundo

| Qué | Valor en el juego |
|---|---|
| Gravedad | 1,35 m/s² |
| Densidad del aire | ~5,3 kg/m³ (**4 veces la de la Tierra**): el paracaídas frena mucho y el dron sube con poca potencia |
| Dron | Sustentación = potencia × densidad; vuela lento y estable, con inercia (flota) |
| Batería | El RTG la recarga **solo con el dron posado**: hay que aterrizar para recargar (como el Dragonfly real) |
| Lagos | Superficie líquida: si te posas, el dron se hunde (fin) |
| Tiempo | 2 h 27 min de descenso → unos 70 segundos de juego |

## 7. La misión y las estrellas

Hay 3 sitios, como la misión real:
1. **Dunas**: arena de moléculas orgánicas.
2. **Cráter Selk**: donde agua de un impacto se mezcló con orgánicos. Es el mejor lugar para la química de la vida y el objetivo de Dragonfly.
3. **Orilla del lago de metano**: tomar la muestra sin caer al lago.

| Estrellas | Condición |
|---|---|
| ★ | Aterrizar fuera del lago |
| ★★ | Muestras de los 3 sitios |
| ★★★ | Además, nunca bajar del 15 % de batería en el aire |

## 8. Lo que el diseño cambia aquí

| Decisión | Efecto |
|---|---|
| **Solar** | Casi no funciona (neblina y mucha distancia al Sol): vuelos muy cortos |
| **RTG** | Lo correcto (Dragonfly usa uno): recarga constante al posarse |
| Espectrómetro UV | Encuentra los orgánicos; sin él, las muestras valen la mitad |
| Taladro | Sin él el dron solo **recoge** arena suelta: más lento, un tercio de los puntos, ★★ como máximo |
| Cámara gran angular | Ve los sitios desde el aire (sin ella, los marcadores aparecen más cerca) |
| Antena | Velocidad para enviar a casa; a esta distancia las antenas bajas casi no llegan |

## 9. Eventos

- **Lluvia de metano**: se ve menos y el dron pesa más (baja un poco).
- **Ráfaga**: empuja el dron hacia el lago.

## 10. Multijugador

Carrera de drones: gana quien junta primero las 3 muestras. Chocar en el aire empuja al otro dron (el aire denso amortigua).

## 11. Informe "¿y si…?"

- "A Saturno llega 1 % de la luz del Sol que recibe la Tierra, y la neblina de Titán bloquea casi todo: con paneles solares tu dron habría volado 20 segundos."
- "Huygens tocó suelo a 4,5 m/s; tú, a 3,1."

## 12. Fuentes

ESA/NASA Cassini-Huygens (descenso del 14 ene. 2005). NASA Dragonfly (dragonfly.jhuapl.edu). NASA Titan Fact Sheet.
