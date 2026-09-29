# Mundo 4 — JÚPITER · "La caída sin fondo"

> Júpiter **no tiene suelo**: es gas hasta el centro. No se aterriza, **se cae**. Tu sonda entra a la velocidad más alta jamás lograda, abre el paracaídas y baja por las capas de nubes midiendo todo lo que puede **antes de que la presión la aplaste**. La sonda no vuelve: el éxito es cuántos datos llegan a casa.

## 1. Ficha real

| Dato | Valor real |
|---|---|
| Gravedad | **24,79 m/s² (2,5 g)** |
| Composición | ~90 % hidrógeno, ~10 % helio |
| Nubes (de arriba a abajo) | **Amoníaco** (blanco, ~0,7 bar) → **hidrosulfuro de amonio** (naranja-café, ~2 bar) → **agua** (con relámpagos, ~5 bar) |
| Viento | Bandas de hasta 150 m/s |
| Radiación | La más intensa de los planetas: daña la electrónica |
| Misión real | **Sonda Galileo** (7 dic. 1995): entró a **47,4 km/s** (el récord), frenó con **228 g**, perdió unos 80 kg de sus 152 kg de escudo, transmitió **58 minutos** y llegó a **~22 bar** antes de perderse |
| Sorpresa real | Galileo cayó en un "**punto caliente**", una zona seca y sin nubes, y encontró mucha menos agua de la esperada. **Dónde caes importa** |

## 2. Cómo se ve

No hay terreno: es un **túnel vertical de nubes**.

| Capa | Color | Qué hay |
|---|---|---|
| Arriba | Cielo negro; Júpiter llena todo con sus **bandas** crema `#E8DCC0`, café `#B5764A` y roja `#A0482E` | La Gran Mancha Roja a lo lejos; Io (`MN-02_Io`) y el orbitador Juno (`OB-08_Juno`) arriba |
| Capa 1: amoníaco | Blanco-crema, cristales brillantes | Niebla blanca |
| Capa 2: hidrosulfuro | Naranja-café | Niebla densa, más oscura |
| Capa 3: agua | Azul grisáceo `#5C6E8C` | **Relámpagos** dentro de las nubes |
| Abajo | Cada vez más oscuro y rojo por el calor | Fin: la sonda se aplasta |

Se usa niebla por capas y partículas: cristales, gotas y relámpagos.

## 3. Vehículos

| Fase | Vehículo | Modelo |
|---|---|---|
| Entrada | Sonda cónica con escudo grueso | `VH-06_AtmoProbe` (nuevo; se reutiliza en Urano con otro color) |
| Descenso | La misma sonda colgando del paracaídas | `VH-06` + `VH-09_Parachute` |
| Arriba | El orbitador que recibe la señal | `OB-08_Juno` (**ya existe**) |

## 4. Fases y controles

| Fase | Qué pasa | Stick izq. | Stick der. | A (grande) | B | C | Cómo se falla |
|---|---|---|---|---|---|---|---|
| **1. Entrada** (47 km/s) | La entrada más violenta: el escudo se **consume** (barra) | Mantener el ángulo exacto (círculo verde, pequeño) | Mirar | — | — | — | Escudo en 0: la sonda se desintegra |
| **2. Paracaídas** | Ventana verde | — | Mirar | — | **PARACAÍDAS** | — | Fuera de la ventana: se rompe y caes demasiado rápido (menos tiempo para medir) |
| **3. Elegir dónde caer** | Ves abajo las bandas: **nubes** (mucha ciencia) o **punto caliente** (seco, poca ciencia, como Galileo) | **Inclinar** el paracaídas para derivar | Mirar | — | — | **ESCANEAR** (qué hay abajo) | — |
| **4. Caída por las capas** | Bajas por las 3 capas; el **viento** te empuja; la presión y la temperatura suben | Inclinar (poca deriva) | Mirar | **MEDIR** (mantener dentro de cada capa) | **TRANSMITIR** al orbitador | ESCANEAR | Pasar una capa sin medirla |
| **5. Final** | A ~22 bar la sonda se aplasta (se ve en la pantalla) | — | — | — | — | — | Es el final esperado: cuenta lo que transmitiste |

**Importante:** lo medido y **no transmitido** se pierde con la sonda. Hay que medir y transmitir sin parar: la antena decide qué tan rápido.

## 5. Física de este mundo

| Qué | Valor en el juego |
|---|---|
| Gravedad | 24,79 m/s²: caes rápido, el paracaídas trabaja mucho |
| Presión | Sube de 0,5 bar a 22 bar al bajar |
| Temperatura | Sube con la presión |
| Viento | Hasta 150 m/s, cambia por banda |
| Radiación | Daña al inicio (arriba); disminuye al bajar |
| Tiempo | Los 58 minutos reales duran unos 2 minutos de juego |

## 6. La misión y las estrellas

1. Sobrevivir a la entrada.
2. Medir las **3 capas** de nubes (amoníaco, hidrosulfuro y agua).
3. Transmitir todo antes del final.

| Estrellas | Condición |
|---|---|
| ★ | Sobrevivir a la entrada y abrir el paracaídas |
| ★★ | 3 capas medidas **y transmitidas** |
| ★★★ | Además, caer en una zona de nubes (no en el punto caliente) y superar los 58 minutos de Galileo |

## 7. Lo que el diseño cambia aquí

| Decisión | Efecto |
|---|---|
| Energía | Solo cuenta la batería de la sonda (Galileo también usaba baterías). Solar: inútil aquí, recibe 25 veces menos luz que la Tierra |
| **Antena** | Clave: velocidad para enviar al orbitador. Con antena baja pierdes la capa 3 |
| Espectrómetro UV | Mide la composición de cada capa (sin él, MEDIR da la mitad de puntos) |
| Detector de partículas | Mide la radiación y avisa; sin él, la radiación daña sin aviso |
| Radio ciencia | Mide los vientos con la señal, como hizo Galileo (puntos extra) |
| Magnetómetro | Puntos extra: el campo magnético de Júpiter es el más fuerte |

## 8. Eventos

- **Relámpagos** en la capa de agua: interfieren con la transmisión durante un momento.
- **Corriente descendente**: te baja más rápido, con menos tiempo en esa capa.

## 9. Multijugador

Todos caen a la vez: gana quien transmite más ciencia. Se ve la sonda de cada uno cayendo cerca.

## 10. Informe "¿y si…?"

- "Galileo cayó en un punto caliente y encontró poca agua; tú elegiste las nubes y mediste 3 veces más."
- "Con la antena alta habrías enviado la capa de agua antes de ser aplastado."

## 11. Fuentes

NASA Galileo Probe (NSSDCA, JPL). NASA Juno. NASA Jupiter Fact Sheet.
