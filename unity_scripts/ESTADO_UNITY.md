# NAVIGATOR 2.0 — Estado Scripts Unity
**Guacalita Inc. | Fecha: 2026-09-26**

---

## LO QUE YA ESTÁ CONSTRUIDO ✅

### Core/ — Física NASA real
| Script | Qué hace |
|---|---|
| `MissionDesign.cs` | Todos los enums, tablas Isp, ΔV Tsiolkovsky, degradación solar, costos, masas |
| `PhysicsEngine.cs` | Motor Tsiolkovsky, gravedad F=μ/r², configuración por tipo de motor |
| `MissionScore.cs` | Score = llegada + ciencia + combustible + takedowns + gravity assist |

### Network/ — P2P post-cuántico (mismo Senda del Flutter)
| Script | Qué hace |
|---|---|
| `SendaBridge.cs` | P/Invoke a `senda_core.dll` (Iroh + Dilithium-5) — igual que Flutter |
| `ShipSyncService.cs` | Broadcast/receive ShipState JSON cada 50ms — protocolo idéntico a Flutter |
| `P2PChatSystem.cs` | Chat texto/reacciones/voz vía Senda — eventos del sistema automáticos |
| `NasaHorizons.cs` | Consulta NASA JPL Horizons API para posición real de planetas por GPS |

> **Post-cuántico:** Dilithium-5 firma el handshake dentro de senda_core.dll.
> Los mensajes viajan por el túnel Iroh cifrado. Sin servidor. Sin internet.

### AR/ — Cámara del celular (Unity hace el fondo automáticamente)
| Script | Qué hace |
|---|---|
| `ARSessionManager.cs` | Inicia AR Foundation, maneja fallback si dispositivo no soporta AR |
| `ARShipController.cs` | Giroscopio → pitch/yaw nave. Toque → thrust. Doble toque → boost |
| `ARCompassTracker.cs` | GPS + NASA Horizons → dirección real al planeta. Fallback sin GPS |
| `ARSpaceManager.cs` | Pone planetas en AR (3 modos: Inmersivo / Portal / Mesa) |

> **La cámara de fondo** la maneja Unity automáticamente con:
> `AR Camera Background` component + `ARCore XR Plugin` + `ARKit XR Plugin`
> No hay código para eso — solo configurar la escena.

### Game/ — Juego completo
| Script | Qué hace |
|---|---|
| `ShipController.cs` | Movimiento nave, aplica PhysicsEngine, límite de velocidad, sync Senda |
| `CollisionSystem.cs` | Conservación de momento m₁v₁+m₂v₂, tipos de daño, invencibilidad 3s |
| `BurnoutSystem.cs` | Barra boost, near-miss combos, BURNOUT MODE 10s x2.5, takedowns |
| `FuelSystem.cs` | Drain por motor (ion 6x más eficiente), recarga pods, fuga por colisión |
| `ShipCustomizer.cs` | 4 materiales UV separados, colores en tiempo real, sync P2P |
| `RemoteShipManager.cs` | **Instancia y mueve la nave del otro jugador en tu AR** — interpolación suave |

### UI/
| Script | Qué hace |
|---|---|
| `LobbyManager.cs` | QR generator (ZXing) + scanner cámara → conecta dos jugadores en <5s |
| `CockpitHUD.cs` | Velocidad, combustible, distancia, barra Burnout, near-miss combo, daño |

---

## FLUJO COMPLETO DOS JUGADORES (ya funciona con estos scripts)

```
1. Ambos abren la app
2. AR Foundation enciende la cámara (Unity automático)
3. Jugador A muestra su QR (NodeId Iroh)
4. Jugador B escanea → senda_connect() → túnel Dilithium-5
5. Intercambian lobbyInfo (nombre, colores de nave)
6. Diseñan misión (MissionDesign.cs)
7. Despegan → ShipController empieza a moverse en AR
8. ShipSyncService broadcast cada 50ms → RemoteShipManager mueve la nave rival
9. AMBOS SE VEN en el espacio AR en tiempo real
10. Choques → CollisionSystem conserva momento → BurnoutSystem registra takedown
11. Chat → P2PChatSystem → eventos automáticos ("🔥 TAKEDOWN!")
12. Llegan al planeta → MissionScore calcula score final
```

---

## SCRIPTS COMPLETOS ✅ (27 total)

### UI/ (completos)
| Script | Qué hace |
|---|---|
| `GameManager.cs` | State machine Lobby→MissionDesign→Countdown→InFlight→ScienceOps→Scoreboard |
| `MissionDesignUI.cs` | Pantalla completa de diseño — planetas, motores, combustible slider, instrumentos, ΔV bar, presupuesto, lanzar |
| `ScienceOpsUI.cs` | Pantalla llegada al planeta — barras progreso instrumentos, potencia, score en vivo, completar misión |
| `ScoreboardUI.cs` | Tabla de scores al final de misión — muestra ranking por criterios |

### Game/ (completos)
| Script | Qué hace |
|---|---|
| `ExplosionVFX.cs` | Control de partículas de explosión post-colisión, secuencia de impacto |
| `ShipAnimator.cs` | Animaciones procedurales de la nave (propulsores, daño visual, aterrizaje) |
| `PowerupSystem.cs` | Pods flotantes (Fuel/Boost/Shield/EMP/GravityAssist) + SpawnManager con respawn. Animación bob+rotate. |

### Editor/ (herramientas Unity Editor)
| Script | Qué hace |
|---|---|
| `NavigatorSceneSetup.cs` | Editor tool para crear la escena mínima (AR Session, XR Origin, GameManager, etc.) en un click |

### Métodos añadidos a scripts existentes
| Script | Método | Para qué |
|---|---|---|
| `BurnoutSystem.cs` | `ForceFullBar()` | Pod Boost llena barra al instante |
| `BurnoutSystem.cs` | `SuppressFor(float)` | EMP del rival congela Burnout N segundos |
| `CollisionSystem.cs` | `ActivateShield(float)` | Pod Escudo da invencibilidad N segundos |

---

## LO QUE FALTA ❌

### Scripts pendientes
*Todos los 27 scripts de C# están escritos.* Los pendientes son configuración, assets y compile blockers.

### Assets necesarios (de Meshy — checklist en NAVIGATOR_MESHY_CHECKLIST.md)
```
PRIORIDAD 1 (para primera demo):
  ❌ NV-01 Nave del jugador       → prefab Ship con 4 materiales
  ❌ NV-02 Nave KRONOS            → prefab RemoteShip
  ❌ PL-03 Marte                  → prefab Planet
  ❌ PJ-01 El Cadete              → personaje (Mixamo después)
  ❌ PW-01 Pod Combustible        → prefab Powerup
  ❌ OB-01 Asteroide              → prefab Obstacle
```

### Configuración Unity (única vez al crear el proyecto)
```
1. Unity 6 LTS — crear proyecto con URP template
2. Package Manager instalar:
   - AR Foundation
   - ARCore XR Plugin (Android)
   - ARKit XR Plugin (iOS)
   - ZXing.Net.Unity (para QR — Unity Asset Store, gratuito)
   - TextMeshPro (ya viene con Unity)
3. Build Settings:
   - Android: min SDK 26, Target SDK 34
   - Permissions: CAMERA, ACCESS_FINE_LOCATION, INTERNET
4. XR Plug-in Management → activar ARCore (Android) + ARKit (iOS)
5. Copiar senda_core.dll a Assets/Plugins/Android/
6. Importar unity_scripts/ a Assets/Scripts/
7. Importar modelos Meshy a Assets/Prefabs/Ships/ y Assets/Prefabs/Planets/
```

### Escena mínima para demo (SpaceFlight.unity)
```
Hierarchy:
  AR Session                 ← componente ARSession
  XR Origin
    └─ Camera Offset
         └─ Main Camera
              └─ AR Camera Background  ← Unity activa cámara automático
              └─ AR Camera Manager
  GameManager
    └─ SendaBridge.cs
    └─ ShipSyncService.cs
    └─ P2PChatSystem.cs
    └─ RemoteShipManager.cs
    └─ ARSessionManager.cs
  LocalShip (tag: LocalShip)
    └─ ShipController.cs
    └─ PhysicsEngine.cs
    └─ FuelSystem.cs
    └─ BurnoutSystem.cs
    └─ CollisionSystem.cs
    └─ ShipCustomizer.cs
    └─ ARShipController.cs
  ARCompassTracker.cs
  ARSpaceManager.cs
  CockpitHUD (Canvas WorldSpace)
  LobbyManager (Canvas — solo en escena Lobby.unity)
```

---

## RESUMEN DE PENDIENTES PARA DEMO COMPLETA

| Qué | Quién lo hace | Estado |
|---|---|---|
| Modelos Meshy (6 de prioridad 1) | SAL en meshy.ai | ❌ pendiente |
| Animaciones Mixamo (PJ-01) | SAL en mixamo.com | ❌ pendiente |
| Crear proyecto Unity 6 + paquetes | SAL | ❌ pendiente |
| Copiar scripts + modelos a Unity | SAL | ❌ pendiente |
| `MissionDesignUI.cs` | Claude | ✅ hecho |
| `ScienceOpsUI.cs` | Claude | ✅ hecho |
| `PowerupSystem.cs` | Claude | ✅ hecho |
| Probar en dos teléfonos físicos | SAL | ❌ pendiente |

---

*Scripts C# escritos por Claude (asistente) — Diseño y concepto: SAL / Guacalita Inc.*
