# Mundo 6 — URANO · "El gigante acostado"

> Urano gira **acostado**, con su eje inclinado 98°. Solo una nave lo ha visitado (Voyager 2, 1986), de pasada. Aquí haces la misión que la NASA eligió como su **próxima gran prioridad**: un orbitador que suelta una sonda. Primero la sueltas en el momento justo desde la órbita; después la sonda cae por la neblina azul midiendo el metano y el helio, que cuentan cómo nació el planeta.

## 1. Ficha real

| Dato | Valor real |
|---|---|
| Gravedad | 8,69 m/s² (0,9 g) |
| Composición | Hidrógeno, helio y **metano** (~2,3 %), que lo hace azul verdoso |
| Temperatura | **−224 °C** en lo alto de las nubes: la atmósfera planetaria más fría |
| Inclinación | **98°**: sus anillos y lunas se ven "de pie" |
| Viento | Hasta 250 m/s |
| Nubes | Metano congelado (~1,2 bar); más abajo, sulfuro de hidrógeno (huele a huevo podrido) |
| Misiones | Solo **Voyager 2** (enero de 1986). **Uranus Orbiter and Probe**: primera prioridad de las grandes misiones en el informe decadal 2023-2032 |

**En qué se diferencia de Júpiter:**
- Aquí **el problema es el frío, no el calor**.
- Hay una **fase en órbita**: soltar la sonda a tiempo.
- La sonda solo puede transmitir **mientras el orbitador se ve en el cielo**, y el orbitador se mueve.

## 2. Cómo se ve

| Elemento | Detalle |
|---|---|
| Desde la órbita | Urano cian `#7FD1D9` casi sin detalles, **anillos finos verticales**, la luna Miranda (`MN-05_Miranda`) con sus acantilados gigantes |
| Dentro de la atmósfera | Neblina celeste `#BDE8EA` que se vuelve azul profundo `#2E6E7A` al bajar |
| Capa de metano | Cristales blancos que brillan |
| Capa de sulfuro | Niebla amarillenta |
| Luz | Muy tenue: el Sol es una estrella brillante |
| Arriba | El orbitador como un **punto que cruza el cielo** (con indicador) |

## 3. Vehículos

| Fase | Vehículo | Modelo |
|---|---|---|
| Órbita | **Orbitador** con la sonda acoplada | `VH-08_UranusOrbiter` (nuevo) |
| Entrada y descenso | Sonda atmosférica | `VH-06_AtmoProbe` (compartido con Júpiter, en color hielo) + `VH-09_Parachute` |

## 4. Fases y controles

| Fase | Qué pasa | Stick izq. | Stick der. | A (grande) | B | C | Cómo se falla |
|---|---|---|---|---|---|---|---|
| **1. En órbita** | Una **ventana de entrada** (arco verde) se mueve con la rotación del planeta | **Apuntar** el punto de entrada dentro del arco | Mirar | **SOLTAR SONDA** | — | ESCANEAR (Miranda, anillos) | Muy temprano: la sonda rebota en la atmósfera. Muy tarde: entra muy empinada y se quema |
| **2. Entrada** | La cámara **sigue a la sonda** | Mantener el ángulo | Mirar | — | — | — | Escudo quemado |
| **3. Paracaídas** | Ventana verde | — | Mirar | — | **PARACAÍDAS** | — | Se rompe |
| **4. Caída** | La sonda baja por las capas; el orbitador cruza el cielo | Inclinar (poca deriva) | Mirar **dónde está el orbitador** | **MEDIR** (dentro de cada capa) | **TRANSMITIR** (solo con el indicador del orbitador en verde) | **CALEFACTOR** (encender o apagar) | Batería congelada; lo medido y no transmitido se pierde |
| **5. Final** | A **10 bar** la misión termina (profundidad objetivo del diseño de la NASA) | — | — | — | — | — | — |

### El calefactor

El frío de −224 °C congela la batería. Con el calefactor **encendido** la batería se mantiene, pero gasta energía. **Apagado** ahorra, pero si la temperatura baja demasiado, la sonda se apaga. Se juega en los dos sentidos. Más abajo (desde unos 1.200 m) hace menos frío: ahí se puede apagar sin peligro y el juego te avisa.

## 5. Física de este mundo

| Qué | Valor en el juego |
|---|---|
| Gravedad | 8,69 m/s² |
| Presión | De 0,1 a 10 bar al bajar |
| Temperatura | −224 °C arriba, sube al bajar |
| Viento | Hasta 250 m/s: deriva fuerte al principio |
| Visibilidad del orbitador | Un arco de tiempo en que se puede transmitir; luego se esconde |

## 6. La misión y las estrellas

1. Soltar la sonda dentro de la ventana.
2. Medir: **metano** (capa 1), **sulfuro de hidrógeno** (capa 2) y **la proporción de helio**, que dice cómo se formó Urano.
3. Transmitir mientras el orbitador está a la vista.

| Estrellas | Condición |
|---|---|
| ★ | Sonda soltada y sobreviviendo a la entrada |
| ★★ | 3 mediciones transmitidas |
| ★★★ | Además, llegar a 10 bar con la batería por encima de 0 |

## 7. Lo que el diseño cambia aquí

| Decisión | Efecto |
|---|---|
| **Solar** | No sirve: llega 400 veces menos luz que a la Tierra. Solo alcanza la batería |
| **RTG** | El orbitador lo necesita (la misión real lo lleva). Su calor sobrante calienta la sonda antes de soltarla: arranca más tibia |
| Espectrómetro UV | Mide metano y helio (sin él, la mitad de puntos) |
| Antena | Tamaño de la ventana útil para transmitir |
| Magnetómetro | Puntos extra: el campo magnético de Urano está **torcido**, uno de sus misterios |
| Radio ciencia | Mide los vientos |

## 8. Eventos

- **Estación del año:** por la inclinación, un polo tiene 42 años de día. Cambia la luz de la escena (dato curioso en pantalla).
- **Corriente de viento:** deriva brusca en la capa alta.

## 9. Multijugador

Varias sondas desde el mismo orbitador: cada una se suelta en un momento distinto y gana quien transmite más.

## 10. Informe "¿y si…?"

- "Soltaste la sonda 12 segundos tarde: entró más empinada y perdió 40 % del escudo."
- "Con el calefactor siempre encendido, la batería habría durado solo hasta 6 bar."

## 11. Fuentes

NASA Uranus Fact Sheet. Voyager 2 en Urano (1986). Informe decadal de ciencia planetaria 2023-2032 (Uranus Orbiter and Probe).
