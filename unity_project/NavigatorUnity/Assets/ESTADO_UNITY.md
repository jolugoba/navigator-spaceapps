# NAVIGATOR 2.0 — Estado de los scripts Unity
**Guacalita Inc. | 2026-09-26**

Diseño completo y valores: [`NAVIGATOR_GAME_DESIGN_COMPLETE.md`](../../../NAVIGATOR_GAME_DESIGN_COMPLETE.md) ·
Cómo correrlo: [`NAVIGATOR_UNITY_QUICKSTART.md`](../../../NAVIGATOR_UNITY_QUICKSTART.md)

---

## Scripts (35)

### Core/
| Script | Qué hace |
|---|---|
| `MissionDesign.cs` | Tablas reales (Isp, masas, costos, potencia, ΔV mínimo), Tsiolkovsky, solar 1/d², validación |
| `MissionScore.cs` | Puntaje = llegada + ciencia + combustible + takedowns + gravity assist |
| `PhysicsEngine.cs` | Empuje por motor, gravedad μ/r² hacia el planeta |
| `ModelFit.cs` | Ajusta los GLB de Meshy a un tamaño conocido |

### Network/
| Script | Qué hace |
|---|---|
| `SendaBridge.cs` | P/Invoke a `senda_core` (Iroh). Callback estático para IL2CPP, remitente autenticado |
| `ShipSyncService.cs` | Estado de nave a 20 Hz, llegada, puntaje final, nombre del rival |
| `P2PChatSystem.cs` | Chat y avisos del sistema |
| `NasaHorizons.cs` | Azimut, altura y distancia reales del planeta (NASA JPL Horizons) |

### Game/
| Script | Qué hace |
|---|---|
| `ShipController.cs` | Empuje, boost, topes de velocidad, sin combustible = sin empuje |
| `FuelSystem.cs` | Segundos de empuje según el ΔV del diseño, fugas por choques |
| `BurnoutSystem.cs` | Barra Burnout, combos de near-miss, takedowns, EMP |
| `CollisionSystem.cs` | Choque elástico con masas reales, daño, takedowns |
| `PowerupSystem.cs` | 5 tipos de pods repartidos en la ruta; EMP enviado por la red |
| `AsteroidField.cs` | Asteroides en la ruta: near-miss y daño |
| `RemoteShipManager.cs` | Nave rival interpolada, etiqueta con nombre, colisionador para takedowns |
| `ShipCustomizer.cs`, `ShipAnimator.cs`, `ExplosionVFX.cs` | Colores, animación de motores, explosiones |

### AR/ — sin ARCore/ARKit: funciona en cualquier celular con giroscopio
| Script | Qué hace |
|---|---|
| `CameraFeedBackground.cs` | Cámara trasera detrás de la escena (permisos, rotación, espejo) |
| `GyroCameraRig.cs` | La cámara virtual gira con el celular y persigue a la nave; alinea con el norte |
| `ARShipController.cs` | Apuntar = girar, 1 dedo = empuje, 2 dedos / doble toque = boost |
| `ARSpaceManager.cs` | Coloca el planeta destino a la distancia del viaje |
| `ARCompassTracker.cs` | GPS + Horizons → dirección real del planeta; respaldo sin GPS |
| `ARSessionManager.cs` | Arranque del modo AR y recalibración |

### UI/
| Script | Qué hace |
|---|---|
| `GameManager.cs` | Máquina de estados, modo solo, misión fallida, volver a jugar |
| `LobbyManager.cs` | QR propio, escáner con vista previa, "Jugar solo" / "¡Despegar!" |
| `MissionDesignUI.cs` | Pantalla de diseño; LANZAR solo con diseño válido |
| `CockpitHUD.cs` | Velocidad, combustible, progreso, tiempo, flecha al planeta, Burnout, casco |
| `ScienceOpsUI.cs` | Instrumentos limitados por energía, puntos en vivo |
| `ScoreboardUI.cs` | Revelado animado; espera al rival antes de declarar ganador |
| `UIStyle.cs`, `SafeAreaFitter.cs` | Paleta del juego, respeto al notch |

### Editor/ (menú **Navigator**)
| Script | Qué hace |
|---|---|
| `NavigatorSceneSetup.cs` | **Setup Scene**: escena completa, prefabs generados, modelos y UI |
| `NavigatorUIBuilder.cs` | **Build UI**: los 6 paneles con todos los campos conectados |
| `NavigatorAssignModels.cs` | **Assign Models**: GLB de Meshy (prefiere `_FINAL`) |

---

## Pendiente
- iOS: compilar `libsenda_core.a` (estática) para `__Internal`
- Detector de near-miss con la nave rival (el método de Burnout ya existe)
- Dar efecto de juego al objetivo de misión (sobrevuelo / órbita / aterrizaje)
- Sonido (los campos de audio de `ExplosionVFX` están listos)
