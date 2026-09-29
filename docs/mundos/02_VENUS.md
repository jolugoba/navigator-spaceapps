# Mundo 2 — VENUS · "El horno"

> El planeta más caliente del Sistema Solar. Bajas a través de nubes de ácido sulfúrico, aterrizas en una llanura de lava y tienes **pocos minutos** para tomar fotos, taladrar roca y enviar los datos antes de que el calor destruya la sonda.

## 1. Ficha real

| Dato | Valor real |
|---|---|
| Gravedad | 8,87 m/s² (0,9 g), casi como la Tierra |
| Atmósfera | **92 bar** (como estar a 900 m bajo el mar), 96,5 % CO₂ |
| Temperatura | **464 °C**, igual de día y de noche: se derrite el plomo |
| Nubes | **Ácido sulfúrico**, entre 45 y 70 km de altura |
| Viento | 100 m/s arriba (la atmósfera da la vuelta al planeta en 4 días); ~1 m/s en el suelo |
| Día | 243 días terrestres, girando al revés |
| Misiones reales | **Venera 7** (1970): primer aterrizaje en otro planeta. **Venera 13** (1982): fotos a color y **127 minutos** de vida. Próximas: DAVINCI y VERITAS (NASA) |

**Lo que lo hace único:** el aire es tan denso que **caes como en agua**. Las Venera soltaban el paracaídas a unos 47 km y bajaban solo con un disco de freno. Sin rover: la sonda **no se mueve**. Aquí la mecánica es un **reloj de supervivencia**.

## 2. Cómo se ve

| Elemento | Detalle |
|---|---|
| Cielo | **Naranja apagado** `#D98C3A`, sin Sol visible, nubes amarillentas `#E6D38F` encima |
| Suelo | Basalto oscuro `#4A3B30` teñido de naranja, **placas planas** de lava rotas (como en las fotos de Venera 13) |
| Terreno | Llanura volcánica; un volcán gigante en el horizonte (Maat Mons); relieve áspero ("teselas") a los lados |
| Luz | Difusa, sin sombras marcadas, como en un día muy nublado |
| Atmósfera | Niebla densa: se ve a poca distancia; el aire **ondula** por el calor |
| Descenso | Capas de nubes amarillas que pasan; gotas de ácido en la cámara |

## 3. Vehículos

| Fase | Vehículo | Modelo |
|---|---|---|
| Entrada | Cápsula con escudo de calor | `VH-03_EntryCapsule` (compartido) |
| Paracaídas | Paracaídas | `VH-09_Parachute` (compartido) |
| Descenso y superficie | **Sonda Venera**: esfera blindada sobre un anillo de aterrizaje, con un disco de freno arriba y un brazo con taladro | `VH-04_VenusLander` (nuevo) |

## 4. Fases y controles

| Fase | Qué pasa | Stick izq. | Stick der. | A (grande) | B | C | Cómo se falla |
|---|---|---|---|---|---|---|---|
| **1. Entrada** (11 km/s) | La cápsula brilla | Mantener el ángulo (círculo verde) | Mirar | — | — | — | El escudo se quema |
| **2. Paracaídas** (a ~65 km) | Ventana verde en el velocímetro | — | Mirar | — | **PARACAÍDAS** | — | Fuera de la ventana: se rompe |
| **3. Nubes de ácido** | El **viento de 100 m/s** te empuja de lado; el medidor de **corrosión** sube mientras estés en las nubes | **Inclinar** el paracaídas para derivar hacia la zona segura (llanura) | Mirar | — | **SOLTAR PARACAÍDAS** | ESCANEAR nubes | Quedarte mucho tiempo en las nubes corroe la sonda |
| **4. Caída libre** (bajo 47 km) | Sin paracaídas: el **aire espeso** te frena solo, con el disco | Inclinar el disco (poca deriva) | Mirar el suelo | — | — | — | Caer en las teselas (roca áspera): la sonda se voltea |
| **5. Contacto** (~7 m/s) | El anillo aplastable absorbe el golpe. **Empieza el reloj de calor** | — | — | — | — | — | — |
| **6. En la superficie** | **Cambia la vista**: cámara junto a la sonda. No se mueve: trabaja el brazo | **Mover el brazo** sobre el suelo | Mirar 360° (la cámara) | **TALADRAR** (mantener) | **FOTO** (cuando el visor está sobre un punto marcado) | **TRANSMITIR** | El reloj llega a 0 antes de transmitir |

### El reloj de calor

La sonda aguanta unos **2 minutos de juego**, que equivalen a los 127 minutos de Venera 13 (1 segundo ≈ 1 minuto real). Mientras más haces, más se calienta por dentro:
- Taladrar: +calor.
- Transmitir: +calor.
- No hacer nada: el calor entra igual.

## 5. Física de este mundo

| Qué | Valor en el juego |
|---|---|
| Gravedad | 8,87 m/s² |
| Densidad del aire en el suelo | ~65 kg/m³ (**65 veces la de la Tierra**): velocidad de caída límite de unos 7-8 m/s solo con el disco |
| Viento | 100 m/s a la altura de las nubes → ~1 m/s al llegar al suelo |
| Tiempo | Todo el descenso real (~1 hora) dura unos 70 segundos |

## 6. La misión y las estrellas

1. **Fotos panorámicas:** 4 puntos marcados alrededor. Con el stick derecho giras la cámara y con **FOTO** la tomas.
2. **2 muestras de roca:** con el stick izquierdo mueves el brazo sobre la placa marcada y mantienes **TALADRAR**.
3. **TRANSMITIR** todo al orbitador que pasa encima, antes de que se esconda en el horizonte y antes de que el calor te gane.

| Estrellas | Condición |
|---|---|
| ★ | Aterrizar en la llanura |
| ★★ | 4 fotos + 2 muestras transmitidas |
| ★★★ | Además, superar los 127 "minutos" de Venera 13 |

## 7. Lo que el diseño cambia aquí

| Decisión | Efecto |
|---|---|
| **Solar** | Casi inútil: bajo las nubes llega muy poca luz. Solo alcanza la batería inicial |
| **RTG** | Energía constante, pero **agrega calor** (menos tiempo). Otra decisión con dos caras |
| Cámara gran angular | Sin ella no hay fotos |
| Taladro | Sin él no hay muestras |
| Espectrómetro UV | Analiza las nubes al bajar (puntos extra) |
| Antena | Qué tanto alcanzas a enviar antes del final: con antena baja, quizá solo llegan las fotos |
| Altímetro radar | Sin él no sabes cuándo soltar el paracaídas: lo haces a ojo |

## 8. Eventos

- **Relámpagos** en las nubes durante el descenso: interferencia en la radio.
- **El orbitador se va:** la ventana para transmitir es limitada (flecha y temporizador).

## 9. Multijugador

Varias sondas en la misma llanura: gana quien transmite más datos antes de "morir". Se ve cuánto duró cada una.

## 10. Informe "¿y si…?"

- "Con la antena de alta ganancia habrías enviado las 2 muestras, no solo 1."
- "Soltaste el paracaídas 8 km tarde: estuviste 30 % más tiempo en el ácido."
- "Venera 13 duró 127 minutos; tu sonda duró 104."

## 11. Fuentes

NASA Venus Fact Sheet. NSSDCA: Venera 7 y Venera 13. NASA DAVINCI y VERITAS.
