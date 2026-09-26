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
| Objetivos de misión | Destino (7 planetas) + objetivo (sobrevuelo / órbita / aterrizaje) | `MissionDesignUI` |
| Diseño de nave | Motor, combustible, energía, antena, lanzador, instrumentos | `MissionDesign` |
| Instrumentos científicos | 8 instrumentos con masa, potencia, costo y puntos | `MissionDesign`, `ScienceOpsUI` |
| Vehículos de lanzamiento | Falcon 9 / Falcon Heavy / SLS / Starship limitan la masa | `LaunchCapacityKg` |
| Presupuesto | Límite de $1,000M | `IsBudgetOk` |
| Energía | Solar (ley del cuadrado inverso) / RTG / reactor | `EffectivePowerW` |
| Masa | Masa húmeda ≤ capacidad del lanzador | `IsMassOk` |
| Comunicaciones | Tasa de datos por antena + retardo de señal por planeta | `AntennaDataRateKbps`, `CommDelayLabel` |
| Restricciones orbitales | ΔV de Tsiolkovsky ≥ ΔV mínimo del planeta, o no despega | `HasDeltaV` |
| Trade-offs de ingeniería | Más combustible = más masa = lanzador más caro = menos instrumentos | todo el diseño |
| Evaluar el éxito | Puntaje = llegada + ciencia + combustible + takedowns + gravity assist | `MissionScore` |
| Del concepto a la operación | Diseño → lanzamiento → vuelo AR → llegada → ciencia → resultado | `GameManager` |

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

**Misión fallida:** 0 vidas de casco o 240 s sin llegar (`MaxFlightTimeS`). Solo cuentan los takedowns.

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
public bool HasDeltaV  => DeltaVms >= MinDeltaV[TargetPlanetId];
public bool IsValid    => IsBudgetOk && IsMassOk && IsPowerOk && HasDeltaV;   // LANZAR
```

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
- El objetivo (sobrevuelo / órbita / aterrizaje) hoy es informativo: se muestra en la pantalla de ciencia.

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

### Realidad aumentada — `AR/GyroCameraRig.cs`, `AR/CameraFeedBackground.cs`
AR de 3 grados de libertad que funciona en **cualquier celular** (no requiere ARCore/ARKit):
- La **cámara trasera** se dibuja detrás de todo (`WebCamTexture`, maneja permisos, rotación y espejo).
- La **cámara virtual gira igual que el celular** (giroscopio) y persigue a la nave desde 7 u atrás y 1.6 u arriba.
- Con GPS, `ARCompassTracker` pide a **NASA JPL Horizons** el azimut y la altura reales del planeta y
  alinea el "adelante" del juego con el **norte verdadero** (brújula): el planeta está donde está en el cielo.
- Sin GPS o sin internet: el planeta aparece al frente, 20° arriba.
- El HUD muestra una flecha hacia el planeta cuando no está en pantalla.

---

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

## 8. Ciencia en el planeta — `UI/ScienceOpsUI.cs`

- Cada instrumento escanea durante su tiempo (tabla de la sección 3) y al terminar suma sus puntos.
- **Restricción de energía en vivo:** un instrumento solo avanza si queda energía para él + el bus (80 W).
  Con poca energía los instrumentos esperan su turno → el diseño de energía se nota aquí.
- La pantalla muestra planeta, posición de llegada, objetivo, retardo de señal, gravedad superficial y atmósfera.
- **COMPLETAR MISIÓN** se habilita cuando todos los instrumentos terminan.

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
Misión fallida: solo `TakedownCount × 150` (`GameManager.FinishMission`).

### Máximo teórico
| Componente | Máximo |
|---|---|
| Llegar primero | 1,500 |
| Ciencia (los 8 instrumentos) | 10,600 |
| Combustible al 100 % | 500 |
| 3 takedowns | 450 |
| Gravity assist | 200 |
| **Total** | **~13,250** |

Los 8 instrumentos caben en un diseño real: químico + RTG + antena media + Falcon Heavy = $795M,
6,110 kg, 345 W de 500 W, ΔV 5,886 m/s (alcanza Marte).

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
| Assign Models | Solo reasigna los GLB (prefiere las versiones `_FINAL`). |

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
- Aún sin detector: near-miss con la nave rival. Sin efecto en el juego: el objetivo de misión y el retardo de señal (solo informativos).

---

*Guacalita Inc. — Navigator 2.0 — NASA Space Apps Challenge 2026*
