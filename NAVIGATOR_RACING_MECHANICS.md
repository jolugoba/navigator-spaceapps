# NAVIGATOR 2.0 — Mecánicas de Carrera + Colisiones
**Guacalita Inc. — Mario Kart con física NASA real**
**Fecha: 2026-09-23**

> **Nota:** documento de diseño histórico. Los valores y el comportamiento actuales del juego están en
> [`NAVIGATOR_GAME_DESIGN_COMPLETE.md`](NAVIGATOR_GAME_DESIGN_COMPLETE.md), validado contra el código.


---

## CONCEPTO CENTRAL

> La misión espacial ES una carrera.
> Todos despegan al mismo tiempo desde la Tierra.
> Primero en llegar al planeta gana ventaja científica.
> Pero quemar combustible rápido = menos instrumentos activos.
> La física real ES la estrategia del juego.

**Pitch corto:**
> "Mario Kart + NASA mission planning + AR + post-quantum crypto"

---

## ESTRUCTURA DE LA CARRERA

### Fases de la carrera
```
FASE 1 — CUENTA REGRESIVA (30s)
  Todos los jugadores en lobby ven la misma cuenta regresiva en AR
  Sus naves aparecen en posición de salida en el espacio real
  Sistema de semáforo espacial: rojo → amarillo → verde → DESPEGUE

FASE 2 — CARRERA (duración según planeta)
  Marte:    ~3-5 minutos de vuelo real en AR
  Júpiter:  ~8-12 minutos
  Neptuno:  ~15-20 minutos (modo hardcore)
  
  Durante la carrera:
  - Cada jugador gestiona su combustible
  - Pueden colisionar entre sí
  - Hay asteroides de obstáculo
  - Hay pods de recursos flotando
  - Gravity assist disponible como atajo

FASE 3 — LLEGADA (science ops)
  Primero en llegar → bonus de ciencia +50%
  Segundo:           bonus de ciencia +25%
  Tercero:           bonus de ciencia +10%
  Combustible restante = puntos adicionales
  
FASE 4 — PUNTUACIÓN FINAL
  Posición llegada + Ciencia recolectada + Combustible restante
  = Score total en leaderboard
```

---

## MECÁNICA DE COLISIONES

### Tipos de colisión
```
COLISIÓN LEVE (roce)
  Velocidad relativa < 20 u/s
  → Ambas naves rebotan
  → Pérdida de velocidad 30%
  → Escombros pequeños
  → Sin daño de instrumentos

COLISIÓN MEDIA
  Velocidad relativa 20-50 u/s
  → Ambas naves rebotan con fuerza
  → Pérdida de velocidad 60%
  → 1 instrumento aleatorio desactivado
  → Fuga de combustible 10%
  → Pantalla shake + flash rojo

COLISIÓN FUERTE (impacto)
  Velocidad relativa > 50 u/s
  → Nave más lenta pierde 1 vida
  → Fuga de combustible 25%
  → 2 instrumentos desactivados
  → Nave más rápida pierde velocidad 40%
  → Explosión visual grande
  → Scombros flotan en el espacio

COLISIÓN ASTEROIDE
  Igual que colisión media/fuerte según tamaño del asteroide
  → No hay ganador — ambos pierden
```

### Estrategia de colisión
```
El jugador puede usar colisiones como estrategia:
  Acercarse a la nave líder y empujarla fuera del camino
  Pero si va demasiado rápido → daño propio también
  
  Zona de protección: 3s de invencibilidad post-colisión
  No se puede hacer spam de colisiones

Physics de colisión:
  Conservación de momento lineal (física real)
  m₁v₁ + m₂v₂ = (m₁+m₂)vf  → ambas naves intercambian velocidad
  La nave más masada (más combustible) empuja más fuerte
  LECCIÓN: nave cargada = ventaja en colisión
```

---

## POWERUPS FLOTANTES EN EL ESPACIO

```
POD-01 — COMBUSTIBLE EXTRA  ⛽
  Esfera verde flotando
  Recolectar → +15% de combustible
  Aparece en puntos fijos de la ruta

POD-02 — BOOST TEMPORAL  ⚡
  Esfera amarilla brillante
  Recolectar → boost x2 durante 8 segundos
  Exhaust cambia a dorado

POD-03 — ESCUDO  🛡️
  Esfera azul con brillo
  Recolectar → burbuja protectora 10 segundos
  Próxima colisión = sin daño ni pérdida combustible
  Burbuja visible para otros jugadores

POD-04 — EMP ESPACIAL  ⚡💥
  Esfera roja con electricidad
  Recolectar → desactiva instrumentos de nave rival más cercana 5s
  El rival ve sus pantallas parpadeando

POD-05 — GRAVITY ASSIST FORZADO  🌀
  Representación de campo gravitacional
  Recolectar → impulso automático en dirección al planeta
  Equivale a +500 m/s de ΔV gratis
```

---

## OBSTÁCULOS EN LA RUTA

```
CAMPO DE ASTEROIDES
  Zona densa de asteroides medianos y pequeños
  Hay ruta libre si mantienes velocidad controlada
  Si vas demasiado rápido → colisión inevitable
  Ruta alternativa: rodear el campo (más tiempo)

ZONA DE RADIACIÓN (Júpiter)
  Cinturón de Van Allen joviano
  Entrar sin escudo → 1 instrumento falla cada 5s
  Con escudo POD-03 → puedes cruzar seguro

TORMENTA SOLAR
  Evento aleatorio — aviso 10s antes
  Durante 15s: empuje reducido 50% para todos
  Comunicaciones interrumpidas (no chat)
  
CAMPO DE ESCOMBROS
  Restos de nave anterior (cinemática)
  Esquivar o reducir velocidad
  Los escombros tienen física real → se mueven al golpearlos
```

---

## GRAVITY ASSIST COMO ATAJO DE CARRERA

```
En rutas largas (Saturno, Urano, Neptuno):
  Hay un planeta intermedio en la ruta
  Jugador puede activar "Gravity Assist Trajectory"
  
  Ruta directa a Saturno:
    Tiempo: 8 min | ΔV requerido: alto | Sin desvío
  
  Ruta con flyby de Júpiter:
    Tiempo: +2 min de desvío | ΔV requerido: -40% | Llega igual
  
  Estrategia:
    Jugador impaciente → ruta directa, gasta más fuel
    Jugador estratega → flyby, llega con más fuel para ciencia
    
  VISUAL en AR:
    La curva de trayectoria se ve como arco brillante
    Al pasar cerca de Júpiter: nave acelera visiblemente
    Efecto de slingshot con trail especial
```

---

## SISTEMA DE DAÑO Y REPARACIÓN

```
ESTADOS DE LA NAVE:
  PERFECTA     (100%) → rendimiento completo
  DAÑADA       (60%)  → velocidad max -20%, 1 instrumento off
  MUY DAÑADA   (30%)  → velocidad max -40%, 2 instrumentos off, fuga fuel
  CRÍTICA      (10%)  → velocidad max -60%, controles inestables
  DESTRUIDA    (0%)   → fin de carrera para ese jugador

REPARACIÓN:
  Flotar quieto 5s → auto-reparación 10% por segundo
  Pero perdes posición en la carrera
  O encontrar POD de reparación en la ruta (raro)

DAÑO VISIBLE EN NAVE (Meshy + Unity):
  60%  → panel lateral suelto (animación colgando)
  30%  → humo saliendo del motor (partículas Unity)
  10%  → chispas + motor intermitente (shader parpadeante)
```

---

## NUEVO MODO: CARRERA PURA (sin misión científica)

```
Para jugadores que quieren solo la carrera:
  Sin diseño de misión previo
  Nave estándar igual para todos (fair play)
  Personalización solo cosmética
  Primero en llegar = ganador
  
  Este modo es el más accesible para nuevos jugadores
  Ideal para demos rápidas y hackathons
```

---

## ASSETS NUEVOS NECESARIOS POR MECÁNICA DE CARRERA

### Powerups (5 modelos Meshy)

**PW-01 — POD Combustible**
```
Descripción:   Esfera verde con símbolo de combustible
               Brillo suave pulsante
Animaciones:   UNITY CODE — flotación suave arriba/abajo
               UNITY SHADER — pulso de luz verde
Notas Meshy:   Esfera simple con símbolo grabado.
               Sub-mesh exterior para separar brillo.
```

**PW-02 — POD Boost**
```
Descripción:   Esfera amarilla con rayo eléctrico
               Más brillante que el combustible
Animaciones:   UNITY CODE — rotación + flotación
               UNITY SHADER — destellos eléctricos
Notas Meshy:   Esfera con rayos en relieve.
```

**PW-03 — POD Escudo**
```
Descripción:   Esfera azul semitransparente
               Hexágonos en la superficie (estilo sci-fi)
Animaciones:   UNITY CODE — rotación lenta
               UNITY SHADER — hexágonos brillan al acercarse
Notas Meshy:   Esfera con patrón hexagonal en la superficie.
```

**PW-04 — POD EMP**
```
Descripción:   Esfera roja con arcos de electricidad
               Más amenazante visualmente
Animaciones:   UNITY CODE — vibración leve + rotación
               UNITY SHADER — arcos eléctricos rojos
Notas Meshy:   Esfera con líneas de energía en relieve.
```

**PW-05 — POD Gravity Assist**
```
Descripción:   Forma de vórtice / espiral
               No es una esfera — es un campo visual
Animaciones:   UNITY CODE — rotación de espiral constante
               UNITY SHADER — efecto de distorsión gravitacional
Notas Meshy:   Forma de toro retorcido / espiral 3D.
```

---

### Estados de daño de nave — NV-01 EXPANDIDO

```
NV-01 ya modelado en Meshy.
Agregar sub-meshes adicionales:

  "panel_damaged"   → panel lateral roto (cuelga suelto)
  "smoke_emitter"   → punto de emisión de humo en motor izquierdo
  "spark_emitter"   → punto de chispas en ala derecha

  Animaciones Unity para estados de daño:
  damaged_panel  : panel oscila como péndulo
  critical_shake : nave entera vibra levemente

  Los efectos visuales (humo, chispas) son partículas Unity.
  Los sub-meshes solo marcan de DÓNDE salen.
```

---

### Línea de salida / meta (1 modelo)

**EN-05 — ARCO DE SALIDA ESPACIAL**
```
Descripción:   Dos pilares con arco entre ellos
               Luces de salida en los pilares
               Logo NAVIGATOR en el arco
               Aparece en el espacio AR al inicio de carrera
Animaciones:   UNITY KEYFRAME
  countdown     : luces cambian rojo → amarillo → verde
  launch_flash  : flash de luz al despegar
Notas Meshy:   Estructura minimalista futurista.
               Luces como sub-mesh separado para cambiar color.
```

---

### Burbuja de escudo activo (efecto en nave)

**EF-01 — ESCUDO BURBUJA**
```
No es un modelo Meshy independiente.
Es una esfera semitransparente instanciada alrededor de la nave en Unity.
  UNITY SHADER: hexágonos semitransparentes azules
  UNITY CODE: escala de 0 a 1 al activarse (aparece)
  UNITY CODE: pulso de luz al absorber un impacto
  UNITY CODE: desaparece gradualmente al expirar
```

---

## TABLA ACTUALIZADA COMPLETA DE ASSETS

### RESUMEN TOTAL CON RACING

| Categoría | Cantidad | Con animación |
|---|---|---|
| Personajes | 4 | 4 (Mixamo) |
| Naves | 4 | 4 (Unity code/keyframe) |
| Entornos/estructuras | 5 | 5 (Unity keyframe) |
| Planetas | 7 | 2 con sub-mesh (Unity code) |
| Objetos espacio | 5 | 5 (Unity code) |
| Instrumentos científicos | 4 | 4 (Unity keyframe) |
| **Powerups (NUEVO)** | **5** | **5 (Unity code/shader)** |
| **Arco de salida (NUEVO)** | **1** | **1 (Unity keyframe)** |
| **Total** | **35 modelos** | **30 con animación** |

---

## ORDEN DE CREACIÓN ACTUALIZADO

```
PRIORIDAD 1 — Sin esto no hay demo:
  1. NV-01  Nave del jugador (con zonas UV separadas)
  2. EN-01  Cockpit interior
  3. PL-03  Marte
  4. PJ-01  El Cadete
  5. PW-01  POD Combustible
  6. OB-01  Asteroide

PRIORIDAD 2 — Para la carrera completa:
  7.  NV-02  Nave KRONOS
  8.  EN-05  Arco de salida
  9.  PW-02  POD Boost
  10. PW-03  POD Escudo
  11. PW-04  POD EMP
  12. PL-04  Júpiter
  13. PL-05  Saturno

PRIORIDAD 3 — Polish y contenido completo:
  14-35: resto de planetas, NPCs, instrumentos,
         PW-05 gravity assist, objetos ambientales
```

---

## PITCH FINAL ACTUALIZADO

> NAVIGATOR es Mario Kart en el espacio real con física NASA.
>
> Diseñas tu nave con restricciones reales de ingeniería.
> La carrera ocurre en tu espacio real a través de la cámara del celular.
> Colisionas con otros jugadores, recoges powerups, esquivas asteroides.
> La física real ES la estrategia: más combustible = nave más pesada = más resistencia en colisión pero más lento.
> Todo cifrado post-cuántico entre jugadores sin servidor central.
> Primero en llegar al planeta recolecta más ciencia.
> La ciencia determina el ganador final — no solo la velocidad.

---

*Diseño y concepto: SAL / Guacalita Inc.*
*Implementación Unity: Claude (asistente)*
