# NAVIGATOR 2.0 — Lista Completa de Assets Meshy
**Guacalita Inc. — Lo que hay que crear antes de programar**
**Fecha: 2026-09-22**

---

## HISTORIA DEL JUEGO

### Narrativa — Inicio, Nudo, Desenlace

**ACTO 1 — EL LLAMADO**
> Año 2041. Una señal anómala cerca de Neptuno amenaza con desviar
> asteroides hacia la Tierra. La NASA activa el Programa NAVIGATOR:
> cadetes espaciales diseñan y pilotan misiones de rescate.
> El jugador es reclutado. Diseña su nave. Se une a su equipo.

**ACTO 2 — LA MISIÓN**
> Los cadetes despegan en AR desde su mundo real.
> En el camino: asteroides, agencia rival KRONOS intentando llegar primero,
> combustible limitado, retrasos de señal de minutos.
> Los jugadores cooperan: transfieren combustible, se cubren, se comunican
> por red post-cuántica que nadie más puede interceptar.

**ACTO 3 — EL ENCUENTRO**
> Al llegar al planeta: despliegan instrumentos científicos.
> Descubren que la anomalía es artificial — KRONOS la está causando.
> Transmiten datos a la Tierra. Score final determina el héroe de la misión.

---

## PERSONAJES

### PJ-01 — EL CADETE (jugador principal)
```
Descripción:   Astronauta joven con traje espacial moderno
               Casco con visor reflectante
               Parches NASA + NAVIGATOR en el traje
               Diseño neutro (hombre o mujer, personalizable por color)
Vistas:        Puede verse en 3ra persona (exterior nave) y
               brazos en 1ra persona (dentro cockpit)
Animaciones:   SISTEMA EXTERNO — Mixamo
  idle_cockpit     : sentado mirando pantallas, dedos moviéndose levemente
  idle_standing    : parado, respiración leve
  walk             : caminar (para lobby/base)
  sit_down         : sentarse en silla de piloto
  reach_lever      : brazo se extiende y agarra palanca
  press_button     : dedo presiona botón
  look_radar       : inclina cabeza hacia pantalla radar
  celebrate        : levanta brazos (misión exitosa)
  death            : se desvanece / flota sin control
Notas Meshy:   Traje completo con casco. Cara no visible (visor opaco).
               Sin pelo ni expresiones faciales necesarias.
               Guantes incluidos en el modelo.
```

### PJ-02 — COMANDANTE REYES (NPC — Mission Control)
```
Descripción:   Mujer ~45 años, uniforme NASA azul oscuro
               Auriculares de comunicación
               Expresión de autoridad y confianza
               Aparece en pantalla de la cabina durante briefings
Solo necesita: Modelo estático de busto (de hombros para arriba)
               Se muestra en una pantalla, no en el mundo 3D
Animaciones:   SISTEMA EXTERNO — Mixamo (solo upper body)
  idle_talk    : movimiento sutil de cabeza, labios (simulado)
  point        : señala con dedo hacia adelante
  thumbsup     : aprobación al inicio de misión
Notas Meshy:   Solo de hombros para arriba. Detalles de cara importantes.
               Pantalla de comunicación como marco del modelo.
```

### PJ-03 — AGENTE KRONOS (antagonista NPC)
```
Descripción:   Piloto rival. Traje negro con líneas rojas
               Casco oscuro, visor rojo
               Logo de agencia privada en el hombro
               Mismo molde que El Cadete pero colores y detalles opuestos
Animaciones:   SISTEMA EXTERNO — Mixamo (reusar skeleton del Cadete)
  idle_cockpit : igual que Cadete
  taunt        : gesto de burla con la mano
  defeated     : postura de derrota
Notas Meshy:   Reusar proporciones exactas del Cadete.
               Solo cambiar materiales/colores/logos.
```

### PJ-04 — DR. NAKAMURA (NPC — científico de misión)
```
Descripción:   Hombre ~55 años, bata blanca sobre traje espacial
               Gafas. Tableta en la mano. Aspecto académico
               Aparece en pantalla durante Science Ops
Solo necesita: Modelo estático de busto
Animaciones:   Mixamo — idle_talk, point, escribir en tableta
Notas Meshy:   Solo busto. Tableta en mano izquierda visible.
```

---

## NAVES

### NV-01 — NAVE DEL JUGADOR (exterior)
```
Descripción:   Nave compacta estilo caza espacial moderno
               Dos motores traseros principales
               Alas delta cortas
               Zona de carga inferior (para instrumentos científicos)
               Cabina con cúpula de vidrio adelante
               Colores: blanco/gris con detalles cyan
Animaciones:   UNITY CODE (no Meshy)
  engine_glow  : Unity shader — luz pulsante en toberas
  wing_flex    : Unity keyframe — alas se inclinan al girar
  landing_gear : Unity keyframe — tren de aterrizaje baja/sube
Notas Meshy:   Modelo exterior completo. Las toberas como sub-mesh separado
               para que Unity aplique glow independiente.
               Sin interior — el interior es otro asset (Cockpit).
```

### NV-02 — NAVE RIVAL KRONOS (exterior)
```
Descripción:   Mismas proporciones que NV-01 pero:
               Diseño más agresivo, aristas más afiladas
               Colores negro/rojo
               Sin zona de carga científica — tiene cañones laterales
Animaciones:   UNITY CODE — igual que NV-01
Notas Meshy:   Reusar silhouette de NV-01, cambiar detalles agresivos.
```

### NV-03 — COHETE LANZADOR (intro + cinemática)
```
Descripción:   Cohete estilo Falcon 9/Starship híbrido
               Alto, delgado, logo NAVIGATOR en el costado
               Solo aparece en la secuencia de lanzamiento (intro)
Animaciones:   UNITY KEYFRAME
  launch       : cohete sube verticalmente con humo (partículas Unity)
  stage_sep    : primera etapa se separa y cae
Notas Meshy:   Puede ser modelo simple — solo se ve en cinemática corta.
               Incluir detalle de toberas inferiores.
```

### NV-04 — CÁPSULA DE SUMINISTROS (objeto flotante)
```
Descripción:   Cápsula pequeña cilíndrica con paneles solares
               Flota en el espacio. El jugador se acerca para recargar
Animaciones:   UNITY CODE — rotación lenta constante
               Unity shader — luz parpadeante de beacon
Notas Meshy:   Modelo simple. Paneles solares extendidos.
```

---

## ENTORNOS / ESTRUCTURAS

### EN-01 — COCKPIT INTERIOR (primera persona)
```
Descripción:   Interior de la cabina de piloto
               Vista desde asiento del piloto mirando hacia adelante
               Una sola pieza que incluye TODO:
                 - Asiento con correas
                 - Tablero de control principal (ver TB-01)
                 - Ventana frontal panorámica (marco incluido)
                 - Pantallas laterales de sistemas
                 - Palanca de control central
                 - Paneles laterales con switches
Animaciones:   Ver TB-01 (Tablero de Control) — las partes interactivas
               tienen sus propias animaciones dentro de este modelo
Notas Meshy:   MODELO ÚNICO — no partes separadas.
               El tablero, la ventana, el asiento = 1 solo GLB.
               Las partes interactivas como sub-meshes nombrados:
               "lever_main", "radar_screen", "btn_launch", "btn_boost"
               Unity las identifica por nombre y les aplica animación.
```

### EN-02 — TABLERO DE CONTROL (prop interactivo dentro del cockpit)
```
Descripción:   Panel de instrumentos detallado
               Incluye en UN SOLO MODELO:
               [1] Palanca de dirección (joystick central)
               [2] Pantalla de radar circular (con barrido giratorio)
               [3] Botón de lanzamiento de instrumentos (rojo grande)
               [4] Medidor de combustible (aguja analógica)
               [5] Pantalla de comunicaciones (rectangular, lateral)
               [6] Switch de boost (palanca pequeña)
Animaciones:   UNITY KEYFRAME — cada parte tiene su clip:
  lever_tilt   : palanca se inclina en cualquier dirección (joystick)
  radar_sweep  : línea de radar gira 360° continuo
  btn_press    : botón baja 5mm y vuelve (spring)
  fuel_needle  : aguja gira de 0° a 270° según combustible
  comms_blink  : pantalla parpadea al recibir mensaje
Interacción:
  Jugador toca radar    → zoom al mapa de la misión
  Jugador mueve palanca → cambia dirección de vuelo
  Jugador presiona btn  → lanza instrumento científico
Notas Meshy:   Estilo retro-futurista. Muchos botones decorativos ok.
               Sub-meshes nombrados para que Unity los identifique.
```

### EN-03 — ESTACIÓN ESPACIAL HUB (lobby/base)
```
Descripción:   Estación modular tipo ISS pero más moderna
               Módulo central grande con brazos extendidos
               Paneles solares en los extremos
               Ventanas con luz interior cálida
               Zona de atraque para naves del jugador
Animaciones:   UNITY CODE
  panel_rotate : paneles solares rotan hacia el "sol" lentamente
  dock_light   : luces de atraque parpadean (shader Unity)
Notas Meshy:   Modelo de tamaño medio. No necesita interior.
               Se ve desde afuera en AR.
```

### EN-04 — BASE EN PLANETA (objetivo final)
```
Descripción:   Pequeña base científica en superficie de planeta
               Cúpula central + antenas + rovers estacionados
               Logo NASA visible
               Aparece cuando el jugador llega al planeta
Animaciones:   UNITY CODE
  antenna_rot  : antena principal gira lentamente
  dome_light   : luces de la cúpula parpadean
Notas Meshy:   Escala pequeña — se ve desde lejos en AR.
               Detalle suficiente para reconocerse al acercarse.
```

---

## PLANETAS Y CUERPOS CELESTES

> NOTA: La rotación de planetas es UNITY CODE — giran solos con transform.Rotate()
> Meshy solo provee el mesh con textura. Sin animaciones necesarias.

### PL-01 — MERCURIO
```
Descripción:   Esfera gris/marrón con muchos cráteres
               Sin atmósfera visible
               Superficie muy texturizada
Animaciones:   Ninguna (Unity rota)
Notas Meshy:   Textura de cráteres pronunciada. Radio pequeño relativo.
```

### PL-02 — VENUS
```
Descripción:   Esfera amarillo/naranja con nubes densas en capas
               Atmósfera gruesa visible (capa exterior semitransparente)
Animaciones:   Ninguna (Unity rota)
Notas Meshy:   Capas de nubes como detalle en la textura. Color cálido.
```

### PL-03 — MARTE
```
Descripción:   Esfera roja/naranja. Casquetes polares blancos
               Cañones y volcanes visibles en la superficie
               Tenue atmósfera rojiza
Animaciones:   Ninguna (Unity rota)
Notas Meshy:   Casquetes polares importantes. Superficie muy detallada.
```

### PL-04 — JÚPITER
```
Descripción:   Esfera grande con bandas horizontales naranja/marrón/crema
               Gran Mancha Roja visible en el hemisferio sur
Animaciones:   Ninguna (Unity rota — más rápido que otros)
Notas Meshy:   Bandas de color son lo más importante. Gran Mancha Roja grande.
```

### PL-05 — SATURNO + ANILLOS
```
Descripción:   Esfera amarillo/dorado con bandas suaves
               Anillos prominentes inclinados (sub-mesh separado)
Animaciones:   UNITY CODE — anillos en plano ligeramente diferente al planeta
Notas Meshy:   IMPORTANTE: anillos como sub-mesh separado del planeta.
               Anillos semitransparentes con partículas internas.
```

### PL-06 — URANO
```
Descripción:   Esfera azul/cian uniforme, casi sin detalles
               Anillos finos y tenues (sub-mesh separado)
               Color muy característico: azul-verde pálido
Animaciones:   UNITY CODE
Notas Meshy:   Anillos como sub-mesh. Casi sin detalles superficiales.
```

### PL-07 — NEPTUNO
```
Descripción:   Esfera azul profundo intenso
               Mancha oscura visible (como la Gran Mancha Roja)
               Algo de atmósfera en los bordes
Animaciones:   Ninguna (Unity rota)
Notas Meshy:   Azul muy intenso y saturado. Mancha oscura prominente.
```

### PL-08 — AGUJERO NEGRO (objeto especial)
```
Descripción:   Disco de acreción luminoso naranja/blanco alrededor
               Centro completamente negro
               Distorsión visual alrededor (lensing)
Animaciones:   UNITY SHADER — no Meshy
               El disco y la distorsión son shaders en Unity.
               Meshy solo provee la geometría base (disco + esfera central)
Notas Meshy:   Geometría simple: esfera negra + toro (disco).
               Unity hace todo el efecto visual con shaders.
```

---

## OBJETOS DEL ESPACIO

### OB-01 — ASTEROIDE (3 variaciones de tamaño)
```
Descripción:   Roca irregular espacial
               Variación A: grande (bloquea paso)
               Variación B: mediano (campo de asteroides)
               Variación C: pequeño (lluvia de meteoritos)
Animaciones:   UNITY CODE — rotación lenta aleatoria
Notas Meshy:   Forma irregular, no perfectamente redonda. Textura rocosa.
               Los 3 tamaños pueden ser el mismo modelo escalado en Unity.
```

### OB-02 — PORTAL / AGUJERO DE GUSANO
```
Descripción:   Anillo brillante en el espacio
               Centro con efecto de distorsión visual
               Sirve como punto de viaje rápido entre zonas
Animaciones:   UNITY SHADER — el efecto interior es shader
               UNITY CODE — el anillo rota lentamente
Notas Meshy:   Solo el anillo exterior como modelo.
               Interior vacío — Unity pone el shader de distorsión.
```

### OB-03 — BALIZA DE MISIÓN (objetivo)
```
Descripción:   Objeto flotante pequeño con luz pulsante
               Forma de octaedro o diamante
               Indica punto de llegada o objetivo de misión
Animaciones:   UNITY CODE — rotación lenta
               UNITY SHADER — pulso de luz
Notas Meshy:   Modelo pequeño y simple. Forma geométrica clara.
```

### OB-04 — SATÉLITE EN ÓRBITA (objeto ambiental)
```
Descripción:   Satélite científico pequeño con paneles solares
               Flota en órbita alrededor del planeta
               Referencia visual para dar escala al planeta
Animaciones:   UNITY CODE — órbita alrededor del planeta
               UNITY CODE — paneles solares rotan hacia el sol
Notas Meshy:   Modelo simple. Paneles solares visibles.
```

### OB-05 — ESCOMBROS DE NAVE (post-colisión)
```
Descripción:   Fragmentos rotos de nave espacial
               5-6 piezas distintas: panel roto, motor quemado,
               ventana fracturada, trozo de ala
               Flotan separados
Animaciones:   UNITY CODE — cada pieza rota en dirección aleatoria
               UNITY SHADER — bordes con brillo de metal quemado
Notas Meshy:   5 piezas como modelos separados.
               Bordes irregulares, metal deformado.
```

---

## INSTRUMENTOS CIENTÍFICOS (deployables)

> Se despliegan desde la nave al llegar al planeta.
> Aparecen en la superficie o flotando cerca del planeta.

### IC-01 — CÁMARA CIENTÍFICA (en soporte)
```
Un solo modelo: cámara grande en trípode extensible
Animaciones:   UNITY KEYFRAME
  deploy       : trípode se extiende desde posición compacta
  scan         : cámara rota horizontalmente haciendo barrido
Notas Meshy:   Trípode plegable + cámara tipo telescopio en la punta.
```

### IC-02 — ANTENA DE TRANSMISIÓN
```
Un solo modelo: antena parabólica con soporte
Animaciones:   UNITY KEYFRAME
  deploy       : antena se despliega desde posición cerrada
  track        : antena apunta hacia la Tierra (rota lentamente)
Notas Meshy:   Plato parabólico + soporte + cables. Forma clásica de antena.
```

### IC-03 — MAGNETÓMETRO EN BRAZO
```
Un solo modelo: brazo extensible con sensor en la punta
Animaciones:   UNITY KEYFRAME
  extend       : brazo se extiende
  sensor_spin  : pequeño rotor en la punta gira constantemente
Notas Meshy:   Brazo articulado + sensor cilíndrico en la punta.
```

### IC-04 — PANEL DE INSTRUMENTOS CIENTÍFICOS (todos juntos)
```
Descripción:   Cuando se despliega la carga científica completa,
               aparece UN panel con todos los instrumentos integrados.
               Ahorra modelos individuales para misiones avanzadas.
Animaciones:   UNITY KEYFRAME — deploy general, antena gira
Notas Meshy:   Panel rectangular con varios instrumentos visibles.
```

---

## EFECTOS VISUALES
> ESTOS NO SON DE MESHY — son de Unity VFX Graph / Particle System
> Se listan aquí para no confundirlos con los assets de Meshy.

```
NO CREAR EN MESHY — Unity los genera por código/VFX:
  Motor exhaust     : partículas cyan/naranja detrás de la nave
  Explosión         : bola de fuego + shockwave + escombros
  Boost trail       : estela brillante al activar turbo
  Impacto escudo    : destello al recibir daño
  Warp/hyperspace   : estrellas se alargan al boost máximo
  Atmósfera planeta : shader de glow alrededor del planeta
  Polvo espacial    : partículas finas moviéndose al avanzar
  Fuego cohete      : llamas del lanzador en intro
  Nebulosa          : nube de gas de colores (shader volumétrico)
```

---

## PERSONALIZACIÓN DE NAVE

### REGLA MESHY para NV-01 y NV-02 — UV zones separadas
```
La nave DEBE tener 4 materiales separados al exportar de Meshy:
  Material 0 → fuselaje principal     (color primario)
  Material 1 → alas y detalles        (color secundario)
  Material 2 → toberas del motor      (color motor / glow)
  Material 3 → cabina / vidrio        (siempre oscuro translúcido)

Si todo viene en 1 solo material → personalización imposible.
Pedir a Meshy: "separate materials for fuselage, wings, engines, cockpit glass"
```

### Lo que el jugador puede personalizar
| Zona | Qué cambia | Ejemplo |
|---|---|---|
| Color primario | Fuselaje completo | Blanco NASA / Rojo / Negro |
| Color secundario | Alas, bordes, detalles | Cyan / Naranja / Dorado |
| Color motor | Glow de toberas | Cyan / Morado / Naranja |
| Nombre de nave | Texto en el lateral | "NAVIGATOR-07" |
| Emblema | Logo en el ala | NASA / Estrella / Tierra / Custom |
| Color traje | Astronauta coincide con nave | Mismo primary color |

### Sincronización P2P
```
Al conectarse al lobby → ShipCustomization se envía por Senda
Otros jugadores ven tu nave con TUS colores en tiempo real
Cada nave en el espacio AR tiene su identidad visual única
```

---

## PROPS DE UI / HUD
> ESTOS TAMPOCO SON DE MESHY — son Unity UI Toolkit / Canvas
> Se listan para claridad.

```
NO CREAR EN MESHY — Unity UI Canvas:
  HUD velocidad     : gauge circular
  HUD combustible   : barra
  HUD distancia     : número + flecha dirección
  Minimapa          : vista top-down de la misión
  Chat overlay      : burbujas de texto
  Mission Design UI : paneles de presupuesto/masa/delta-V
  Lobby UI          : lista de jugadores + QR code
  Science Ops UI    : barras de progreso instrumentos
```

---

## RESUMEN — QUÉ TIENES QUE CREAR EN MESHY

### PERSONAJES (4 modelos)
| # | Nombre | Animación necesaria | Fuente animación |
|---|--------|-------------------|-----------------|
| PJ-01 | El Cadete (jugador) | Sí — 8 clips | Mixamo |
| PJ-02 | Comandante Reyes | Sí — 3 clips (solo busto) | Mixamo |
| PJ-03 | Agente KRONOS | Sí — 3 clips | Mixamo (reusar skeleton) |
| PJ-04 | Dr. Nakamura | Sí — 2 clips (solo busto) | Mixamo |

### NAVES (4 modelos)
| # | Nombre | Animación necesaria | Fuente animación |
|---|--------|-------------------|-----------------|
| NV-01 | Nave jugador | Motor glow, landing gear | Unity code/keyframe |
| NV-02 | Nave KRONOS | Igual que NV-01 | Unity code/keyframe |
| NV-03 | Cohete lanzador | Launch, separación | Unity keyframe |
| NV-04 | Cápsula suministros | Rotación lenta | Unity code |

### ENTORNOS (4 modelos)
| # | Nombre | Animación necesaria | Fuente animación |
|---|--------|-------------------|-----------------|
| EN-01 | Cockpit interior | Ver tablero TB-01 | Unity keyframe |
| EN-02 | Tablero de control | 5 animaciones interactivas | Unity keyframe |
| EN-03 | Estación espacial | Paneles solares rotan | Unity code |
| EN-04 | Base en planeta | Antena rota | Unity code |

### PLANETAS (8 modelos)
| # | Nombre | Animación necesaria | Fuente animación |
|---|--------|-------------------|-----------------|
| PL-01 | Mercurio | No | — |
| PL-02 | Venus | No | — |
| PL-03 | Marte | No | — |
| PL-04 | Júpiter | No | — |
| PL-05 | Saturno + Anillos | Anillos sub-mesh | Unity code |
| PL-06 | Urano + Anillos | Anillos sub-mesh | Unity code |
| PL-07 | Neptuno | No | — |
| PL-08 | Agujero Negro | Shader — no Meshy | Unity shader |

### OBJETOS ESPACIO (5 modelos)
| # | Nombre | Animación necesaria | Fuente animación |
|---|--------|-------------------|-----------------|
| OB-01 | Asteroide (x3 tamaños) | Rotación | Unity code |
| OB-02 | Portal/Agujero gusano | Shader interior | Unity shader |
| OB-03 | Baliza de misión | Rotación + pulso | Unity code |
| OB-04 | Satélite en órbita | Órbita + paneles | Unity code |
| OB-05 | Escombros nave (x5 piezas) | Rotación caos | Unity code |

### INSTRUMENTOS CIENTÍFICOS (4 modelos)
| # | Nombre | Animación necesaria | Fuente animación |
|---|--------|-------------------|-----------------|
| IC-01 | Cámara científica | Deploy + scan | Unity keyframe |
| IC-02 | Antena transmisión | Deploy + track | Unity keyframe |
| IC-03 | Magnetómetro | Deploy + spin | Unity keyframe |
| IC-04 | Panel científico completo | Deploy general | Unity keyframe |

---

## TOTAL A CREAR EN MESHY

```
Personajes:             4 modelos
Naves:                  4 modelos
Entornos/estructuras:   4 modelos
Planetas:               7 modelos (agujero negro = shader Unity)
Objetos espacio:        5 modelos (+ 5 piezas escombros)
Instrumentos:           4 modelos
─────────────────────────────────
TOTAL:                  28 modelos GLB/FBX

De esos 28:
  Con animación Mixamo:    4  (personajes)
  Con animación Unity:     17 (mecánicos + código)
  Sin animación:           7  (planetas base)
```

---

## ORDEN RECOMENDADO DE CREACIÓN

```
PRIORIDAD 1 — Sin esto no hay juego:
  1. NV-01  Nave del jugador
  2. EN-01  Cockpit interior
  3. PL-03  Marte (planeta más usado en demos)
  4. PJ-01  El Cadete

PRIORIDAD 2 — Para completar la experiencia:
  5. NV-02  Nave KRONOS
  6. PL-04  Júpiter
  7. PL-05  Saturno
  8. OB-01  Asteroide
  9. EN-03  Estación espacial
  10. IC-02 Antena transmisión

PRIORIDAD 3 — Para polish y contenido:
  11-28: resto de planetas, personajes NPC,
         instrumentos, objetos ambientales
```

---

*Lista definida por SAL / Guacalita Inc.*
*Implementación Unity: Claude (asistente)*
