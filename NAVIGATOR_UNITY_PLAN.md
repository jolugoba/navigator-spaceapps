# NAVIGATOR 2.0 — Plan Unity (producto único)
**Guacalita Inc. — NASA Space Apps Challenge 2026 / Producto comercial**
**Definido: 2026-09-22 · Revisado: 2026-09-26 — Unity pasa a ser la ÚNICA plataforma**

> **Cambio de rumbo (2026-09-26):** todo NAVIGATOR se construye en Unity, incluida
> la entrega para NASA Space Apps. La app Flutter (`lib/`, `android/`, `ios/`,
> `pubspec.yaml`) queda como **referencia de lógica ya portada** y deja de
> desarrollarse. No hay "Fase 0 Flutter".

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

### Cobertura del reto — estado REAL en los scripts Unity

| Requisito del reto | Cómo lo resuelve Navigator | Estado |
|---|---|---|
| Mission objectives | flyby / orbit / landing | ⚠️ Se elige en `MissionDesignUI`, pero **no cambia el ΔV requerido** — falta en `MissionDesign.cs` |
| Spacecraft design | motor + potencia + antena + combustible | ✅ `MissionDesign.cs` |
| Scientific instruments | 8 instrumentos con masa/potencia/puntos | ✅ `MissionDesign.cs` + `ScienceOpsUI.cs` |
| Launch vehicles | Falcon 9 / Heavy / SLS / Starship limitan masa | ✅ `MissionDesign.cs` |
| Budgets | límite $1,000M | ✅ `IsBudgetOk` |
| Power | solar (cuadrado inverso) / RTG / reactor | ✅ `IsPowerOk` + degradación solar |
| Mass | masa total ≤ capacidad del lanzador | ✅ `IsMassOk` |
| Communications | kbps por antena + retardo de luz (AU × 8.317 min) | ✅ `ScienceOpsUI.cs` |
| Orbital constraints | ΔV Tsiolkovsky vs ΔV mínimo por planeta | ✅ `MissionDesign.cs` |
| Engineering decisions | gauges en tiempo real | ✅ `MissionDesignUI.cs` (requiere montar el Canvas) |
| Managing limited resources | más motor/fuel = menos instrumentos | ✅ |
| Evaluate how choices shape success | score = llegada + ciencia + fuel + takedowns + GA | ✅ `MissionScore.cs` + `ScoreboardUI.cs` |
| Concept to operation | diseño → despegue AR → vuelo → llegada → ciencia → resultado | ✅ `GameManager.cs` (máquina de estados) |

### Diferenciador
```
Otros equipos:   juego de escritorio / web con menús y sliders
Navigator:       misión en AR en tu celular — la ves ocurrir en tu espacio real
                 + multijugador P2P cifrado sin servidor
                 + física Tsiolkovsky + posiciones planetarias NASA reales
```

---

## 1. QUÉ ES NAVIGATOR

Juego AR multijugador espacial para celular — **sin gafas, sin headset, solo el teléfono**.
El jugador diseña una misión con restricciones reales de ingeniería, despega en AR
en su propio espacio (cuarto, aula, auditorio) y compite/coopera con otros jugadores
conectados P2P, sin servidor central.

---

## 2. DECISIONES DE STACK (cerradas)

| Área | Decisión | Descartado |
|---|---|---|
| Motor | **Unity 6 LTS** | Flutter + filament_scene |
| Render | **URP** 17 | Built-in |
| AR | **AR Foundation 6** + ARCore (Android) + ARKit (iOS) | CameraPreview de Flutter |
| Red P2P | **Senda nativo** (`senda_core`, Rust) vía **P/Invoke** en `SendaBridge.cs` | Mirror, Photon (requieren servidor/relay), companion app por WebSocket |
| Cripto | Túnel **Iroh** (QUIC + TLS 1.3) + firmas **Dilithium-5** | — |
| QR | ZXing.Net (Asset Store) | Unity Relay/Lobby |
| Datos NASA | JPL Horizons API vía `UnityWebRequest` | — |
| Assets 3D | Meshy AI → GLB/FBX; personajes con Mixamo | — |
| Input | Input System 1.9 **+ legacy** (ver §4) | — |
| Plataformas | Android (ARM64, API 26+) + iOS | Desktop |

> **Sobre "post-cuántico":** hoy el código usa **Dilithium-5** (firma post-cuántica)
> sobre el túnel Iroh. El intercambio de claves de Iroh (TLS 1.3) es clásico (X25519), salvo que `senda_core` añada otra capa.
> **Kyber-1024 no está implementado.** Hasta que `senda_core` exponga un handshake
> Kyber, el pitch debe decir *"identidad firmada con Dilithium-5 (post-cuántico)"*,
> no *"cifrado Kyber-1024"*. Añadir Kyber está en la Fase 6.

> **IA a bordo (NORA):** fuera de alcance para la entrega NASA. Se evalúa después del lanzamiento.

---

## 3. ARQUITECTURA — LO QUE YA EXISTE (27 scripts en `unity_scripts/`)

Una sola escena de juego (`SpaceFlight.unity`) controlada por **`GameManager`**
(máquina de estados: `Lobby → MissionDesign → Countdown → InFlight → ScienceOps → Scoreboard`).
Los paneles de UI se activan/desactivan por estado; no hay `SceneManager.LoadScene`.

```
Assets/Scripts/
├── Core/        Navigator.Core.asmdef
│   ├── MissionDesign.cs      enums, Isp, ΔV Tsiolkovsky, costos, masas, degradación solar
│   ├── PhysicsEngine.cs      empuje por motor + gravedad F=μ/r² (μ en unidades de juego)
│   └── MissionScore.cs       score final
├── Network/     Navigator.Network.asmdef
│   ├── SendaBridge.cs        P/Invoke a senda_core (Win .dll / Android .so / iOS __Internal)
│   ├── ShipSyncService.cs    broadcast ShipState JSON cada 50 ms (contiene ShipStateNet)
│   ├── P2PChatSystem.cs      texto + reacciones + mensajes de sistema (sin UI propia)
│   └── NasaHorizons.cs       Horizons API → azimut/altitud/distancia
├── AR/          Navigator.AR.asmdef
│   ├── ARSessionManager.cs   inicio AR + fallback si el equipo no soporta AR
│   ├── ARShipController.cs   giroscopio → pitch/yaw, toque → empuje, doble toque → boost
│   ├── ARCompassTracker.cs   GPS + brújula + Horizons → dirección al planeta
│   └── ARSpaceManager.cs     coloca planetas; modos Immersive / Portal / Table
├── Game/        Navigator.Game.asmdef
│   ├── ShipController.cs     movimiento, límite de velocidad, multiplicador Burnout
│   ├── CollisionSystem.cs    conservación de momento, daño, invencibilidad, escudo
│   ├── BurnoutSystem.cs      near-miss, takedowns, BURNOUT MODE
│   ├── FuelSystem.cs         consumo por motor, pods, fugas
│   ├── PowerupSystem.cs      5 pods + spawner
│   ├── ShipCustomizer.cs     4 zonas de color + sync P2P
│   ├── ShipAnimator.cs       toberas, alas, exhaust por código
│   ├── RemoteShipManager.cs  instancia e interpola naves remotas + nametag
│   └── ExplosionVFX.cs       partículas + flash en colisión fuerte
├── UI/          Navigator.UI.asmdef
│   ├── GameManager.cs        director / máquina de estados
│   ├── LobbyManager.cs       QR propio + escáner (ZXing)
│   ├── MissionDesignUI.cs    diseño de misión con gauges
│   ├── CockpitHUD.cs         velocidad, fuel, distancia, burnout, daño
│   ├── ScienceOpsUI.cs       instrumentos, potencia, retardo de señal
│   └── ScoreboardUI.cs       resultados finales
└── Editor/      Navigator.Editor.asmdef
    └── NavigatorSceneSetup.cs   menú Navigator → Setup Scene
```

---

## 4. BLOQUEANTES DE COMPILACIÓN (resolver antes de nada)

Los scripts **no compilan tal cual** en un proyecto Unity 6 nuevo:

| # | Problema | Arreglo |
|---|---|---|
| B-1 | Todos los `.asmdef` referencian `"GUID:Navigator.Core"` etc. — el prefijo `GUID:` exige un GUID real | Usar el nombre a secas: `"Navigator.Core"` |
| B-2 | `Game` y `UI` usan `TMPro` / `UnityEngine.UI` sin referenciarlos en su asmdef | Añadir `"Unity.TextMeshPro"` y `"UnityEngine.UI"` a `Navigator.Game` y `Navigator.UI` |
| B-3 | `manifest.json` no incluye `com.unity.ugui`; en Unity 6 TextMeshPro vive dentro de uGUI 2.0 y `com.unity.textmeshpro 3.2.0-pre.10` sobra | Añadir `"com.unity.ugui": "2.0.0"`, quitar `com.unity.textmeshpro` |
| B-4 | Hay **dos** manifests distintos (`Packages/manifest.json` y `unity_project/Packages/manifest.json`) | Dejar uno solo, el del proyecto Unity real |
| B-5 | Los scripts usan `Input.gyro` / `Input.GetTouch` (legacy) y el manifest trae Input System | Player Settings → *Active Input Handling* = **Both** (o migrar a Input System) |
| B-6 | `ShipController.cs:105` tiene el planeta objetivo fijo en `"mars"` | Leerlo de `MissionDesign.TargetPlanetId` vía `GameManager` |
| B-7 | `assets/Plugins/Android/AndroidManifest.xml` está en la carpeta de assets de **Flutter** | Moverlo a `Assets/Plugins/Android/` del proyecto Unity |

---

## 5. LO QUE FALTA (brechas reales)

### 5.1 Funcionalidad que existía en Flutter y no está en Unity
| Falta | Flutter de referencia | Nota |
|---|---|---|
| Menú principal | `lib/ui/main_menu.dart` | Nuevo estado `MainMenu` en `GameManager` |
| UI de chat (panel + burbujas AR) | `lib/ui/chat_overlay.dart` | `P2PChatSystem` solo tiene lógica |
| Idiomas es/en | `lib/l10n/app_localizations.dart` | Unity Localization o tabla simple |
| Modo sin AR (cockpit + giroscopio) | `lib/flight/cockpit_view.dart` | Fallback de `ARSessionManager` cuando el equipo no soporta AR |

### 5.2 Diseño documentado y aún no implementado
| Mecánica | Documento | Prioridad NASA |
|---|---|---|
| Objetivo flyby/orbit/landing suma ΔV | este plan §0 | **Alta** — el reto lo pide |
| ΔV por fases (TMI / correcciones / MOI / aterrizaje) | `NAVIGATOR_FISICA_REAL.md` §4 | Alta |
| Tooltips con misión NASA de referencia | `NAVIGATOR_FISICA_REAL.md` §9 | Alta (educativo) |
| Ventanas de lanzamiento | `NAVIGATOR_FISICA_REAL.md` §3 | Media |
| Más motores (LOX/RP-1, LOX/CH4, Kriptón, vela solar) | `NAVIGATOR_FISICA_REAL.md` §1 | Media |
| Gravity assist como elección de ruta | `NAVIGATOR_FISICA_REAL.md` §7 | Media |
| Blackouts de comunicación, radiación que daña instrumentos | `NAVIGATOR_FISICA_REAL.md` §5-6 | Baja |
| Anclaje a superficie en modos Portal/Table (`ARPlaneManager` + raycast) | §7 de este plan | Media |
| Countdown sincronizado entre jugadores | `NAVIGATOR_RACING_MECHANICS.md` | Media |
| Asteroides, estados de daño, reparación | `NAVIGATOR_RACING_MECHANICS.md` | Baja |
| Aftertouch, chain takedown, voz PTT | `NAVIGATOR_BURNOUT_P2P.md` | Post-NASA |
| Descubrimiento mDNS / Bluetooth | `NAVIGATOR_BURNOUT_P2P.md` | Post-NASA |

### 5.3 Dependencias externas
- **`senda_core` compilado** para Android arm64 (`libsenda_core.so`), iOS (`.a` estático, se enlaza como `__Internal`) y Windows (`senda_core.dll` para el Editor).
- **Modelos Meshy** de prioridad 1 — ver `NAVIGATOR_MESHY_CHECKLIST.md`.

---

## 6. FASES DE DESARROLLO

### FASE 1 — Proyecto Unity que compila
- [ ] Crear proyecto Unity 6 LTS (plantilla Universal 3D) — ver `NAVIGATOR_UNITY_QUICKSTART.md`
- [ ] Resolver B-1 … B-7 (§4)
- [ ] Importar `unity_scripts/` → `Assets/Scripts/` y ZXing.Net
- [ ] Compilar sin errores; *Navigator → Setup Scene* funciona
- [ ] Versionar el proyecto Unity en este repo (`Assets/`, `Packages/`, `ProjectSettings/`) con `.gitignore` de Unity

### FASE 2 — Demo jugable un jugador (placeholders)
- [ ] Cubos/esferas como placeholder de nave y Marte
- [ ] Flujo completo: MainMenu → MissionDesign → Countdown → InFlight → ScienceOps → Scoreboard
- [ ] Objetivo flyby/orbit/landing afecta el ΔV requerido
- [ ] Horizons + GPS en dispositivo; fallback sin red
- [ ] Probado en un Android físico

### FASE 3 — Multijugador P2P
- [ ] `senda_core` para Android arm64 en `Assets/Plugins/Android/`
- [ ] QR lobby → conexión → ambos ven la nave del otro (`RemoteShipManager`)
- [ ] Panel de chat + reacciones
- [ ] Countdown sincronizado
- [ ] Probado en dos teléfonos físicos

### FASE 4 — Assets Meshy + AR
- [ ] Prioridad 1 Meshy: NV-01, EN-01, PL-03, PJ-01, PW-01, OB-01
- [ ] Asignar prefabs en el Inspector (tabla del Quickstart, paso 9)
- [ ] Modo Table con `ARPlaneManager`
- [ ] VFX: exhaust, boost, explosión

### FASE 5 — Entrega NASA Space Apps
- [ ] Tooltips con misión NASA real por motor/planeta
- [ ] ΔV por fases visible en MissionDesign
- [ ] Video demo 2 min + APK
- [ ] Pitch con afirmaciones que el build cumple (ver §8)

### FASE 6 — Post-NASA (producto)
- [ ] Kyber-1024 en el handshake de `senda_core`
- [ ] Build iOS (ARKit + `senda_core.a`)
- [ ] Mecánicas Burnout avanzadas, mDNS, voz
- [ ] Google Play + App Store

---

## 7. MODO AR — visión (funciona en CUALQUIER lugar)

**REGLA DE ORO: SIN GAFAS. SIN CASCO. SOLO EL CELULAR.**
El AR no depende de apuntar al cielo: funciona en sala cerrada, aula o evento.

| Modo | Qué ve el jugador | Estado |
|---|---|---|
| **A — Inmersivo** (principal) | La misión ocurre a su alrededor en 360°; planetas en la dirección real (GPS + Horizons) | ✅ `ARSpaceManager` (Immersive) |
| **B — Portal** | Ventana circular anclada a una pared/piso con la misión dentro | ⚠️ Enum existe; falta anclaje con `ARPlaneManager` |
| **C — Sistema solar en la mesa** | Sistema solar miniatura sobre una superficie (0.5 m = 1 AU) | ⚠️ Escala existe; falta anclaje con `ARPlaneManager` |

Flujo técnico:
```
1. ARSessionManager inicia ARSession; ARCameraBackground pinta la cámara
2. ARCompassTracker obtiene GPS + brújula → NasaHorizons → dirección del planeta
3. ARSpaceManager instancia el prefab del planeta en esa dirección
4. ARShipController: giroscopio + toques mueven la nave
5. ShipSyncService/RemoteShipManager: las naves de otros jugadores aparecen en tu espacio
```

---

## 8. PITCH (alineado con lo que el build hace)

> **NAVIGATOR** es un juego AR multijugador para celular donde diseñas una misión
> espacial con restricciones reales de la NASA — presupuesto, masa, potencia,
> comunicaciones y ΔV de Tsiolkovsky — y luego la vuelas en tu propio espacio a
> través de la cámara.
>
> Tus amigos se conectan escaneando un QR, sin servidor, con identidad firmada por
> criptografía post-cuántica (Dilithium-5). Compiten por llegar primero y reunir
> más ciencia: la física real ES la estrategia.

---

## 9. REFERENCIAS TÉCNICAS

```
NASA Horizons API:    https://ssd.jpl.nasa.gov/horizons/
Unity AR Foundation:  com.unity.xr.arfoundation
Iroh (P2P):           https://iroh.computer
liboqs (Kyber/Dilithium): https://github.com/open-quantum-safe/liboqs
ZXing.Net:            https://github.com/micjahn/ZXing.Net
Meshy AI 3D:          https://www.meshy.ai
Mixamo:               https://www.mixamo.com
```

Documentos relacionados:
- `NAVIGATOR_UNITY_QUICKSTART.md` — crear el proyecto paso a paso
- `unity_scripts/ESTADO_UNITY.md` — estado por script
- `NAVIGATOR_FISICA_REAL.md` — física y datos NASA
- `NAVIGATOR_RACING_MECHANICS.md`, `NAVIGATOR_BURNOUT_P2P.md` — diseño de juego
- `NAVIGATOR_MESHY_CHECKLIST.md` — assets 3D
- `NAVIGATOR.md` — visión original Flutter (histórico)

---

*Documento de Guacalita Inc. — Todos los derechos reservados.*
*Diseño y concepto: SAL / Guacalita Inc.*
*Implementación técnica: Claude (asistente)*
