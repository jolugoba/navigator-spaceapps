# NAVIGATOR 2.0 — Plan Completo Unity + AR + Senda Post-Cuántico
**Guacalita Inc. — NASA Space Apps Challenge / Producto comercial**
**Fecha definición: 2026-09-22**

---

## 0. RETO NASA SPACE APPS CHALLENGE 2026

**Texto oficial del reto:**
> "Designing a space mission requires careful consideration of competing demands,
> including mission objectives, spacecraft design, scientific instruments, launch vehicles,
> budgets, power, mass, communications, and orbital constraints. For students who rarely
> get to experiment with an entire mission from concept to operation, these trade-offs
> often remain abstract. Your challenge is to create an interactive game that allows
> participants to design, manage, and simulate a complete space mission while making
> engineering decisions, managing limited resources, and evaluating how each choice
> shapes the success of their mission."

**Nivel:** Intermediate / Beginner-Youth | **Tema:** Games

### Cobertura completa del reto

| Requisito del reto | Cómo Navigator lo resuelve |
|---|---|
| Mission objectives | flyby / orbit / landing — cada uno cambia delta-V mínimo requerido |
| Spacecraft design | selección de motor + potencia + antena + combustible con consecuencias reales |
| Scientific instruments | 8 instrumentos NASA reales con masa/potencia/puntos de ciencia propios |
| Launch vehicles | Falcon 9 / Heavy / SLS / Starship — cada uno limita la masa total |
| Budgets | límite real $1,000M — si te pasas no puedes lanzar |
| Power | solar/RTG/reactor generan W — instrumentos consumen W — balance visible |
| Mass | masa seca + combustible debe caber en el lanzador elegido |
| Communications | antena define kbps reales + retardo de luz (AU × 8.317 min) |
| Orbital constraints | delta-V Tsiolkovsky — si no alcanza, la misión falla antes de despegar |
| Engineering decisions | gauges en tiempo real muestran impacto de cada decisión |
| Managing limited resources | trade-off real: más motor = menos instrumentos = menos ciencia |
| Evaluate how choices shape success | score final = velocidad llegada + ciencia + combustible restante |
| Concept to operation | diseño → lanzamiento AR → vuelo → llegada → ciencia ops → resultado |

### Diferenciador sobre otros participantes
```
Otros equipos harán:   juego de escritorio / web con menús y sliders
Navigator hace:        misión en AR sobre tu celular — la ves ocurrir
                       en tu espacio real + multijugador cifrado
                       + física real Tsiolkovsky + posiciones NASA reales
```

---

## 1. QUÉ ES NAVIGATOR

Juego AR multijugador espacial donde la misión espacial ocurre en tiempo real
a través de la cámara del celular — sin gafas, sin headset, solo el teléfono.
Toda comunicación entre jugadores cifrada con criptografía post-cuántica
(Kyber-1024 + Dilithium-5). Sin servidor central — P2P puro.

**Diferenciador único en el mundo:**
> AR en celular (sin gafas) + multiplayer P2P post-cuántico + física NASA real + misión científica completa concept-to-operation

---

## 2. LO QUE CONSTRUIMOS EN FLUTTER (Navigator 1.0)

### 2.1 Stack Flutter actual
- **Motor 3D:** filament_scene (PBR Filament) + CustomPainter 2D proyectado
- **Red P2P:** senda_p2p_chat (Kyber-1024 + Dilithium-5, sin servidor)
- **Física:** ECS propio (SpaceWorld, EntityId, Systems)
- **AR:** cámara básica con CameraPreview de fondo
- **GPS + NASA:** Horizons API → posición real de planetas
- **Lenguaje:** Dart / Flutter

### 2.2 Sistemas implementados (funcionando)

#### ECS (Entity Component System)
```
SpaceWorld          — registro de entidades y componentes
EntityId            — identificador único por entidad
Components:
  ShipTransform     — posición, yaw, pitch, roll
  ShipVelocity      — velocidad lineal + angular
  ShipThrust        — thrust main/lateral/boost
  ShipFuel          — current/max, fraction, isEmpty
  ShipHealth        — puntos de vida
  ShipOwner         — nodeId, displayName, isLocal
  ShipFuel          — combustible real mapeado de misión
  MissionTarget     — planetId, distanceUnits, arrivedAt
  MissionScore      — tiempo, boost usado, delta-V total
Systems:
  FuelSystem        — drain por thrust + boost
  FlightPhysicsSystem — thrust → vel → pos + gravedad planetaria
  SteeringSystem    — yaw/pitch angular
  MissionTrackingSystem — distancia al planeta objetivo
  ShipCollisionSystem — detección colisiones ECS
```

#### Motor de física real
```dart
// Ecuación de Tsiolkovsky (delta-V real)
deltaV = Isp × g₀ × ln(masa_húmeda / masa_seca)

// Gravedad planetaria F = μ/r²
forceMag = planetGravityMu[planetId] / r²
vel += direction.normalized × forceMag × dt

// Valores μ por planeta (unidades de juego, escala 1:1000u)
mercury: 190_000   venus:   455_000   mars:   190_000
jupiter: 1_265_000 saturn:  535_000   uranus: 445_000
neptune: 570_000

// Multiplicador de empuje por motor
chemical: 1.0x    ion: 0.55x    nuclear: 1.4x
```

#### Mission Design System (diseño de misión científica)
```
Componentes seleccionables por el jugador:
  Motor:        químico / iónico / nuclear
  Combustible:  200–8000 kg (slider)
  Potencia:     paneles solares / RTG / reactor nuclear
  Antena:       omnidireccional (0.5kbps) / mediana (100kbps) / HGA (6Mbps)
  Lanzador:     Falcon 9 / Falcon Heavy / SLS / Starship
  Instrumentos: 8 tipos (cámara, espectrómetro, magnetómetro, radar, etc.)

Cálculos en tiempo real:
  Presupuesto total ($M)     — límite $1,000M
  Masa total (kg)            — límite según lanzador
  Delta-V disponible (m/s)   — mínimo por planeta
  Potencia disponible (W)    — no sobrepasar con instrumentos

Planetas objetivo con delta-V mínimo real:
  mercury: 10,900 m/s   venus:   6,500 m/s   mars:    5,600 m/s
  jupiter:  9,100 m/s   saturn: 10,400 m/s   uranus: 15,900 m/s
  neptune: 16,000 m/s
```

#### Operaciones Científicas (ScienceOpsScreen)
```
Al llegar al planeta:
  - Activar/desactivar instrumentos según potencia disponible
  - Cada instrumento llena barra de progreso en ~25s
  - Retraso de comunicación real (AU × 8.317 min/AU)
    mercury: 5.1 min   jupiter: 43.2 min   neptune: 250 min
  - Transmisión a la Tierra a velocidad real de antena
  - Puntos de ciencia acumulados → score final
```

#### Efectos visuales implementados en Flutter
```
_UniversePainter (CustomPainter):
  - 300 estrellas con proyección perspectiva 3D manual
  - 5 nebulosas con blur radial
  - Sol con corona 4 capas + 5 lens flares
  - Planetas con texturas JPG descargadas + procedural fallback
  - Anillos Saturn/Uranus (back half + front half)
  - Hiperespace: estrellas se elongan en boost
  - Shooting stars cada 5s
  - Space dust parallax (profundidad)
  - Explosiones con shockwave + partículas
  - Silhouette cabina + nariz nave + alas
  - Gauges: velocidad, combustible, G-force
  - Indicador de vidas (3 corazones)
  - Alerta proximidad + colisión
  - Warning combustible crítico (parpadeo)
```

#### Red P2P post-cuántica
```
senda_p2p_chat:
  - Kyber-1024: encapsulación de clave
  - Dilithium-5: firma digital identidad
  - Iroh P2P: descubrimiento de peers sin servidor
  - Sin internet requerido (LAN/WiFi directo)
  - WebRTC para streaming de estado de naves

ShipSyncService:
  - broadcast(ShipState) — posición/vel/yaw/pitch cada frame
  - onRemoteUpdate stream — recibe estados de otras naves
  - broadcastMissionEnd(score) — finaliza misión
  - onRemoteScore stream — leaderboard en tiempo real

ShipState sincronizado entre jugadores:
  position, velocity, yaw, pitch, roll,
  throttle, boosting, missionTarget, distanceToTarget,
  score, timestamp
```

#### NASA Horizons API
```
HorizonsService.getPlanetPosition(nasaBodyId, lat, lon):
  - Consulta posición real del planeta desde ubicación GPS del jugador
  - Retorna azimuth + altitud en grados
  - Convierte a Vec3 direccional para mostrar en pantalla correcta
  - Fallback a posición fija si no hay internet
```

#### Colisiones y vida
```
- 3 vidas por jugador
- Colisión = -1 vida + explosión + screen shake + 3s invencibilidad
- 0 vidas = nave destruida + fin de misión
- Transferencia de combustible entre naves cercanas (<30u)
- Combustible vacío = fin de misión (fallo)
```

### 2.3 Pantallas implementadas
```
MainMenuScreen      — menú principal animado
LobbyScreen         — sala P2P, QR code de conexión, lista de jugadores
MissionSelectScreen — selección de planeta objetivo
MissionDesignScreen — diseño de nave con presupuesto/masa/potencia en tiempo real
CockpitView         — vuelo principal con AR + HUD + controles
ScienceOpsScreen    — operaciones científicas al llegar al planeta
MissionResultScreen — tabla de puntuaciones multijugador
ChatOverlay         — chat P2P durante el vuelo
HudOverlay          — HUD de distancia, dirección planeta, estado naves remotas
```

---

## 3. NAVIGATOR 2.0 — VISIÓN EN UNITY

### 3.1 Stack nuevo
```
Motor:        Unity 6 (LTS)
Render:       URP (Universal Render Pipeline)
3D Assets:    Meshy AI → GLB/FBX → Unity
AR:           Unity AR Foundation (ARCore + ARKit)
Multiplayer:  Mirror Networking o Photon Fusion
Cripto P2P:   Senda (companion app) ← WebSocket → Unity
              ó liboqs C# (Kyber + Dilithium nativos en C#)
GPS/Astro:    NASA Horizons API (misma, reescrita en C#)
IA bordo:     NORA v8 (HTTP local desde Unity → NORA server)
Lenguaje:     C#
Plataformas:  Android + iOS (mismo build)
```

### 3.2 Lo que Meshy genera (tú lo creas, yo lo monto)

```
Activos 3D a crear en Meshy:
  nave_jugador.glb        — nave principal vista en tercera persona
  nave_remota.glb         — nave de otros jugadores
  cabina_interior.glb     — cockpit 3D real con instrumentos
  planeta_rocoso.glb      — template para Mercurio/Marte
  planeta_gaseoso.glb     — template para Júpiter/Saturno
  planeta_hielo.glb       — template para Urano/Neptuno
  estacion_espacial.glb   — objetivo de misión alternativo
  satelite.glb            — objeto flotante en órbita
  asteroide.glb           — obstáculo dinámico
  explosion_debris.glb    — fragmentos post-colisión
```

### 3.3 Arquitectura Unity

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── MissionDesign.cs       ← portado de Dart
│   │   ├── PhysicsEngine.cs       ← portado de Dart (Tsiolkovsky)
│   │   ├── PlanetGravity.cs       ← portado de Dart (F=μ/r²)
│   │   └── MissionScore.cs        ← portado de Dart
│   ├── Network/
│   │   ├── SendaBridge.cs         ← WebSocket → Senda companion app
│   │   ├── ShipSyncService.cs     ← portado de Dart
│   │   └── NasaHorizons.cs        ← portado de Dart (C# HttpClient)
│   ├── AR/
│   │   ├── ARSpaceManager.cs      ← coloca planetas en dirección GPS real
│   │   ├── ARShipController.cs    ← controla nave en espacio AR
│   │   └── ARCompassTracker.cs    ← sincroniza AR con brújula + GPS
│   ├── UI/
│   │   ├── MissionDesignUI.cs     ← portado de Dart
│   │   ├── ScienceOpsUI.cs        ← portado de Dart
│   │   ├── CockpitHUD.cs          ← portado de Dart
│   │   └── LobbyUI.cs             ← portado de Dart
│   └── Game/
│       ├── ShipController.cs      ← movimiento nave
│       ├── CollisionSystem.cs     ← colisiones + vidas
│       ├── FuelSystem.cs          ← portado de Dart
│       └── ExplosionVFX.cs        ← partículas Unity VFX Graph
├── Prefabs/
│   ├── Ships/                     ← modelos Meshy montados
│   ├── Planets/                   ← modelos Meshy montados
│   └── Effects/                   ← explosiones, exhaust, etc.
├── Materials/
│   ├── SpaceSkybox/               ← HDRI del espacio real
│   └── PlanetShaders/             ← shaders PBR para planetas
└── Scenes/
    ├── MainMenu.unity
    ├── Lobby.unity
    ├── MissionDesign.unity
    ├── SpaceFlight.unity          ← modo normal (sin AR)
    └── ARFlight.unity             ← modo AR con cámara real
```

### 3.4 Modo AR — Visión correcta (funciona en CUALQUIER lugar)

**REGLA DE ORO: SIN GAFAS. SIN CASCO. SOLO EL CELULAR.**
```
El jugador sostiene su celular normal
La pantalla muestra la cámara (su cuarto, sala, calle, lo que sea)
Unity dibuja la misión espacial ENCIMA de esa imagen en tiempo real
El jugador ve planetas + cohete + naves en su pantalla
Exactamente como Pokémon GO pero con una misión espacial completa
Cualquier persona con Android o iPhone puede jugarlo — sin comprar nada
```
**Principio fundamental:** el AR no depende de apuntar al cielo.
Funciona en sala cerrada, aula, evento, demo en interiores — siempre impacta.
La cámara muestra el mundo real y la misión espacial ocurre EN ESE ESPACIO.

#### Modo A — ESPACIO INMERSIVO (principal)
```
El jugador abre la app → activa cámara
El fondo real desaparece → reemplazado por espacio profundo
Los objetos físicos reales quedan como siluetas/sombras
La misión ocurre a su alrededor en 360°:
  🚀 El cohete despegando frente a él
  🪐 Planetas flotando en el espacio que lo rodea
  🛸 Naves de otros jugadores moviéndose en tiempo real
  💥 Explosiones, exhaust, efectos de partículas
  📡 HUD con datos de la misión superpuesto

Funciona en: cuarto, sala, patio, auditorio, cualquier lugar
```

#### Modo B — VENTANA AL ESPACIO (más accesible)
```
La cámara sigue mostrando el mundo real (cuarto, pared, mesa)
Un portal/ventana circular aparece flotando en el espacio
Adentro del portal: la misión espacial ocurriendo en vivo
El jugador camina alrededor → ve distintos ángulos
Otro jugador ve exactamente el mismo portal en su espacio

Tecnología: AR Foundation plane detection → ancla portal
en superficie detectada (piso, pared, mesa)
```

#### Modo C — SISTEMA SOLAR EN TU MESA
```
AR Foundation detecta superficie plana (mesa, piso)
El sistema solar aparece miniaturizado sobre esa superficie
El jugador controla su nave que vuela entre los planetas
Los planetas tienen escala relativa real
Otro jugador conectado por Senda ve el mismo sistema solar
Pueden verse las naves de ambos moviendose en tiempo real

Escala: Marte a 30cm de la Tierra en la mesa
        Júpiter flotando a 1m de distancia
```

#### Flujo técnico Unity AR Foundation
```
1. ARSession inicia → ARCameraManager activa cámara
2. ARPlaneManager detecta superficies (modo mesa/portal)
   ó XRCameraBackground reemplaza fondo (modo inmersivo)
3. PhysicsEngine.cs corre en espacio AR (misma lógica Flutter)
4. Planetas Meshy 3D se instancian como Prefabs AR
5. ShipController.cs mueve nave con giroscopio + joystick
6. SendaBridge.cs sincroniza estado de naves entre jugadores
7. Ambos jugadores ven la misma escena en sus espacios físicos

Scripts AR necesarios:
  ARSpaceManager.cs    ← gestiona la escena espacial en AR
  ARPortalSpawner.cs   ← coloca portal en superficie detectada
  ARShipController.cs  ← controla nave en coordenadas AR
  ARSessionManager.cs  ← inicia/detiene sesión AR
```

### 3.5 Integración Senda Post-Cuántico en Unity

```
Arquitectura:

Opción A — Companion App (más rápida de implementar):
  ┌──────────────────────────────────────────┐
  │  Unity Game (C#)                         │
  │    ↕ WebSocket localhost:8765            │
  │  Senda Bridge Service (Flutter/Dart)     │
  │    ↕ P2P Kyber-1024 + Dilithium-5        │
  │  Otro dispositivo                        │
  └──────────────────────────────────────────┘

Opción B — liboqs nativo en Unity (más limpio):
  liboqs (C library) → Unity Native Plugin (C++) → C# P/Invoke
  Kyber1024.Encapsulate() directo en C#
  Dilithium5.Sign() directo en C#
  Mirror Networking como transporte, cripto por encima

Mensajes sincronizados (mismo protocolo que Flutter):
  ShipState: position, velocity, yaw, pitch, throttle, boosting
  MissionEnd: finalScore, planetId
  ChatMessage: encrypted text, sender, timestamp
  FuelTransfer: amount, fromNode, toNode
```

### 3.6 Física real portada

```csharp
// PhysicsEngine.cs — portado de Dart
public class PhysicsEngine : MonoBehaviour
{
    // Tsiolkovsky
    public float DeltaV(float ispS, float wetMass, float dryMass)
        => ispS * 9.80665f * Mathf.Log(wetMass / dryMass);

    // Gravedad planetaria F = μ/r²
    void ApplyPlanetGravity(Rigidbody ship, Vector3 planetPos, float mu)
    {
        Vector3 delta = planetPos - ship.position;
        float r2 = delta.sqrMagnitude;
        if (r2 < 100f) return;
        float force = mu / r2;
        ship.AddForce(delta.normalized * force);
    }

    // Multiplicador motor
    const float thrustChemical = 1.0f;
    const float thrustIon      = 0.55f;
    const float thrustNuclear  = 1.4f;
}
```

---

## 4. QUÉ SE PORTA vs QUÉ SE RECONSTRUYE

```
PORTA DIRECTO A C#                  RECONSTRUYE EN UNITY NATIVO
──────────────────────────────────  ─────────────────────────────────
MissionDesign (toda la lógica)      Estrellitas → Skybox HDRI real
PhysicsEngine (Tsiolkovsky + μ/r²)  Planetas procedurales → Meshy 3D
MissionScore (cálculos)             CustomPainter → Shader Graph URP
NasaHorizons (API calls)            Partículas Flutter → VFX Graph
ShipSyncService (protocolo)         CameraPreview → AR Foundation
FuelSystem (drain rates)            Explosiones → Particle System
ScienceOps (instrumentos/puntos)    Cabina 2D → Modelo 3D Meshy
MissionDesignScreen (UI lógica)     Joystick → Input System Unity
ChatOverlay (mensajes)              QR Lobby → Unity Relay/Lobby
```

---

## 5. FASES DE DESARROLLO

### FASE 0 — Terminar Navigator 1.0 Flutter (NASA Space Apps)
- [ ] Probar en dispositivo físico Android
- [ ] Verificar AR cámara + posiciones NASA reales
- [ ] Entregar NASA Space Apps Challenge 2026

### FASE 1 — Setup Unity + primeros assets Meshy
- [ ] Instalar Unity 6 LTS + URP
- [ ] Crear proyecto navigator2
- [ ] Diseñar nave en papel → generar con Meshy → importar GLB
- [ ] Probar movimiento básico nave en Unity

### FASE 2 — Portar lógica core a C#
- [ ] MissionDesign.cs (copiar lógica de Dart)
- [ ] PhysicsEngine.cs (Tsiolkovsky + gravedad)
- [ ] NasaHorizons.cs (API en C# HttpClient)
- [ ] MissionScore.cs

### FASE 3 — AR Foundation
- [ ] Instalar AR Foundation + ARCore/ARKit
- [ ] ARSpaceManager: anclar planetas en dirección GPS real
- [ ] Probar: apuntar al cielo → ver Marte donde está

### FASE 4 — Multiplayer P2P con Senda
- [ ] Implementar SendaBridge WebSocket
- [ ] ShipSyncService.cs (misma lógica que Dart)
- [ ] Probar: dos celulares ven la misma nave en AR

### FASE 5 — UI completa + MissionDesign
- [ ] Portar MissionDesignUI a Unity UI Toolkit
- [ ] ScienceOpsUI
- [ ] LobbyUI con QR

### FASE 6 — Visual polish
- [ ] Más modelos Meshy (planetas, explosiones, estación)
- [ ] VFX Graph: exhaust, explosiones, boost
- [ ] Skybox HDRI espacio real
- [ ] Efectos AR: shaders PBR en modelos

### FASE 7 — Lanzamiento
- [ ] Build Android APK
- [ ] Build iOS IPA
- [ ] Google Play + App Store

---

## 6. PITCH DEL PRODUCTO

> **NAVIGATOR** es el primer juego AR multijugador del mundo con cifrado post-cuántico.
>
> Apuntas tu celular al cielo → ves Marte exactamente donde está → tu nave vuela hacia allá en realidad aumentada → un amigo en otra ciudad se conecta sin servidor, cifrado con criptografía que resiste computadoras cuánticas → diseñan juntos la misión científica → recolectan datos reales usando instrumentos de la NASA → compiten por el mayor puntaje de ciencia.
>
> Sin Unity. Sin servidor central. Sin NSA. Solo física real, posiciones astronómicas reales, y cifrado del futuro.

---

## 7. REFERENCIAS TÉCNICAS

```
NASA Horizons API:    https://ssd.jpl.nasa.gov/horizons/
Unity AR Foundation:  com.unity.xr.arfoundation
liboqs (C#):          https://github.com/open-quantum-safe/liboqs-dotnet
Mirror Networking:    https://mirror-networking.com
Meshy AI 3D:          https://www.meshy.ai
VFX Graph Unity:      com.unity.visualeffectgraph
```

---

*Documento generado por Guacalita Inc. — Todos los derechos reservados.*
*Diseño y concepto: SAL / Guacalita Inc.*
*Implementación técnica: Claude (asistente)*
