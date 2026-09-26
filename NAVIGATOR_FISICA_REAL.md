# NAVIGATOR 2.0 — Física Real NASA que impacta a los jueces
**Guacalita Inc. — Lo que nos hace ganar el reto**
**Fecha: 2026-09-22**

> **Nota:** documento de diseño histórico. Los valores y el comportamiento actuales del juego están en
> [`NAVIGATOR_GAME_DESIGN_COMPLETE.md`](NAVIGATOR_GAME_DESIGN_COMPLETE.md), validado contra el código.


---

## POR QUÉ ESTO GANA

Los demás equipos harán sliders genéricos.
Nosotros mostramos física que NASA usa HOY en misiones reales.
Cada restricción del juego tiene una misión real de referencia.
Los jueces son ingenieros — van a reconocer esto inmediatamente.

---

## 1. MOTORES — TIPOS REALES CON COMBUSTIBLE REAL

### Tabla completa de propulsión

| Motor | Combustible | Isp real | Empuje | Misión real que lo usó |
|---|---|---|---|---|
| Químico LOX/RP-1 | Keroseno + Oxígeno líquido | 350s | Alto | Falcon 9, Atlas V |
| Químico LOX/LH2 | Hidrógeno + Oxígeno líquido | 450s | Alto | Space Shuttle, SLS |
| Químico LOX/CH4 | Metano + Oxígeno líquido | 380s | Alto | Starship (Raptor) |
| Iónico Xenón | Gas Xenón | 3,000s | Muy bajo | Dawn (cinturón asteroides) |
| Iónico Kriptón | Gas Kriptón | 2,500s | Muy bajo | Starlink v2 |
| Nuclear Térmico | Hidrógeno líquido (LH2) | 900s | Alto | NERVA (probado), NTP NASA |
| Nuclear Eléctrico | Xenón + Reactor nuclear | 8,000s | Mínimo | Concepto NASA para Marte |
| Vela Solar | Sin combustible | ∞ (fotones) | Mínimo | LightSail 2, IKAROS |

### Por qué el Isp importa — Tsiolkovsky visual en el juego
```
ΔV = Isp × 9.80665 × ln(masa_húmeda / masa_seca)

Ejemplo real visible en el juego:
  Motor iónico Xenón (Isp 3,000s):
    100 kg de combustible → ΔV = 3,000 × 9.8 × ln(1,100/1,000)
                          → ΔV = 2,860 m/s con solo 100 kg !!

  Motor químico LOX/RP-1 (Isp 350s):
    100 kg de combustible → ΔV = 350 × 9.8 × ln(1,100/1,000)
                          → ΔV =   333 m/s con los mismos 100 kg

CONCLUSIÓN que el jugador aprende:
  El motor iónico es 8.6x más eficiente en combustible
  PERO el empuje es tan bajo que tarda años en llegar a Júpiter
  POR ESO Dawn usó iones para el cinturón de asteroides (tiempo ok)
  POR ESO no se usa para Marte (urgencia > eficiencia)
```

### Cómo mostrarlo en el juego (feedback educativo)
```
Jugador elige motor → aparece TOOLTIP con misión real:
  "Motor iónico — Xenón
   La sonda Dawn usó este motor para llegar a Vesta y Ceres.
   Ventaja: 8x más eficiente que químico.
   Desventaja: tarda 3 años en llegar a Júpiter.
   Isp: 3,000s | Empuje: 0.09N"
```

---

## 2. POTENCIA SOLAR — DEGRADACIÓN REAL CON DISTANCIA

### La ley del cuadrado inverso
```
Potencia_disponible = Potencia_nominal × (1 AU / distancia_AU)²

Tabla real por planeta:
  Mercurio (0.39 AU) → 657% de potencia nominal  ← ojo, demasiado calor
  Venus    (0.72 AU) → 193% de potencia nominal
  Tierra   (1.00 AU) → 100% (referencia)
  Marte    (1.52 AU) →  43% de potencia nominal  ← paneles pierden más de la mitad
  Júpiter  (5.20 AU) →   3.7% ← paneles solares casi inútiles
  Saturno  (9.54 AU) →   1.1% ← paneles completamente inútiles
  Neptuno (30.07 AU) →   0.1% ← sin RTG/Reactor = misión imposible

Misiones reales:
  Juno (Júpiter): usa paneles solares ENORMES (72 m²) — solución especial
  Voyager, Cassini, New Horizons: RTG obligatorio — sin sol no hay otra opción
```

### Cómo afecta al juego
```
Si jugador elige paneles solares + Neptuno:
  Potencia disponible: 280W × 0.001 = 0.28W ← literalmente nada
  → ALERTA ROJA: "Paneles solares inviables a esta distancia"
  → "Misiones reales a Neptuno usan RTG. Recomendado: RTG o Reactor Nuclear"

Si jugador elige RTG + cualquier planeta:
  Potencia constante 500W sin importar distancia ← siempre funciona
  "Los RTG de Voyager 1 siguen generando energía a 23,000 millones de km"
```

---

## 3. VENTANAS DE LANZAMIENTO — TIMING REAL

```
No puedes lanzar cuando quieras. Los planetas se mueven.
La ventana de lanzamiento óptima ocurre cada N años:

  Marte:    cada 26 meses (ciclo sinódico)
  Júpiter:  cada 13 meses
  Saturno:  cada 12.4 meses
  Neptuno:  casi anual pero ángulo varía mucho

Si lanzas fuera de ventana:
  ΔV requerido se duplica o triplica
  Viaje puede durar 2x más tiempo
  Algunas misiones se vuelven imposibles con combustible disponible

Cómo mostrarlo en el juego:
  Calendario de ventanas visibles en Mission Design
  Contador: "Próxima ventana a Marte: 14 días"
  Penalización: lanzar fuera de ventana = +30% de combustible requerido
```

---

## 4. RESTRICCIONES ORBITALES REALES

### Delta-V por fase de la misión
```
Una misión a Marte no es solo "llegar" — son varias quemas:

  FASE 1 — Trans-Mars Injection (TMI):     ~3,600 m/s
  FASE 2 — Correcciones de trayectoria:     ~150 m/s
  FASE 3 — Mars Orbit Insertion (MOI):    ~1,000 m/s  (si quieres orbitar)
  FASE 4 — Descenso/aterrizaje:           ~1,000 m/s  (si quieres aterrizar)
  TOTAL aterrizaje en Marte:              ~5,750 m/s

El juego muestra la barra de ΔV dividida por fases:
  [TMI ████] [Correc █] [MOI ████] [Land ████] → Total: 5,750 m/s
  Tu nave tiene: 4,200 m/s ← NO ALCANZA → ajusta combustible o cambia objetivo
```

### Tipos de misión y sus costos de ΔV
```
  Flyby    = solo pasar cerca          → ΔV mínimo, poco tiempo científico
  Órbita   = quedarse girando alrededor → +1,000-3,000 m/s extra
  Aterrizaje = tocar la superficie      → +500-2,000 m/s más
  Retorno  = volver a la Tierra        → duplica el ΔV total

Misiones reales:
  New Horizons → Flyby Plutón: pasó en 9 años, 9 horas de ciencia
  Cassini      → Órbita Saturno: 13 años de datos
  Curiosity    → Aterrizaje Marte: ciencia continua indefinida
```

---

## 5. COMUNICACIONES — RESTRICCIONES REALES

### Retraso de señal (velocidad de la luz)
```
1 AU = 8.317 minutos de viaje para la señal

  Marte:    3 – 22 minutos (depende de posición orbital)
  Júpiter:  35 – 52 minutos
  Saturno:  68 – 84 minutos
  Neptuno:  245 – 260 minutos (~4 horas)

Impacto en el juego:
  Cuando el jugador hace algo en el planeta →
  "Tu comando tardó 43 minutos en llegar. Respuesta en 43 min más."
  Los jugadores aprenden por qué las sondas son autónomas.
```

### Blackouts de comunicación
```
  Conjunción solar: el Sol bloquea la señal (días a semanas)
  Ocultación: el planeta bloquea la señal (minutos a horas)
  Antena HGA: debe apuntar EXACTAMENTE a la Tierra (±0.1°)

En el juego:
  Evento aleatorio: "BLACKOUT — conjunción solar 3 días"
  La antena HGA falla si la nave maniobra bruscamente
  Antena omnidireccional: siempre funciona pero 0.5 kbps (apenas telemetría)
```

### Por qué la antena importa
```
  Antena LGA (omnidireccional):  0.5 kbps  → solo datos de salud de la nave
  Antena MGA (mediana):        100 kbps  → imágenes comprimidas pequeñas
  Antena HGA 3.7m:           6,000 kbps  → imágenes HD en tiempo real

  Voyager transmite a 160 bps desde 23,000 millones de km
  (la señal tarda 21 horas en llegar con HGA de 3.7m)

  Con antena LGA a Neptuno: 0.5 kbps × (0.001 eficiencia distancia)
  → efectivamente 0 — imposible transmitir ciencia útil
  LECCIÓN: a planetas lejanos, HGA es obligatoria
```

---

## 6. ENTORNO ESPACIAL — RESTRICCIONES QUE NASA ENFRENTA

### Radiación por zona
```
  Cinturón de Van Allen (Tierra): dañino para electrónica no blindada
  Cinturón de Júpiter: EXTREMO — el más intenso del sistema solar
    → Juno tiene blindaje especial de titanio
    → Galileo degradó en meses por la radiación
    → En el juego: instrumento no blindado falla cerca de Júpiter

Impacto en el juego:
  Zona de alta radiación detectada → instrumentos en riesgo
  Jugador debe decidir: apagar instrumentos (seguro) o datos completos (riesgo)
```

### Temperatura
```
  Mercurio: +430°C día / -180°C noche → necesita aislamiento especial
  Plutón:  -230°C → componentes electrónicos necesitan calefacción activa

  En el juego:
  Si usas paneles solares en Mercurio → sobrecalentamiento = fallo
  Si usas batería estándar en Neptuno → temperatura destruye la batería
  LECCIÓN: cada planeta tiene su "receta" de componentes correctos
```

---

## 7. ASISTENCIA GRAVITACIONAL (GRAVITY ASSIST) — MECÁNICA ESPECIAL

```
Voyager 2 llegó a Neptuno en 12 años usando 4 gravity assists
Sin gravity assists: hubiera necesitado 30+ años o combustible imposible

Cómo funciona:
  La nave pasa cerca de un planeta → roba energía orbital → acelera gratis
  El planeta frena infinitesimalmente (masa enorme, efecto insignificante)
  La nave gana velocidad sin gastar combustible

En el juego — mecánica opcional avanzada:
  Ruta directa a Saturno:    ΔV requerido 10,400 m/s
  Ruta con Jupiter flyby:    ΔV requerido  6,200 m/s  ← ahorra 4,200 m/s
  Pero tarda 2 años más

  Jugador elige: rápido y caro vs lento y eficiente
  LECCIÓN: así Voyager, Cassini, New Horizons llegaron tan lejos
```

---

## 8. FRACCIÓN DE MASA — EL CONCEPTO CLAVE

```
La "tiranía del cohete" — los ingenieros odian este problema:

  Quieres 1 kg más de carga científica
  → necesitas más combustible para cargar ese kg
  → ese combustible extra pesa más
  → necesitas más combustible para cargar ese combustible
  → y así infinito...

Tsiolkovsky lo captura en la fracción de masa:
  masa_combustible / masa_total = 1 - e^(-ΔV / (Isp × g₀))

  Para ir a Marte (ΔV = 5,600 m/s) con motor químico (Isp 450s):
    fracción = 1 - e^(-5600 / (450×9.8)) = 72%
    72% de la nave es COMBUSTIBLE — solo 28% es ciencia + estructura

  Con motor iónico (Isp 3,000s):
    fracción = 1 - e^(-5600 / (3000×9.8)) = 17%
    Solo 17% combustible — 83% disponible para ciencia !!

  ESTA es la lección más importante del reto NASA
  El juego la muestra visualmente: barra de masa = combustible vs carga útil
```

---

## 9. MISIONES REALES DE REFERENCIA POR PLANETA

```
MARTE:
  Curiosity/Perseverance: aterrizaje, química suelo, búsqueda de vida
  MAVEN: órbita, estudio atmósfera, radiación
  InSight: aterrizaje, sismología (terremotos marcianos)

JÚPITER:
  Juno: órbita polar, campo magnético, composición atmósfera
  Europa Clipper (2025): flyby de Europa, búsqueda de océano bajo el hielo
  Galileo: órbita, sonda atmosférica (destruida por presión)

SATURNO:
  Cassini: 13 años en órbita, descubrió géiseres en Encélado
  Huygens (probe): aterrizó en Titán, atmosfera densa de nitrógeno

URANO / NEPTUNO:
  Solo Voyager 2 (1986/1989) — ambos con flyby único
  Próxima misión: Uranus Orbiter and Probe (NASA, ~2030s)

MERCURIO:
  MESSENGER: órbita, mapeó la superficie completa
  BepiColombo (ESA+JAXA, en ruta): llegará 2025

En el juego: al seleccionar planeta aparece referencia real:
  "Marte — Misión de referencia: Perseverance (2020)
   Objetivo: Astrobiology, búsqueda de vida pasada
   Instrumentos: MOXIE, Mastcam-Z, SHERLOC, PIXL"
```

---

## 10. LO QUE HACE GANAR EL RETO — LISTA FINAL

```
OTROS EQUIPOS:
  ✗ Sliders genéricos sin física real
  ✗ "Motor A es mejor que Motor B" sin explicar por qué
  ✗ Sin referencia a misiones NASA reales
  ✗ Sin consecuencias físicas reales a las decisiones
  ✗ Sin multiplayer
  ✗ Sin AR

NAVIGATOR:
  ✅ Tsiolkovsky real con fracción de masa visible
  ✅ Isp real por tipo de motor y combustible
  ✅ Degradación solar por ley del cuadrado inverso
  ✅ Ventanas de lanzamiento con penalización real
  ✅ ΔV por fase (TMI + MOI + aterrizaje)
  ✅ Retraso de comunicación real por planeta (luz)
  ✅ Blackouts de comunicación
  ✅ Radiación cerca de Júpiter afecta instrumentos
  ✅ Gravity assist como mecánica de optimización
  ✅ Referencia a misión NASA real por cada planeta
  ✅ AR en celular — la misión ocurre en tu espacio real
  ✅ Multiplayer P2P post-cuántico — misión cooperativa
  ✅ Feedback educativo: "por esto Dawn usó iones, no químico"
```

---

## IMPLEMENTACIÓN EN EL JUEGO — CAMBIOS A HACER

### En MissionDesign (ya existe — expandir)
```
Agregar:
  EngineType.lox_rp1     Isp=350s  misión="Falcon 9"
  EngineType.lox_lh2     Isp=450s  misión="Space Shuttle"
  EngineType.lox_ch4     Isp=380s  misión="Starship"
  EngineType.ion_xenon   Isp=3000s misión="Dawn"
  EngineType.ion_krypton Isp=2500s misión="Starlink"
  EngineType.ntr_lh2     Isp=900s  misión="NERVA/NTP"
  EngineType.solar_sail  Isp=∞     misión="LightSail 2"

Cada motor muestra:
  - Combustible específico
  - Misión NASA de referencia
  - Por qué se usa en esa misión
```

### Nueva restricción: degradación solar
```dart
double solarEfficiency(String planetId) {
  const au = {'mercury':0.39,'venus':0.72,'mars':1.52,
               'jupiter':5.20,'saturn':9.54,'neptune':30.07};
  final d = au[planetId] ?? 1.0;
  return 1.0 / (d * d); // ley cuadrado inverso
}

// Si power == solar → powerAvailableW × solarEfficiency(targetPlanetId)
// Si power == rtg   → powerAvailableW (constante, no depende distancia)
```

### Nueva mecánica: gravity assist
```
Al seleccionar Saturno o más allá:
  Opción A: Ruta directa (más ΔV, menos tiempo)
  Opción B: Flyby de Júpiter (menos ΔV, más tiempo)
  
  El juego muestra: "Casini usó flyby de Venus×2 + Tierra + Júpiter"
```

### Tooltip educativo en cada selección
```
Cada componente seleccionado muestra:
  Nombre → Descripción técnica → Misión NASA que lo usó → Por qué
```

---

*Física validada contra datos reales de NASA JPL*
*Diseño y concepto: SAL / Guacalita Inc.*
*Implementación: Claude (asistente)*
