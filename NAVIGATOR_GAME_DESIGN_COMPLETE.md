# NAVIGATOR 2.0 — Documento de Diseño del Juego
**Guacalita Inc. — NASA Space Apps Challenge 2026**
**Plataforma: Unity 6 (6000.6) · URP · AR por cámara + giroscopio · Senda P2P**
**Validado contra el código: 2026-09-26**

> Este documento es la **fuente de verdad**. Cada número de aquí sale del código en
> `unity_project/NavigatorUnity/Assets/`. Si cambias un valor en el código, cámbialo aquí.
> Los documentos anteriores (`NAVIGATOR_BURNOUT_P2P.md`, `NAVIGATOR_FISICA_REAL.md`,
> `NAVIGATOR_RACING_MECHANICS.md`, `NAVIGATOR_UNITY_PLAN.md`) son notas de diseño históricas.

---

## 1. Concepto

> **"Mario Kart con física de la NASA, en tu sala, contra alguien al otro lado del mundo, sin servidor."**

Dos jugadores diseñan una sonda espacial con restricciones reales (presupuesto, masa, energía,
ΔV de Tsiolkovsky), la pilotan en realidad aumentada con el celular, compiten por llegar primero
al planeta, se chocan estilo Burnout y, al llegar, operan instrumentos científicos para sumar puntos.

### Respuesta al reto NASA Space Apps

| Requisito del reto | Cómo lo resuelve Navigator | Código |
|---|---|---|
| Objetivos de misión | Destino (7 planetas) + sobrevuelo / órbita / aterrizaje: cada uno cuesta ΔV y multiplica la ciencia | `RequiredDeltaV`, `ScienceMultiplier` |
| Diseño de nave | Motor, combustible, energía, antena, lanzador, instrumentos | `MissionDesign` |
| Instrumentos científicos | 8 instrumentos; cada uno vale ×0.3 a ×2 según el planeta (radar en Venus, magnetómetro en Júpiter…) | `PlanetScienceMult` |
| Vehículos de lanzamiento | Falcon 9 / Falcon Heavy / SLS / Starship limitan la masa | `LaunchCapacityKg` |
| Presupuesto | Límite de $1,000M | `IsBudgetOk` |
| Energía | Solar (ley del cuadrado inverso) / RTG / reactor | `EffectivePowerW` |
| Masa | Masa húmeda ≤ capacidad del lanzador | `IsMassOk` |
| Comunicaciones | Los datos solo puntúan si la antena los envía a la Tierra antes de que cierre la ventana; la luz tarda según la distancia | `ScienceOpsUI` |
| Restricciones orbitales | ΔV de Tsiolkovsky ≥ ΔV mínimo del planeta, o no despega | `HasDeltaV` |
| Trade-offs de ingeniería | Más combustible = más masa = lanzador más caro = menos instrumentos | todo el diseño |
| Evaluar el éxito | Puntaje + **informe de misión** que explica cómo cada decisión (ΔV, energía, antena, objetivo, presupuesto) afectó el resultado, y qué misión real de la NASA se parece a la tuya | `MissionReport` |
| Del concepto a la operación | Briefing → diseño → lanzamiento → **operación en crucero con anomalías reales** → llegada → ciencia → informe | `TutorialUI`, `MissionEventsUI` |
| Gestionar recursos limitados | En crucero decides con combustible, energía, tiempo e instrumentos: tormenta solar, falla, apagón, error de navegación | `MissionEventsUI` |

### Diferenciador
```
Otros equipos: sliders en pantalla → simulación 2D
Navigator:     la misión vuela en tu cuarto (AR) y el planeta está en su dirección real del cielo
               + rival real conectado P2P sin servidor (Senda / Iroh)
               + posiciones reales de NASA JPL Horizons
               + la ecuación de Tsiolkovsky decide si puedes despegar y cuánto puedes acelerar
```

---

## 2. Flujo del juego — `UI/GameManager.cs`

```
LOBBY ──────────► DISEÑO ─────────► CUENTA ATRÁS ──► VUELO ─────────► CIENCIA ───────► SCOREBOARD
QR P2P o          MissionDesignUI   3·2·1·¡DESPEGUE!  HUD + AR          ScienceOpsUI      ScoreboardUI
"Jugar solo"      botón LANZAR      (planeta se       llega al planeta  "COMPLETAR        "Jugar otra vez"
                  (solo si válido)  ubica aquí)       │                  MISIÓN"           vuelve a DISEÑO
                                                      └─ nave destruida / tiempo agotado ─► SCOREBOARD (fallida)
```

| Estado | Entra con | Qué pasa |
|---|---|---|
| `Lobby` | inicio de escena | Se muestra tu QR (NodeId de Iroh). El botón dice **JUGAR SOLO** sin rival o **¡DESPEGAR!** con rival. |
| `MissionDesign` | `OnLobbyReady()` | Se enciende la cámara trasera de fondo. Diseñas la nave. **LANZAR** solo se activa si el diseño es válido. |
| `Countdown` | `OnLaunchConfirmed()` | Se coloca el planeta (dirección de respaldo al instante; NASA Horizons la refina si hay GPS). 4 s. |
| `InFlight` | `StartFlight()` | Se fija la ruta, se reparten pods y asteroides, se activan los controles. |
| `ScienceOps` | llegada | Distancia al planeta < radio (60) + 40 unidades. Se anuncia la llegada al rival. |
| `Scoreboard` | `OnMissionComplete()` o misión fallida | Se envía tu puntaje. Si el rival sigue jugando, muestra "Esperando al rival…". |

**Misión fallida:** 0 vidas de casco, varado sin combustible, o 240 s sin llegar (`MaxFlightTimeS`). Solo cuentan los takedowns.

---

## 3. Diseño de misión — `Core/MissionDesign.cs`

### Nave base
- Bus de la nave: **500 kg**, consume **80 W**
- Presupuesto máximo: **$1,000M**
- Combustible: slider de 100 a 5,000 kg; cuesta $1M por cada 1,000 kg

### Planetas

| Planeta | ΔV mínimo | Distancia al Sol | Retardo de señal (ida) | Eficiencia solar | Viaje en juego* |
|---|---|---|---|---|---|
| Mercurio | 10,900 m/s | 0.39 AU | 3–13 min | 657 % | 1,434 u |
| Venus | 6,500 m/s | 0.72 AU | 2–14 min | 193 % | 1,364 u |
| Marte | 5,600 m/s | 1.52 AU | 3–22 min | 43 % | 1,416 u |
| Júpiter | 9,100 m/s | 5.20 AU | 35–52 min | 3.7 % | 1,815 u |
| Saturno | 10,400 m/s | 9.54 AU | 68–84 min | 1.1 % | 2,077 u |
| Urano | 15,900 m/s | 19.19 AU | 156–172 min | 0.27 % | 2,480 u |
| Neptuno | 16,000 m/s | 30.07 AU | 245–260 min | 0.11 % | 2,818 u |

\* Viaje = `1200 + 300 × √(distancia Tierra–planeta en AU)`. Sin GPS se usa la distancia media
`|AU − 1|`; con NASA Horizons se usa la distancia real de hoy (`ARSpaceManager`).

### Motores

| Motor | Isp | Masa | Costo | Empuje | Referencia NASA |
|---|---|---|---|---|---|
| Químico | 450 s | 850 kg | $80M | ×1.00 | Space Shuttle, SLS |
| Iónico | 3,000 s | 220 kg | $140M | ×0.55 | Sonda Dawn |
| Nuclear | 900 s | 1,200 kg | $320M | ×1.40 | Programa NERVA |

### Energía

| Sistema | Potencia nominal | Masa | Costo | Nota |
|---|---|---|---|---|
| Solar | 280 W × (1 AU / d)² | 180 kg | $40M | Inútil más allá de Marte (aviso < 5 %) |
| RTG | 500 W constantes | 55 kg | $110M | Voyager, Cassini, New Horizons |
| Reactor | 1,200 W constantes | 350 kg | $250M | Kilopower |

### Antenas

| Antena | Datos | Masa | Costo |
|---|---|---|---|
| Baja ganancia | 0.5 kbps | 8 kg | $15M |
| Media ganancia | 100 kbps | 35 kg | $55M |
| Alta ganancia | 6 Mbps | 95 kg | $120M |

### Lanzadores

| Lanzador | Capacidad | Costo |
|---|---|---|
| Falcon 9 | 3,500 kg | $67M |
| Falcon Heavy | 8,000 kg | $150M |
| SLS | 22,000 kg | $2,000M (excede el presupuesto solo) |
| Starship | 100,000 kg | $100M |

### Instrumentos científicos

| Instrumento | Masa | Potencia | Costo | Puntos | Escaneo |
|---|---|---|---|---|---|
| Cámara gran angular | 12 kg | 15 W | $25M | 800 | 8 s |
| Cámara teleobjetivo | 18 kg | 25 W | $45M | 1,200 | 12 s |
| Espectrómetro UV | 28 kg | 40 W | $70M | 1,800 | 20 s |
| Mapeador infrarrojo | 35 kg | 60 W | $85M | 2,200 | 25 s |
| Magnetómetro | 8 kg | 5 W | $30M | 1,000 | 6 s |
| Detector de partículas | 22 kg | 30 W | $55M | 1,400 | 18 s |
| Radio ciencia | 5 kg | 10 W | $20M | 600 | 5 s |
| Radar altímetro | 42 kg | 80 W | $65M | 1,600 | 30 s |
| **Los 8** | **170 kg** | **265 W** | **$395M** | **10,600** | |

### Las ecuaciones (código real)
```csharp
public float DryMassKg => SpacecraftBusKg + EngineMassKg[Engine] + PowerMassKg[Power]
                        + AntennaMassKg[Antenna] + Instruments.Sum(i => InstrumentMassKg[i]);
public float WetMassKg => DryMassKg + FuelMassKg;

// Tsiolkovsky: ΔV = Isp × g₀ × ln(m_húmeda / m_seca)
public float DeltaVms => EngineIspS[Engine] * 9.80665f * Mathf.Log(WetMassKg / DryMassKg);

// Solar: ley del cuadrado inverso
public float EffectivePowerW => Power == PowerSystem.Solar
    ? PowerOutputW[Power] * (1f / (au * au))
    : PowerOutputW[Power];

public bool IsBudgetOk => TotalCostM <= 1000f;
public bool IsMassOk   => WetMassKg  <= LaunchCapacityKg[LaunchVeh];
public bool IsPowerOk  => BusPowerW + Σ InstrumentPowerW <= EffectivePowerW;
public float RequiredDeltaV => CruiseDeltaV + InsertionDeltaV + LandingDeltaV;
public bool  HasDeltaV      => DeltaVms >= RequiredDeltaV;
public bool  IsObjectiveOk  => !IsLanding || CanLandOnTarget;       // sin aterrizar en gigantes gaseosos
public bool  IsValid        => IsBudgetOk && IsMassOk && IsPowerOk && HasDeltaV && IsObjectiveOk;   // LANZAR
```

### Objetivos de misión
| Objetivo | ΔV extra | Masa / costo extra | Ciencia |
|---|---|---|---|
| Sobrevuelo | — | — | ×1.0 |
| Órbita | inserción orbital | — | ×1.5 |
| Aterrizaje | inserción + descenso | módulo de 250 kg y $150M | ×2.0 |

| Planeta | Inserción orbital | Descenso | Misiones reales (sobrevuelo / órbita / aterrizaje) |
|---|---|---|---|
| Mercurio | 900 m/s | 3,000 m/s (sin atmósfera: todo con cohete) | Mariner 10 / MESSENGER / ninguna |
| Venus | 1,000 m/s | 600 m/s (atmósfera densa frena) | Mariner 2 / Magellan / Venera 13 |
| Marte | 1,000 m/s | 900 m/s (escudo + paracaídas) | Mariner 4 / MRO / Perseverance |
| Júpiter | 600 m/s | sin superficie | Pioneer 10 / Juno / — |
| Saturno | 650 m/s | sin superficie | Voyager 1 / Cassini / — |
| Urano | 1,500 m/s | sin superficie | Voyager 2 / propuesta NASA / — |
| Neptuno | 1,800 m/s | sin superficie | Voyager 2 / propuesta NASA / — |

Inserciones reales de referencia: Juno 542 m/s, Cassini 626 m/s, MRO ~1,000 m/s, MESSENGER ~860 m/s.

### Diseño por defecto (Marte, válido desde el inicio)
Químico · 4,500 kg de combustible · Solar · antena media · Falcon Heavy · cámara gran angular · sobrevuelo
→ masa seca 1,577 kg · ΔV **5,953 m/s** (≥ 5,600) · costo $354.5M · energía 95 W de 121 W disponibles.

### Los trade-offs que el jugador descubre
- **Venus con motor químico** solo llega aligerando la nave: con solar y antena media se queda en
  ~6,300 m/s (< 6,500); con RTG + antena baja + 1 instrumento alcanza ~6,650 m/s.
- **Urano y Neptuno (≥ 15,900 m/s)** solo con motor iónico; el nuclear se queda en 11,300–11,900 m/s.
- **Solar en Júpiter o más lejos** entrega 10 W o menos: no alcanza ni para el bus. RTG o reactor.
- **SLS** cuesta $2,000M: nunca cabe en el presupuesto. Starship es la opción para naves pesadas.
- **Iónico** tiene muchísimo ΔV pero empuje ×0.55: la nave es más lenta y el rival te puede ganar.
- **Aterrizar en Marte** exige 7,500 m/s y 250 kg más: con motor químico no alcanza; el nuclear sí, pero
  se come el presupuesto. El iónico + Starship es la jugada fina.
- **Antena de baja ganancia** ahorra $40M y 27 kg, pero envía 4× más lento: con muchos instrumentos,
  los datos no llegan antes de que cierre la ventana y esos puntos se pierden.

---

### Valor científico según el planeta
Cada instrumento se multiplica según lo que realmente vale en ese mundo (×0.3 inútil, ×1 normal, ×2 clave):

| Planeta | Instrumentos clave (×2) | Casi inútiles | Misión real que lo mostró |
|---|---|---|---|
| Mercurio | Magnetómetro | — | MESSENGER (campo magnético inesperado) |
| Venus | Radar, espectrómetro UV | Cámaras ×0.3 | Magellan (radar bajo las nubes) |
| Marte | Cámara teleobjetivo, radar | — | MRO (HiRISE, SHARAD) |
| Júpiter | Magnetómetro, detector de partículas | Radar ×0.3 | Juno |
| Saturno | Radio ciencia | — | Cassini (ocultaciones de anillos) |
| Urano | Magnetómetro | Radar ×0.3 | Voyager 2 |
| Neptuno | Magnetómetro | Radar ×0.3 | Voyager 2 (géiseres de Tritón) |

Puntos de un instrumento = base × valor en el planeta × multiplicador del objetivo. La pantalla de diseño
muestra el ×N de cada instrumento en verde o rojo, y el informe te dice qué instrumento clave te faltó.

### Briefing de la primera misión — `UI/TutorialUI.cs`
8 tarjetas que explican ΔV y Tsiolkovsky, motores (Isp), energía solar vs RTG, ciencia por planeta,
comunicaciones, objetivos y operación. Aparece la primera vez; el botón **?** del diseño lo reabre.

---

## 4. Vuelo

### Controles — `AR/ARShipController.cs`
| Celular | Editor | Acción |
|---|---|---|
| Apuntar el celular | clic derecho + arrastrar, flechas | La nave gira hacia donde miras |
| 1 dedo en pantalla | clic izquierdo o W | Empuje |
| 2 dedos o doble toque | Espacio | Boost (tope de velocidad ×1.5, gasta ×2.2) |

### Física — `Core/PhysicsEngine.cs`, `Game/ShipController.cs`
```
Empuje:     40 u/s² × multiplicador del motor (×2.2 con boost)
Arrastre:   0.5 (arcade: si sueltas, frenas → hay que seguir empujando)
Velocidad:  tope 80 u/s · ×1.5 con boost · ×2.5 en modo Burnout
Gravedad:   a = μ / r² hacia el planeta (μ Marte = 190,000; Júpiter = 1,265,000)
Llegada:    a menos de radio del planeta (60) + 40 unidades
```

### Combustible — `Game/FuelSystem.cs`
El ΔV del diseño se convierte en **segundos de empuje**:
```csharp
BurnTimeS = 45 s × clamp(ΔV_diseño / ΔV_mínimo, 0.25, 4)
consumo por segundo = empuje / BurnTimeS × (2.2 si boost)
```
Diseñar justo al mínimo te da 45 s de empuje a fondo; el doble de ΔV, 90 s. **Sin combustible no hay
empuje** (`ShipController` lo bloquea): la nave frena por el arrastre y la misión falla por tiempo.

### Motor visible — `Game/ThrusterFX.cs`
Llama, estela y luz en la tobera: **cian** al acelerar, **naranja** en boost, **blanca** en Burnout.
Con menos del 15 % de combustible el motor **tose** (la llama se corta a ratos) y sin combustible se apaga
y el HUD muestra **SIN COMBUSTIBLE**. Las naves rivales muestran su llama con los datos que llegan por red.

### Efectos y sonido
| Efecto | Qué comunica | Código |
|---|---|---|
| Polvo espacial que se estira en líneas | Velocidad (el polvo queda fijo en el espacio) | `SpaceDustFX` |
| Campo de visión que se abre | Velocidad, boost (+6°) y Burnout (+12°) | `GyroCameraRig` |
| Burbuja de energía azul / naranja | Escudo o invulnerabilidad / Burnout | `ShieldFX` |
| Viñeta naranja pulsante / roja | Burnout activo / casco crítico | `CockpitHUD.Vignette` |
| Ráfaga de partículas + aviso "+15% FUEL" | Recoger un pod | `PickupBurst`, `Powerup.Collected` |
| Sacudida de cámara | Golpes, cuasi-colisiones | `GyroCameraRig.Shake` |
| Sonido sintetizado (sin archivos) | Motor que sube con el empuje, explosiones, golpes, pods, alerta de anomalía, cuasi-colisión, Burnout, cuenta regresiva | `AudioFX` |

### Realidad aumentada — `AR/GyroCameraRig.cs`, `AR/CameraFeedBackground.cs`
AR de 3 grados de libertad que funciona en **cualquier celular** (no requiere ARCore/ARKit):
- La **cámara trasera** se dibuja detrás de todo (`WebCamTexture`, maneja permisos, rotación y espejo).
- La **cámara virtual gira igual que el celular** (giroscopio) y persigue a la nave desde 7 u atrás y 1.6 u arriba.
- Con GPS, `ARCompassTracker` pide a **NASA JPL Horizons** el azimut y la altura reales del planeta y
  alinea el "adelante" del juego con el **norte verdadero** (brújula): el planeta está donde está en el cielo.
- Sin GPS o sin internet: el planeta aparece al frente, 20° arriba.
- El HUD muestra una flecha hacia el planeta cuando no está en pantalla.

---

### Operación en crucero — `UI/MissionEventsUI.cs`
Dos anomalías por vuelo (al 30 % y 65 % del camino). Aparecen como **banner arriba de la pantalla sin
pausar el juego** (en multijugador los rivales siguen volando, así que decidir rápido importa). Tienes 12 s;
si no decides, control de misión elige la opción segura. "Modo seguro" y "esperar el enlace" **apagan los
motores 5 s**: cuestan distancia real frente a los rivales. **Tu diseño decide qué tan grave es cada una:**

| Evento | Opción A | Opción B | El diseño que te salva |
|---|---|---|---|
| Tormenta solar | Modo seguro: motores apagados 5 s | Seguir: 50 % de perder un instrumento | Detector de partículas: la mides, +50 % de sus puntos |
| Campo de escombros | Maniobra: −8 % combustible | Atravesar: 60 % de −1 casco y −25 % | Margen de combustible |
| Falla de instrumento | Reiniciar (necesita 20 W libres) | Apagarlo: se pierde | Margen de energía (RTG / reactor) |
| Error de navegación | Corregir ya: −5 % | Corregir tarde: −15 % | Lección real: las TCM tempranas son baratas |
| Apagón (conjunción solar) | Esperar: motores apagados 5 s | Piloto automático: 40 % de percance | Antena de alta ganancia: sin demora |
| Paneles debilitándose (solar lejos del Sol) | Apagar el instrumento que más consume | −30 W para la ciencia | Elegir RTG en vez de solar |

Cada decisión y su consecuencia aparece en el informe final.

## 5. Burnout — `Game/BurnoutSystem.cs`

### Cómo se llena la barra
| Acción | Carga | Dónde se detecta |
|---|---|---|
| Near-miss con asteroide | +8 % × combo | `AsteroidField` (pasar a < radio + 7 u sin tocar) |
| Near-miss con nave rival | +12 % × combo | método disponible, aún sin detector |
| Takedown frontal / lateral / trasero | +35 % / +25 % / +20 % | `CollisionSystem` |
| Takedown en cadena | +100 % (Burnout inmediato) | `CollisionSystem` |
| Gravity assist (pod) | +20 % | `Powerup` |
| Volar con < 5 % de combustible | +5 % por segundo | `ShipController` |
| Volar en boost | +0.8 % por segundo | `ShipController` |
| Pod Boost | barra al 100 % | `Powerup` |

Combo de near-miss: ×1.5 desde 3, ×2 desde 5, ×3 desde 10 (se reinicia tras 3 s sin near-miss).
La barra se descarga 2 % por segundo si no arriesgas.

### Modo Burnout (barra al 100 %)
- Dura **10 s**
- Tope de velocidad **×2.5**
- **Inmune al daño** de choques y asteroides
- EMP del rival lo cancela y bloquea la carga durante 5 s (`SuppressFor`)

---

## 6. Colisiones — `Game/CollisionSystem.cs`

### Conservación del momento (choque elástico)
```csharp
Vector3 vf1 = ((m1 - m2) * v1 + 2f * m2 * v2) / (m1 + m2);
Vector3 vf2 = ((m2 - m1) * v2 + 2f * m1 * v1) / (m1 + m2);
```
La masa es la masa real del diseño (seca + combustible): **la nave más pesada gana el choque**.

### Daño
| Severidad | Velocidad relativa | Efecto |
|---|---|---|
| Leve | < 20 u/s | Solo rebote |
| Media | 20–50 u/s | −10 % de combustible |
| Fuerte | ≥ 50 u/s | −1 casco y −25 % de combustible |

- Casco: 3 → 2 → 1 → destruida (misión fallida). 3 s de invulnerabilidad tras cada golpe.
- Chocar con un asteroide cuenta como daño medio.

### Choque entre jugadores (multijugador)
Cada celular simula solo su propia nave. Cuando dos naves chocan, **solo el jugador con el NodeId menor**
resuelve el choque (así el daño no se aplica dos veces): calcula el rebote de ambos y envía al rival un
`shipEvent` con `"hit"` (severidad) y su nueva velocidad. El rival aplica el daño, la fuga de combustible
y el rebote en su nave. Ambos ven la explosión, la sacudida de cámara y el aviso en el HUD
(ROCE / IMPACTO −10 % / GOLPE FUERTE −1 casco / TAKEDOWN / ESCUDO).

### Takedown
```csharp
bool isTakedown = severity == CollisionSeverity.Heavy && m1 > m2 * 2f;
if (isTakedown) _burnout?.RegisterTakedown(DetermineTakedownType(v1, v2));
ExplosionVFX.Spawn(ExplosionPrefab, impactPoint, isTakedown);
```
Tipo según el ángulo entre velocidades: frontal (de frente), trasero (misma dirección), lateral (resto).

---

## 7. Espacio: pods y asteroides

### Pods — `Game/PowerupSystem.cs`
8 pods entre el 15 % y el 85 % de la ruta, desviados hasta 60 u. Reaparecen 12 s después de recogerlos.
Secuencia: combustible, boost, combustible, escudo, boost, gravity assist, EMP, gravity assist.

| Pod | Color | Efecto |
|---|---|---|
| Combustible | verde | +15 % de combustible |
| Boost | naranja | Barra de Burnout al 100 % |
| Escudo | azul | Invulnerable 10 s |
| EMP | violeta | Se envía por la red: el rival pierde el Burnout y no carga durante 5 s |
| Gravity assist | amarillo | +60 u/s en tu dirección, +20 % de Burnout, +200 pts |

### Asteroides — `Game/AsteroidField.cs`
40 asteroides (modelo `OB-01_Asteroid_FINAL`) de radio 3–9 u entre el 12 % y el 88 % de la ruta.
Pasar cerca = near-miss. Tocar uno = daño medio y explosión.

---

## 8. Ciencia y comunicaciones en el planeta — `UI/ScienceOpsUI.cs`

Cada instrumento recorre una cadena; **solo suma puntos cuando sus datos llegan a la Tierra**:

```
ENERGÍA ──► ESCANEO ──► COLA ──► TRANSMISIÓN ──► VIAJE A LA LUZ ──► RECIBIDO (+pts × objetivo)
(bus 80 W   (tiempo de   (uno a     (Mb ÷ velocidad   (retardo del
 + instr.)   escaneo)     la vez)    de la antena)     planeta)
```

| Etapa | Regla | Decisión de diseño que la controla |
|---|---|---|
| Energía | Escanean en paralelo mientras bus + instrumentos activos ≤ potencia disponible; el resto espera | Sistema de energía, distancia al Sol |
| Escaneo | 5–30 s según el instrumento | Instrumentos elegidos |
| Transmisión | Un producto a la vez: baja 1.5 · media 6 · alta 30 Mb/s | Antena |
| Viaje a la luz | Marte 2.5 s · Júpiter 8.7 s · Neptuno 50.5 s (1 min real = 0.2 s) | Destino |
| Ventana | 75 s + retardo del planeta; lo que no llegó se **pierde** | Todo lo anterior |

Datos por instrumento: cámara gran angular 40 Mb, teleobjetivo 60, UV 30, infrarrojo 80, magnetómetro 5,
partículas 10, radio ciencia 2, radar 50. Las velocidades reales van de 0.5 kbps a 6 Mbps (12,000×);
en el juego se comprime la proporción para que la antena baja siga siendo jugable, pero conserva el orden.

La pantalla muestra en vivo el estado de cada instrumento (esperando energía, escaneando, enviando,
viajando, recibido, perdido), la potencia usada, la cuenta regresiva de la ventana y el puntaje.

---

## 9. Puntaje — `Core/MissionScore.cs`

```csharp
public int TotalScore()
{
    // ArrivalBonus: 1.º ×1.50 · 2.º ×1.25 · 3.º ×1.10 · otro ×1.00
    int baseScore     = Mathf.RoundToInt(1000f * ArrivalBonus(ArrivalPosition));
    int scienceBonus  = SciencePoints;
    int fuelBonus     = Mathf.RoundToInt(FuelFraction * 500f);
    int takedownBonus = TakedownCount * 150;
    int gravAssist    = GravityAssistUsed ? 200 : 0;
    return baseScore + scienceBonus + fuelBonus + takedownBonus + gravAssist;
}
```
`SciencePoints` ya incluye el multiplicador del objetivo y solo cuenta datos recibidos.
Misión fallida (casco destruido, sin combustible o tiempo agotado): solo `TakedownCount × 150`.

### Máximo teórico
| Componente | Máximo |
|---|---|
| Llegar primero | 1,500 |
| Ciencia (los 8 instrumentos, sobrevuelo) | 10,600 (×1.5 en órbita, ×2 aterrizando) |
| Combustible al 100 % | 500 |
| 3 takedowns | 450 |
| Gravity assist | 200 |
| **Total (sobrevuelo)** | **~13,250** |

Los 8 instrumentos caben en un diseño real: químico + RTG + antena media + Falcon Heavy = $795M,
6,110 kg, 345 W de 500 W, ΔV 5,886 m/s (alcanza Marte).

### Informe de misión — `UI/MissionReport.cs`
Al final, el scoreboard explica **por qué** salió así, línea por línea y con color (bien / mejorable / mal):
- **Propulsión:** ΔV diseñado vs requerido, margen, combustible al llegar o si te quedaste sin él.
- **Objetivo:** cuánto ΔV y masa costó, cuánto multiplicó la ciencia (o qué habría dado una órbita).
- **Energía:** si los instrumentos tuvieron que esperar y cuánto (y si un RTG habría ayudado).
- **Comunicaciones:** cuántos instrumentos llegaron a la Tierra y cuántos puntos se perdieron por la antena.
- **Recursos:** presupuesto y masa usados; si sobró presupuesto para más instrumentos.
- **Misión real parecida:** Mariner 4, MRO, Perseverance, Juno, Cassini, Voyager…

El botón **MEJORAR DISEÑO Y REPETIR** vuelve al diseño con tu nave anterior para iterar.

### Scoreboard — `UI/ScoreboardUI.cs`
Animación de conteo con ease-out cúbico. **Nunca declara ganador antes de tiempo:** si el rival sigue
volando muestra "Esperando al rival…" hasta recibir su `missionEnd`. En modo solo muestra
"MISIÓN CUMPLIDA" o "MISIÓN FALLIDA" con el motivo.

---

## 10. Multijugador P2P sin servidor — `Network/`

```
Jugador A  ◄──── QUIC directo (Iroh) ────►  Jugador B
            sin servidor central · el NodeId es la identidad
```

### Conexión
1. A muestra su NodeId como QR (`LobbyManager`, ZXing).
2. B lo escanea → `SendaBridge.ConnectToNode()`.
3. B envía `lobbyInfo` (su nombre). A registra a B al recibir su primer mensaje y responde con el suyo.

### Mensajes (`NavMsgType`)
| Tipo | Contenido | Frecuencia |
|---|---|---|
| `shipState` | posición, velocidad, rotación, empuje, boost, burnout, combustible, casco | 20 Hz |
| `lobbyInfo` | nombre del jugador | al conectar |
| `chatMsg` | texto | a demanda |
| `shipEvent` | avisos del sistema; **EMP** (`"emp":5`) | a demanda |
| `missionSync` | llegada al planeta (`"arrived":pos`) | 1 vez |
| `missionEnd` | puntaje final | 1 vez |

- El remitente de cada mensaje lo pone la capa nativa (no el JSON), así que no se puede suplantar.
- Ambos jugadores vuelan en el **mismo sistema de coordenadas** (salida en el origen, planeta en la
  misma dirección), así que la nave rival aparece donde realmente está en la carrera.
- La llamada nativa → C# es estática con `[MonoPInvokeCallback]` (requisito de IL2CPP en Android)
  y encola en un `ConcurrentQueue` que se vacía en el hilo principal.

---

## 10a. Campaña · Programa NASA — `Core/Campaign.cs`, `UI/CampaignUI.cs`
Seis misiones de dificultad creciente. Cada una fija destino, objetivo y **presupuesto**, y tiene metas
de puntaje para 1, 2 y 3 estrellas (● ○ ○). Con 1 estrella se desbloquea la siguiente; se guarda el récord.

| # | Misión | Destino · objetivo | Presupuesto | Metas ●/●●/●●● |
|---|---|---|---|---|
| 1 | First Light / Primera luz | Marte · sobrevuelo | $600M | 2,000 / 3,500 / 5,000 |
| 2 | Beneath the Clouds / Bajo las nubes | Venus · órbita | $800M | 4,000 / 7,000 / 10,000 |
| 3 | Red Planet Touchdown / Aterrizaje en Marte | Marte · aterrizaje | $1,000M | 5,000 / 9,000 / 13,000 |
| 4 | King of Planets / El rey de los planetas | Júpiter · órbita | $1,000M | 4,000 / 8,000 / 12,000 |
| 5 | Lord of the Rings / El señor de los anillos | Saturno · órbita | $1,100M | 4,000 / 8,000 / 12,000 |
| 6 | The Last Giant / El último gigante | Neptuno · sobrevuelo | $1,200M | 3,000 / 6,000 / 9,000 |

Cada meta se verificó con un diseño real posible (por ejemplo, Venus en órbita exige 7,500 m/s: solo con motor iónico).
Al terminar, el marcador muestra las estrellas y los botones **Reintentar** (con tu diseño) y **Siguiente misión**.

## 10c. Multijugador sin límite de jugadores
No hay tope de jugadores (pensado para un aula entera). La frecuencia de sincronización baja sola con el tamaño
de la sala (20 Hz con hasta 5 jugadores, mínimo 4 Hz) para que el tráfico total no crezca. El marcador muestra
los primeros 6 y tu lugar, y no espera más de 90 s a quien se haya ido.
Al conectarse, cada celular comparte su lista de jugadores en `lobbyInfo` y los demás se conectan a los que les falten:
una red en malla sin anfitrión. Un nuevo jugador puede escanear el QR de **cualquier** celular ya conectado.
Cada nave sale en su carril, la posición de llegada cuenta a todos los rivales y el marcador muestra la clasificación
completa, esperando el puntaje final de todos.

## 10d. ¿Dónde está esta noche? — `UI/SkyFinderUI.cs`, `Network/NasaHorizons.cs`
Desde el lobby o al terminar una misión: GPS + brújula + **NASA JPL Horizons** calculan dónde está el planeta
**en tu cielo, ahora mismo** (consulta topocéntrica con tu latitud y longitud). La cámara muestra el cielo real,
una flecha guía hasta el planeta y un aro lo marca. Dice azimut y dirección (N, NE…), altura sobre el horizonte,
distancia en UA, cuántos minutos tarda su luz, si es de día (vuelve al atardecer), si está bajo el horizonte y
cómo se ve a simple vista (Venus: la "estrella" más brillante; Neptuno: necesitas telescopio).

## 10e. Reto compartible — `Core/Challenge.cs`, `UI/ShareChallengeUI.cs`
Al terminar una misión: **COMPARTIR RETO** genera un QR y un código `NAVC1:…` (se copia para pegarlo en cualquier chat).
Quien lo recibe lo escanea con el mismo botón *Escanear QR* del lobby, o toca **PEGAR RETO**. Vuela el mismo
destino y objetivo con **lo que gastó el retador como presupuesto máximo**, y debe superar su puntaje.

## 10b. Idiomas — `Core/Loc.cs`
Todo el juego está en **inglés por defecto** y en **español**; el botón de idioma del lobby cambia
entre ambos al instante y la elección se guarda (`PlayerPrefs`). Los textos fijos de la UI usan
`LocalizedText`; los que cambian en vivo llaman a `Loc.T("clave")` / `Loc.F("clave", valores)`.
Para agregar un idioma: añadir el valor al enum `Lang` y una columna a la tabla de `Loc`.

## 11. Arquitectura Unity

```
Navigator.Core      MissionDesign, MissionScore, PhysicsEngine, ModelFit
Navigator.Network   SendaBridge, ShipSyncService, P2PChatSystem, NasaHorizons           → Core
Navigator.Game      ShipController, FuelSystem, BurnoutSystem, CollisionSystem,
                    PowerupSystem, AsteroidField, RemoteShipManager,
                    ShipCustomizer, ShipAnimator, ExplosionVFX                           → Core, Network
Navigator.AR        ARShipController, GyroCameraRig, CameraFeedBackground,
                    ARSpaceManager, ARCompassTracker, ARSessionManager                   → Core, Game, Network
Navigator.UI        GameManager, LobbyManager, MissionDesignUI, CockpitHUD,
                    ScienceOpsUI, ScoreboardUI, UIStyle, SafeAreaFitter                  → Core, Game, Network, AR
Navigator.Editor    NavigatorSceneSetup, NavigatorUIBuilder, NavigatorAssignModels      (solo Editor)
```

### Herramientas de Editor (menú **Navigator**)
| Menú | Qué hace |
|---|---|
| **Setup Scene** | Crea y conecta toda la escena, genera pods/explosión/etiqueta del rival, asigna modelos, construye la UI y agrega la escena al build. Se puede repetir sin duplicar nada. |
| Build UI | Solo reconstruye la interfaz (los 6 paneles con todos sus botones conectados). |
| Assign Models | Solo reasigna los GLB (prefiere las versiones `_FINAL`) y gira cada nave para que su eje largo mire hacia adelante. |
| Flip Ship Nose 180° | Si la nave vuela "de reversa", gira la nariz 180° y lo recuerda. |

---

## 12. Assets 3D — Meshy AI

Se importan con **glTFast** (`com.unity.cloud.gltfast` en el manifest). Sin él, Unity no carga los `.glb`.

| Carpeta | Contenido | Uso en el juego |
|---|---|---|
| `Models/Ships/` | NV-01 (jugador), NV-02 KRONOS (rival), NV-03, NV-04 | NV-01 y NV-02, ajustadas a 3 u |
| `Models/Planets/` | PL-01 Mercurio … PL-07 Neptuno | Destino, ajustado a 120 u de diámetro |
| `Models/Objects/` | OB-01 asteroide, portal, baliza, satélite, restos | OB-01 en `AsteroidField` |
| `Models/Characters/`, `Environments/`, `Instruments/` | personajes, cabina, instrumentos | aún sin uso |

---

## 13. Cómo correrlo

1. Abrir `unity_project/NavigatorUnity` con **Unity 6000.6**. Package Manager instala glTFast y lo demás.
2. **Window → TextMeshPro → Import TMP Essential Resources** (el setup lo pide si falta).
3. Abrir `Assets/Scenes/SpaceFlight.unity`.
4. **Navigator → Setup Scene** → Ctrl+S.
5. **Play** en el Editor: "JUGAR SOLO" → LANZAR → clic izquierdo para acelerar, clic derecho para mirar.
6. **Android:** File → Build Profiles → Android → Build. (IL2CPP, arm64, SDK mínimo 26; `libsenda_core.so` ya está en `Assets/Plugins/Android/libs/`.)
7. Dos celulares: uno muestra su QR, el otro toca **Escanear QR**, y ambos **¡DESPEGAR!**.

### Limitaciones conocidas
- **iOS:** falta `libsenda_core.a` (estática, `__Internal`); sin ella el build de iOS no enlaza.
- **Editor en macOS:** solo hay `senda_core.dll` (Windows); en Mac el juego funciona en modo solo.
- Aún sin detector: near-miss con la nave rival.

---

*Guacalita Inc. — Navigator 2.0 — NASA Space Apps Challenge 2026*
