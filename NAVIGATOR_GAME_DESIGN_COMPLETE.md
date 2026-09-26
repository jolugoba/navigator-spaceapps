# NAVIGATOR 2.0 — Game Design Document Completo
**Guacalita Inc. — NASA Space Apps Challenge 2026**
**Plataforma: Unity 6 LTS + AR Foundation + Senda P2P**
**Actualizado: 2026-09-25**

---

## 1. CONCEPTO

> **"Mario Kart con física NASA real, en tu sala de estar, jugando contra alguien al otro lado del mundo sin servidor."**

Navigator es un juego AR multijugador donde dos jugadores diseñan naves espaciales reales
(Tsiolkovsky, presupuesto, masa, potencia), las pilotan en su espacio físico real con AR,
se dan choques Burnout-style, y despliegan instrumentos científicos al llegar al planeta.

### Respuesta al reto NASA Space Apps

| Requisito del reto | Cómo lo resuelve Navigator |
|---|---|
| Mission objectives | flyby / orbit / landing — elige en MissionDesignUI |
| Spacecraft design | motor + potencia + antena + combustible |
| Scientific instruments | 8 instrumentos con masa/potencia/puntos reales |
| Launch vehicles | Falcon 9 / Heavy / SLS / Starship limitan masa total |
| Budgets | límite $1,000M — IsBudgetOk() en MissionDesign.cs |
| Power | solar (ley cuadrado inverso) / RTG / reactor nuclear |
| Mass | masa total ≤ capacidad del lanzador seleccionado |
| Communications | kbps por antena + retardo de luz (AU × 8.317 min) |
| Orbital constraints | ΔV Tsiolkovsky vs ΔV mínimo por planeta |
| Engineering trade-offs | más motor = menos instrumentos = menos ciencia |
| Evaluate success | score = llegada + ciencia + fuel + takedowns |
| Concept to operation | diseño → AR vuelo → llegada → ciencia → resultado |

### Diferenciador
```
Otros equipos: sliders en pantalla → simulación en 2D
Navigator:     misión en AR en tu celular (la ves en tu espacio físico)
               + rival real conectado P2P sin servidor (Senda / Iroh)
               + cifrado post-cuántico Dilithium-5
               + física Tsiolkovsky + posiciones NASA Horizons reales
```

---

## 2. FLUJO COMPLETO DEL JUEGO

```
┌─────────────┐
│   LOBBY     │  Dos jugadores se conectan por QR code P2P
│  LobbyUI    │  Se muestra código QR de la sesión (ZXing)
│             │  Rival escanea → conexión Iroh directa
└──────┬──────┘
       │ OnLobbyReady()
┌──────▼──────┐
│  MISSION    │  Cada jugador diseña SU PROPIA nave
│  DESIGN     │  Planeta destino, motor, lanzador, instrumentos
│ MissionDUI  │  Gauges en tiempo real: masa / presupuesto / ΔV / potencia
└──────┬──────┘
       │ OnLaunchConfirmed()
┌──────▼──────┐
│  CUENTA     │  "3 - 2 - 1 - ¡GO!"
│ REGRESIVA   │  Ambas naves aparecen en AR
└──────┬──────┘
       │ StartFlight()
┌──────▼──────┐
│   VUELO     │  Pilotaje en AR con giroscopio
│  IN-FLIGHT  │  Combustible real, Burnout, colisiones, pods
│  HUD activo │  Brújula hacia planeta, comms delay
└──────┬──────┘
       │ EnterScienceOps() al llegar
┌──────▼──────┐
│  CIENCIA    │  Despliegue de instrumentos
│ ScienceOps  │  Cada instrumento escanea en tiempo real (6-30s)
│             │  Potencia limitada: no todos pueden operar juntos
└──────┬──────┘
       │ OnMissionComplete()
┌──────▼──────┐
│ SCOREBOARD  │  Animación reveal de scores
│ ScoreboardUI│  Local vs Rival, victoria/derrota/empate
└─────────────┘
```

---

## 3. MISIÓN — DISEÑO DE NAVE

### Archivo: `Core/MissionDesign.cs`

#### Planetas disponibles y sus datos reales
```
Planeta     ΔV mínimo   Distancia   Retardo comms   Dificultad
Mercurio    5.5 km/s    0.7 AU      5.8 min         Media
Venus       3.5 km/s    0.3 AU      2.5 min         Fácil
Marte       3.6 km/s    1.5 AU      12.5 min        Fácil (demo)
Júpiter     6.3 km/s    5.2 AU      43 min          Difícil
Saturno     7.0 km/s    9.6 AU      80 min          Difícil
Urano       7.5 km/s    19.2 AU     160 min         Experto
Neptuno     7.9 km/s    30 AU       250 min         Experto
```

#### Motores disponibles
```
Motor           Isp(s)  Empuje(kN)  Masa(kg)   Costo($M)  Tipo
IonThruster     3000    0.09        50         120        Eléctrico
ChemLox         450     1000        1200       80         Químico
NuclearThermal  900     500         2000       400        Nuclear
SolarSail       N/A     0.001       200        60         Solar
```

#### Fórmula Tsiolkovsky (en código)
```csharp
// MissionDesign.cs
public float ComputeDeltaV()
{
    float isp  = EngineIspTable[Engine];
    float g0   = 9.80665f;
    float wet  = DryMassKg + FuelMassKg;
    float dry  = DryMassKg;
    return isp * g0 * Mathf.Log(wet / dry);  // m/s
}

public bool IsDeltaVOk()
{
    float required = PlanetMinDeltaV[TargetPlanetId] * 1000f; // km/s → m/s
    return ComputeDeltaV() >= required;
}
```

#### Sistema de restricciones en tiempo real
```csharp
public bool IsBudgetOk()  => TotalCostM <= BudgetLimitM;
public bool IsMassOk()    => TotalMassKg <= LauncherCapacityKg[Launcher];
public bool IsPowerOk()   => TotalPowerW <= PowerSourceWatts * SolarEfficiency(TargetPlanetId);
public bool IsDesignOk()  => IsBudgetOk() && IsMassOk() && IsPowerOk() && IsDeltaVOk();
```

#### Trade-off central del diseño
```
Más combustible  → mayor ΔV → llega más lejos
                 → mayor masa → necesita lanzador más caro
                 → menos presupuesto para instrumentos
                 → más pesado → coste mayor

Más instrumentos → más ciencia → más puntos
                 → más masa → necesita más combustible
                 → más potencia → necesita fuente más cara

Esta tensión ES el juego de diseño.
```

---

## 4. VUELO — FÍSICA Y CONTROLES

### Archivo: `Core/PhysicsEngine.cs`

#### Modelo de propulsión
```csharp
// PhysicsEngine.cs
public void Tick(float dt)
{
    if (!_thrusting) return;

    float isp      = EngineIspTable[_config.Engine];
    float exhaustV = isp * 9.80665f;                    // m/s
    float fuelBurn = (_config.ThrustN / exhaustV) * dt; // kg/s * dt

    _fuelKg    -= fuelBurn;
    float mass  = Mathf.Max(_dryMassKg, _fuelKg + _dryMassKg);
    Vector3 acc = transform.forward * (_config.ThrustN / mass);

    ShipRb.AddForce(acc, ForceMode.Acceleration);
}
```

#### Degradación solar de potencia
```csharp
public float SolarEfficiency(string planetId)
{
    // Ley del cuadrado inverso: P ∝ 1/d²
    float au = PlanetDistanceAU[planetId];
    return 1f / (au * au);  // Marte: 1/2.25, Júpiter: 1/27
}
```

#### Controles AR (giroscopio)
```
ShipController.cs + ARShipController.cs:
  Input: giroscopio del celular (Input.gyro.attitude)
  Pitch: inclinar el teléfono arriba/abajo
  Yaw:   girar el teléfono derecha/izquierda
  Roll:  rotar el teléfono
  Throttle: slider en pantalla (0-100%)
  Boost:    botón en pantalla → activa BurnoutSystem
```

---

## 5. BURNOUT — SISTEMA DE BOOST

### Archivos: `Game/BurnoutSystem.cs`, `Game/CollisionSystem.cs`

#### Barra de Boost — cómo se llena
```
ACCIÓN                              BOOST GANADO
───────────────────────────────────────────────
Near-miss asteroide   (<3m sin tocar) → +8%
Near-miss nave rival  (<5m sin tocar) → +12%
Atravesar escombros                  → +15%
Takedown exitoso (destruir rival)    → +35% instantáneo
Ir al límite de combustible (<5%)    → +5% continuo
Gravity assist (slingshot)           → +20%
───────────────────────────────────────────────
Barra llena = BURNOUT MODE (10 segundos)
```

#### Burnout Mode
```
Efectos cuando la barra llega a 100%:
  → Velocidad ×2.5
  → Invencibilidad total a colisiones
  → Exhaust cambia a blanco cegador (shader)
  → Trail de luz en el espacio AR
  → Vignette de fuego en bordes de pantalla
  → Rival ve tu nave con aura de fuego

Estrategia: guardar el burnout para la recta final
Riesgo:     en Burnout gastas combustible ×3
```

#### Código clave
```csharp
// BurnoutSystem.cs
public void AddGain(float amount)
{
    if (_suppressTimer > 0f) return;      // EMP del rival
    _bar = Mathf.Min(1f, _bar + amount);
    if (_bar >= 1f && !BurnoutActive) ActivateBurnout();
}

public void ActivateBurnout()
{
    BurnoutActive = true;
    _burnoutTimer = BurnoutDurationS;     // 10s
    OnBurnoutStart?.Invoke();
}

public void SuppressFor(float seconds)   // Pod EMP del rival
{
    _suppressTimer = Mathf.Max(_suppressTimer, seconds);
}

public void ForceFullBar()               // Pod Boost propio
{
    _bar = 1f;
    ActivateBurnout();
}
```

---

## 6. COLISIONES — FÍSICA REAL

### Archivo: `Game/CollisionSystem.cs`

#### Conservación de momento lineal
```csharp
// Colisión elástica — la nave más pesada (más fuel) domina
public void ResolveShipCollision(CollisionSystem other, float relSpeed)
{
    float m1 = WetMass;   // DryMassKg + FuelMassKg
    float m2 = other.WetMass;
    Vector3 v1 = _rb.linearVelocity;
    Vector3 v2 = other._rb.linearVelocity;

    // Resultado elástico para cada cuerpo
    Vector3 vf1 = ((m1 - m2) * v1 + 2f * m2 * v2) / (m1 + m2);
    Vector3 vf2 = ((m2 - m1) * v2 + 2f * m1 * v1) / (m1 + m2);

    _rb.linearVelocity       = vf1;
    other._rb.linearVelocity = vf2;
}
```

#### Sistema de daño
```
Severidad    Condición         Efecto
──────────────────────────────────────────────
Light        relSpeed < 20m/s  Sin vidas perdidas, rebote
Medium       20-50 m/s         -10% combustible (fuga)
Heavy        > 50 m/s          -1 vida, -25% combustible
──────────────────────────────────────────────
Vidas: 3 → 2 → 1 → Destruido

DamageState: Perfect | Damaged | Critical | Destroyed
```

#### Takedown — la recompensa de la agresividad
```csharp
// Takedown cuando m1 > m2 × 2 en colisión Heavy
bool isTakedown = severity == CollisionSeverity.Heavy && m1 > m2 * 2f;
if (isTakedown)
{
    TakedownType type = DetermineTakedownType(v1, v2);
    // Frontal / Lateral / Trasero
    _burnout?.RegisterTakedown(type);  // +35% boost bar
    other._burnout?.SuppressFor(5f);   // rival suprimido 5s (EMP)
}
ExplosionVFX.Spawn(ExplosionPrefab, impactPoint, isTakedown);
```

---

## 7. POWERUPS — PODS EN EL ESPACIO

### Archivo: `Game/PowerupSystem.cs`

```
Pod         Efecto                          Duración
───────────────────────────────────────────────────
Fuel        +15% combustible               Instantáneo
Boost       Barra de burnout → 100%        Instantáneo
Shield      Invencible a colisiones        10 segundos
EMP         Rival: suprime barra burnout   5 segundos
GravityAsst +500 m/s en dirección planeta  Instantáneo
───────────────────────────────────────────────────
```

#### Spawn
```csharp
// PowerupSystem.cs
// 8 pods distribuidos en esfera de 15-60m
// Se respawnean al ser recogidos (coroutine)
void SpawnAll()
{
    foreach (var type in SpawnSequence)  // array en Inspector
    {
        Vector3 pos = Random.onUnitSphere * Random.Range(15f, 60f);
        var go = Instantiate(PodPrefabFor(type), pos, Quaternion.identity);
        go.GetComponent<Powerup>().Type = type;
    }
}
```

---

## 8. SCIENCE OPS — AL LLEGAR AL PLANETA

### Archivo: `UI/ScienceOpsUI.cs`

#### Instrumentos disponibles
```
ID   Instrumento          Masa(kg)  Potencia(W)  Puntos  Scan(s)
────────────────────────────────────────────────────────────────
CAM  Cámara científica    15        25           500     6
SPE  Espectrómetro        20        40           800     12
MAG  Magnetómetro         10        15           400     8
RAD  Radiómetro           12        20           450     10
GEO  Sismógrafo           25        35           700     15
ATM  Sensor atmosférico   8         10           300     6
GRA  Gravímetro           18        30           600     20
PAR  Analizador partículas 30       50           1000    30
────────────────────────────────────────────────────────────────
```

#### Restricción de potencia en tiempo real
```csharp
// ScienceOpsUI.cs
bool CanActivate(InstrumentId id)
{
    float used = _activeInstruments.Sum(i => PowerW[i]);
    return used + PowerW[id] <= _design.EffectivePowerW;
}
// Si no hay potencia → instrumento no arranca
// Jugador debe priorizar: ¿cuál instrumento vale más por vatio?
```

#### Delay de comunicaciones
```
Planeta    Retardo real       En juego
──────────────────────────────────────
Marte      12.5 min           12.5 s (factor 60)
Júpiter    43 min             43 s
Saturno    80 min             80 s
Neptuno    250 min            250 s

Impacto: datos llegan tarde → menos puntos por turno
La antena de alta ganancia reduce este efecto
```

---

## 9. SCORING — CÓMO SE CALCULA EL SCORE

### Archivo: `Core/MissionScore.cs`

```csharp
public int TotalScore()
{
    // ArrivalBonus: 1st=×1.50, 2nd=×1.25, 3rd=×1.10, else=×1.00
    int baseScore     = Mathf.RoundToInt(1000f * ArrivalBonus(ArrivalPosition));
    int scienceBonus  = SciencePoints;
    int fuelBonus     = Mathf.RoundToInt(FuelFraction * 500f);  // 0-500 pts
    int takedownBonus = TakedownCount * 150;
    int gravAssist    = GravityAssistUsed ? 200 : 0;

    return baseScore + scienceBonus + fuelBonus + takedownBonus + gravAssist;
}
```

#### Tabla de máximo teórico
```
Componente      Máximo
────────────────────────
Llegada #1      1500
Ciencia (8 inst) ~5300
Fuel 100%        500
3 Takedowns      450
Gravity assist   200
────────────────────────
TOTAL MÁXIMO    ~7950
```

#### ScoreboardUI — animación reveal
```csharp
// ScoreboardUI.cs — cuenta de 0 al total con ease-out cúbico
IEnumerator AnimateScore(TMP_Text label, Image bar, int from, int to, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float ease = 1f - Mathf.Pow(1f - elapsed / duration, 3f);
        label.text = Mathf.RoundToInt(Mathf.Lerp(from, to, ease)).ToString("N0");
        yield return null;
    }
}
```

---

## 10. MULTIJUGADOR P2P — SIN SERVIDOR

### Archivos: `Network/SendaBridge.cs`, `Network/ShipSyncService.cs`, `Network/P2PChatSystem.cs`

#### Arquitectura
```
Jugador A (celular)  ←──── Iroh P2P ────→  Jugador B (celular)
                          Dilithium-5
                        (post-cuántico)
                       Sin servidor central
                        Funciona offline
```

#### Tipos de mensajes (NavMsg)
```
Tipo            Contenido               Frecuencia
─────────────────────────────────────────────────
shipState       pos/vel/throttle/fuel   20 Hz
chat            texto                   on-demand
powerup         tipo de pod recogido    on-demand
arrival         posición de llegada     1 vez
missionEnd      score final             1 vez
```

#### Sincronización de nave rival
```csharp
// ShipSyncService.cs — 20 Hz broadcast
void Update()
{
    _timer += Time.deltaTime;
    if (_timer < 1f / SyncHz) return;
    _timer = 0f;

    var state = ShipStateNet.FromTransform(
        NodeId, DisplayName, ship.transform, ship.Rb,
        ship.Throttle, ship.Boosting, burnout.BurnoutActive,
        ...
    );
    Senda.Broadcast(NavMsg.Build(NavMsgType.shipState, NodeId,
        JsonUtility.ToJson(state)));
}
```

---

## 11. ARQUITECTURA UNITY — ASSEMBLIES

```
Navigator.Core       MissionDesign, MissionScore, PhysicsEngine
       ↓
Navigator.Network    SendaBridge, ShipSyncService, P2PChatSystem
       ↓
Navigator.Game       ShipController, FuelSystem, BurnoutSystem,
                     CollisionSystem, PowerupSystem, RemoteShipManager
       ↓
Navigator.AR         ARShipController, ARSpaceManager,
                     ARSessionManager, ARCompassTracker
       ↓
Navigator.UI         GameManager, CockpitHUD, LobbyManager,
                     MissionDesignUI, ScienceOpsUI, ScoreboardUI
       ↓
Navigator.Editor     NavigatorSceneSetup (solo Editor)
```

#### Setup automático de escena
```
Unity → Navigator → Setup Scene
Crea y conecta automáticamente:
  GameManager, AR Session, XR Origin, LocalShip (con todos los
  componentes), ARCompassTracker, ARSpaceManager, ARSessionManager,
  CockpitHUD, PowerupSystem, MissionScore,
  MissionDesignUI, ScoreboardUI, ScienceOpsUI
```

---

## 12. ASSETS 3D — GENERADOS CON MESHY AI

```
Carpeta                 Contenido
────────────────────────────────────────────────────────
Assets/Models/Ships/    NV-01 nave jugador, NV-02 KRONOS,
                        NV-03 cohete lanzador, NV-04 cápsula
Assets/Models/Chars/    PJ-01 cadete, PJ-02 comandante,
                        PJ-03 agente KRONOS, PJ-04 científico
Assets/Models/Envs/     EN-01 cockpit, EN-02 tablero,
                        EN-03 estación espacial, EN-04 base
Assets/Models/Planets/  PL-01 Mercurio → PL-07 Neptuno
Assets/Models/Objects/  OB-01 asteroide, OB-02 portal,
                        OB-03 baliza, OB-04 satélite,
                        OB-05-A/B escombros de nave
Assets/Models/Instr/    IC-01 cámara, IC-02 antena,
                        IC-03 magnetómetro
────────────────────────────────────────────────────────
51 GLBs, 596 MB, 24 con PBR 4K textura
```

---

## 13. PASOS PARA TENER EL JUEGO CORRIENDO

```
1. Abrir NavigatorUnity en Unity 6 LTS
2. Packages/manifest.json ya tiene todos los paquetes
3. Instalar ZXing.Net.Unity desde Asset Store
4. Los scripts ya están en Assets/ (no copiar unity_scripts/)
5. Navigator → Setup Scene (menú Unity)
6. Asignar en Inspector:
   - LocalShip: prefab NV-01_FINAL.glb
   - RemoteShipPrefab: NV-02_FINAL.glb
   - ARSpaceManager: planetas PL-01..PL-07
   - PowerupSystem: 5 prefabs de pods
   - ExplosionPrefab: partículas Unity VFX
7. Copiar senda_core.dll → Assets/Plugins/
8. Build → Android (AR Core) o iOS (AR Kit)
9. Correr en 2 celulares, escanear QR → conectados
```

---

*Guacalita Inc. — Navigator 2.0 — NASA Space Apps Challenge 2026*
*Diseñado y creado por SAL. Claude asistió en documentación e implementación.*
