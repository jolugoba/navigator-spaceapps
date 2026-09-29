# Navigator — Los 7 mundos: guía común

Cuando la nave llega al planeta con el objetivo **Aterrizar**, el juego cambia a otra vista: el **mundo** de ese planeta, con su propia física, sus colores y su propia misión.

Esta guía explica lo que comparten todos los mundos. Cada planeta tiene su ficha:

| # | Mundo | Dónde se juega | Vehículo de bajada | Vehículo en el mundo | Misión |
|---|---|---|---|---|---|
| 1 | [Mercurio](01_MERCURIO.md) | La superficie, en el polo norte | Módulo de aterrizaje con cohetes | **Rover térmico** | Sacar hielo de un cráter en sombra eterna |
| 2 | [Venus](02_VENUS.md) | La superficie | Cápsula, paracaídas y disco de freno | **Sonda fija** con brazo | Fotos y taladro antes de que el calor la destruya |
| 3 | [Marte](03_MARTE.md) | El cráter Jezero | Cápsula, paracaídas y grúa aérea | **Rover** (Perseverance) | 3 muestras de roca llevadas al depósito |
| 4 | [Júpiter](04_JUPITER.md) | *Dentro* de la atmósfera (no hay suelo) | Sonda de entrada (Galileo) | **Sonda que cae** | Medir las 3 capas de nubes antes de ser aplastada |
| 5 | [Saturno → Titán](05_SATURNO_TITAN.md) | Titán, su luna (Saturno no tiene suelo) | Cápsula con paracaídas (Huygens) | **Dron** (Dragonfly) | Volar entre dunas y lagos de metano y recoger moléculas orgánicas |
| 6 | [Urano](06_URANO.md) | Órbita y atmósfera | Orbitador que suelta una sonda | **Orbitador + sonda** | Soltar la sonda a tiempo y medir metano y helio |
| 7 | [Neptuno → Tritón](07_NEPTUNO_TRITON.md) | Tritón, su luna (Neptuno no tiene suelo) | Módulo de aterrizaje con cohetes | **Saltador** | Saltar entre géiseres de nitrógeno y tomar muestras de sus columnas |

Los modelos 3D que hacen falta están en [`NAVIGATOR_MESHY_MUNDOS.md`](../../NAVIGATOR_MESHY_MUNDOS.md): 20 modelos nuevos, el resto se reutiliza.

---

## 1. La secuencia (igual en los 7)

```
VUELO (lo que ya existe)
  └─ llegas al planeta con objetivo ATERRIZAR
      1. LLEGADA      corte de cámara: el planeta llena la pantalla, texto con los datos reales
      2. ENTRADA      la cápsula entra a la atmósfera (si la hay): mantener el ángulo para no quemarse
      3. DESCENSO     paracaídas / cohetes / grúa según el planeta, con su gravedad, aire y viento
      4. CONTACTO     tocar suelo despacio y derecho (o, en los gigantes, llegar a la capa objetivo)
      5. DESPLIEGUE   se suelta el vehículo → CAMBIA LA VISTA → cámara detrás del vehículo
      6. MISIÓN       objetivos del mundo con el vehículo (3 estrellas)
      7. RESULTADO    el informe final agrega el mundo: qué pasó y "¿y si hubieras…?"
```

Si el objetivo es **Sobrevolar** u **Orbitar**, se queda la fase de ciencia que ya existe. Los mundos son el premio del objetivo más difícil, **Aterrizar**.

## 2. Controles: siempre la misma lógica

Los controles son los de la nave: dos joysticks y los botones redondos en los mismos lugares. Solo cambian **qué hace cada uno** y las **etiquetas**.

| Control | Lugar | Qué hace (siempre) |
|---|---|---|
| **Stick izquierdo** | Abajo a la izquierda | **Mover** el vehículo (manejar, inclinar el módulo, dirigir el dron) |
| **Stick derecho** | Abajo a la derecha | **Mirar** 360° alrededor del vehículo |
| **Botón A** (grande) | Donde está DISPARAR | La **acción principal** de ese momento: EMPUJE, TALADRAR, SALTAR… |
| **Botón B** (mediano) | Donde está BOOST | La **segunda acción**: PARACAÍDAS, FOTO, PROPULSOR… |
| **Botón C** (chico) | Donde está ESCANEAR | **ESCANEAR** o la acción de datos (MEDIR, TRANSMITIR) |
| Giroscopio | — | También mira, como en el vuelo (se puede apagar con GYRO) |

Cada ficha trae su tabla de controles por fase. **Nunca hay más de 3 botones a la vez**: si una fase necesita otro, reemplaza a uno que ya no sirve (por ejemplo, PARACAÍDAS desaparece cuando ya se abrió).

## 2.1 Cómo se interactúa con el mundo (reglas comunes)

Estas reglas valen para los 7 mundos. Así el jugador aprende una vez y lo usa en todos.

| Situación | Qué ve y qué hace el jugador |
|---|---|
| **Objetivo lejos** | Una **flecha** en el borde de la pantalla apunta al objetivo, con la distancia ("Roca del delta · 120 m"). Los objetivos son balizas de luz (`OB-03_Beacon`) visibles de lejos |
| **Objetivo cerca** | Al entrar al **círculo de acción** (unos 4 m), el objetivo brilla y el botón que sirve **se ilumina y late**: "TALADRAR", "FOTO", "MUESTREAR" |
| **Acciones de mantener** (taladrar, medir, empuje) | Mantener el botón llena un **anillo alrededor del botón** (como la recarga de DISPARAR). Si sueltas antes, se pausa y no se pierde |
| **Botón que no sirve ahora** | Se ve gris. Si **falta el instrumento** en el diseño, al tocarlo dice por qué: "No llevas taladro: sin él no hay muestras" (lección de diseño) |
| **Ventanas de tiempo** (paracaídas, soltar sonda, géiser) | Una **barra con una zona verde** que se mueve; se aprieta cuando la aguja está en lo verde. Si falla, la sonda sigue con consecuencias, no se reinicia |
| **Escanear** | C sobre lo que está en la mira: aparece la tarjeta con el **dato real** (igual que en el vuelo). Algunas cosas solo se ven con el instrumento correcto (el hielo con infrarrojo) |
| **Medidores del mundo** | Arriba a la izquierda, donde están velocidad y combustible: **batería** siempre, y el medidor propio del mundo: **altura y velocidad vertical** al bajar, **temperatura** (Mercurio), **calor** (Venus), **presión** (Júpiter y Urano), **nitrógeno** (Tritón) |
| **Aterrizaje** | Indicador de contacto: velocidad vertical, horizontal e inclinación en verde, amarillo o rojo. Todo verde = aterrizaje suave |
| **Cambio de vista** | Al soltar el vehículo: corte con un destello, la cámara pasa detrás del vehículo y se muestra un **cartel de 3 segundos** con la misión: "MARTE · Lleva 3 muestras al depósito" |
| **Datos que se envían** | Una barra de "datos enviados" en la parte de arriba. Lo que no se envía se pierde si el vehículo muere (Júpiter, Urano y Venus) |
| **Fallar** | Explosión, derrumbe o congelamiento con un texto claro de la causa real. Luego pasas al informe, que dice qué hubiera cambiado. Siempre se puede **REINTENTAR** el mundo sin repetir el viaje |
| **Salir** | Botón SALIR arriba a la izquierda (como en el vuelo libre): termina la misión con lo logrado hasta ahí |
| **Sensación** | Vibración al tocar suelo, taladrar y recibir un golpe; sonido del viento según la densidad del aire (en Mercurio y Tritón, **silencio**: no hay aire que lleve el sonido) |

## 3. Física: lo que cambia de un mundo a otro

Todo se calcula con valores reales. Solo se acelera el tiempo para que se pueda jugar: un aterrizaje de 7 minutos dura unos 60-90 segundos.

| Mundo | Gravedad (m/s²) | Salto vs. Tierra | Aire en el suelo | Temperatura | Viento |
|---|---|---|---|---|---|
| Mercurio | 3,70 | 2,7× más alto | Ninguno | +430 °C sol / −180 °C sombra | — |
| Venus | 8,87 | 1,1× | 92 bar (como a 900 m bajo el mar) | 464 °C | Arriba 100 m/s, abajo ~1 m/s |
| Marte | 3,71 | 2,6× | 0,006 bar | −63 °C promedio | Ráfagas y tormentas de polvo |
| Júpiter | 24,79 | — (sin suelo) | Aumenta hasta aplastar | Sube al bajar | Bandas de hasta 150 m/s |
| Titán | 1,35 | 7,3× | 1,5 bar (más que la Tierra) | −179 °C | Suave |
| Urano | 8,69 | — (sin suelo) | Aumenta al bajar | −224 °C arriba | Hasta 250 m/s |
| Tritón | 0,78 | 12,6× | Casi nada (14 millonésimas de bar) | −235 °C | — |

En el código, un mismo componente recibe estos números por mundo:
- **Gravedad**: aceleración hacia abajo.
- **Densidad del aire**: arrastre y paracaídas. Sin aire, el paracaídas no hace nada, y eso se le explica al jugador.
- **Viento**: fuerza lateral por altura.
- **Límite de supervivencia**: calor (Venus), presión (Júpiter, Urano), frío o batería (Mercurio, Titán).

## 4. Lo que el diseño decide dentro del mundo

Esta es la parte más importante para el reto: las decisiones del diseño **se ven** en el mundo.

| Decisión del diseño | Efecto en el mundo |
|---|---|
| Combustible que sobró al llegar | Combustible del descenso con cohetes: con poco, frenar es difícil |
| **Energía**: solar, RTG o reactor | Batería del vehículo. Solar: fuerte en Mercurio, débil en Marte y casi nula de Júpiter hacia afuera. RTG: constante en todas partes |
| **Antena** | Velocidad para enviar los datos a casa. Con antena baja, la última muestra puede no llegar a tiempo |
| **Instrumentos** | Cada instrumento del diseño habilita algo en el mundo (tabla de abajo) |
| **Motor** | Iónico: casi no sirve para frenar al aterrizar. El juego lo explica y el descenso usa el combustible de reserva |

**Instrumentos que ya existen en el diseño y qué hacen en los mundos:**

| Instrumento | En el mundo |
|---|---|
| Cámara gran angular | Fotos panorámicas (Venus) y ver el terreno alrededor |
| Cámara teleobjetivo | Ver de lejos: marca las rocas y los géiseres desde lejos |
| Espectrómetro UV | Gases: metano y helio (Urano), orgánicos (Titán), capas de nubes (Júpiter) |
| Mapeador infrarrojo | Temperatura: encuentra el hielo (Mercurio) y los géiseres activos (Tritón) |
| Magnetómetro | Mide el campo magnético (Júpiter y Urano, puntos extra) |
| Detector de partículas | Mide la radiación de Júpiter; sin él, la radiación daña sin avisar |
| Radio ciencia | Mide los vientos con la señal, como la sonda Galileo (Júpiter y Urano) |
| **Altímetro radar** | **Aterrizaje**: muestra la altura real y el terreno. Sin él, la altura es una estimación (±20 %) y aterrizar es más difícil, como antes de que existiera el radar |

**Instrumento nuevo que hay que agregar:** **Taladro y brazo de muestras**, unos 30 kg y 60 W, basado en el sistema de muestras de Perseverance. Sin él no hay muestras en Mercurio, Venus, Marte, Titán ni Tritón.

## 5. Sistemas que se construyen una vez y usan todos

| Sistema | Qué hace | Lo usan |
|---|---|---|
| **WorldProfile** (datos) | Gravedad, aire, viento, colores, texturas, peligros y textos de cada mundo | Los 7 |
| **WorldBuilder** | Genera el terreno (relieve con ruido y cráteres), el cielo (degradado + niebla), la luz y las partículas (polvo, lluvia, géiseres) con los datos del perfil | Los 7 (en Júpiter y Urano, capas de nubes en vez de suelo) |
| **DescentController** | Gravedad + arrastre + paracaídas + cohetes + viento + control de aterrizaje suave | Los 7 |
| **GroundVehicle** | Rover físico (ruedas por raycast, suspensión, tracción según gravedad) | Mercurio, Marte; el **saltador** de Tritón es el mismo con salto |
| **FlyingVehicle** | El control de nave actual con gravedad y sustentación | Dron de Titán |
| **FixedLander** | Sin moverse: brazo, cámara y reloj de supervivencia | Venus |
| **FallingProbe** | Descenso sin contacto: capas, presión y transmisión | Júpiter, Urano |
| **WorldObjectives** | Marcadores, flecha, "TALADRA AQUÍ", 3 estrellas y tiempo | Los 7 |
| **WorldHUD** | Mismo HUD y joysticks; cambia las etiquetas y los medidores (altura, velocidad vertical, batería, calor, presión) | Los 7 |

**Lo que ya existe y se reutiliza:**
- Los dos joysticks (`VirtualJoystick`), los botones redondos y el HUD horizontal.
- La cámara de persecución (`GyroCameraRig`), las partículas y el sistema de combustible.
- El informe con "¿y si…?" (`MissionReport`) y el P2P para multijugador.

## 6. Multijugador

Todos los que aterrizan en el mismo mundo lo comparten, igual que el vuelo libre: ves los vehículos de tus amigos. Cada ficha propone su versión de carrera (quién saca primero las 3 muestras, etc.).

## 7. Orden para construir

1. **Marte completo.** Es el más conocido y el mejor para el video; construye el terreno, el descenso y el rover.
2. **Mercurio y Tritón.** Reusan el descenso con cohetes y el rover (Tritón con salto).
3. **Venus** (sonda fija), **Júpiter y Urano** (sonda que cae).
4. **Titán** (dron).

Si un mundo no está listo, el aterrizaje termina como hoy (fase de ciencia). **Nada de lo que ya funciona se rompe.**
