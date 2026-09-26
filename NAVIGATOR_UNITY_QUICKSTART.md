# NAVIGATOR 2.0 — Quickstart Unity 6
**Guacalita Inc. | Todo preparado, solo copiar y abrir**

---

## PASO 1 — Crear proyecto Unity 6 LTS

1. Abrir Unity Hub → New Project
2. Template: **Universal 3D (URP)**
3. Nombre: `Navigator`
4. Crear

---

## PASO 2 — Copiar archivos preparados (antes de abrir Unity)

Desde `c:\laragonbk\www\dada\navigator\`:

```
Copiar → al proyecto Unity:
  Packages/manifest.json     →  [ProyectoUnity]/Packages/manifest.json   (reemplazar)
  Assets/Plugins/            →  [ProyectoUnity]/Assets/Plugins/
  unity_scripts/             →  [ProyectoUnity]/Assets/Scripts/
```

El `manifest.json` instala automáticamente:
- AR Foundation 6.0.3
- ARCore XR Plugin (Android)
- ARKit XR Plugin (iOS)
- URP 17.0.3
- Input System 1.9.0
- TextMeshPro 3.0.6 (estable)
- UGUI 2.0.0
- Newtonsoft.Json 3.2.1

---

## PASO 3 — Abrir Unity

Unity detecta los paquetes y los descarga (~5 min). Esperar que compile sin errores.

---

## PASO 4 — Instalar ZXing (QR codes)

Este paquete no está en Package Manager — se instala del Asset Store:

1. Window → Asset Store → buscar **ZXing.Net.Unity**
2. Es gratuito — Import → importar todo
3. Alternativa: descargar `.unitypackage` de github.com/Bunny83/Unity-ZXing

---

## PASO 5 — Configurar XR Plug-in Management

1. Edit → Project Settings → XR Plug-in Management
2. Tab Android: activar **ARCore**
3. Tab iOS: activar **ARKit**

---

## PASO 6 — Configurar Build Settings Android

1. File → Build Settings → Android → Switch Platform
2. Player Settings:
   - Company Name: `Guacalita`
   - Package Name: `com.guacalita.navigator`
   - Minimum API Level: **26** (Android 8.0)
   - Target API Level: **34**
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64**
   - **Active Input Handling: Both** ← sin esto `Input.gyro` no compila con Input System 1.9

---

## PASO 7 — Copiar senda_core.dll

```
Desde: senda/native/senda_core/target/release/senda_core.dll
  → Assets/Plugins/senda_core.dll

Desde: senda/native/senda_core/target/aarch64-linux-android/release/libsenda_core.so
  → Assets/Plugins/Android/libsenda_core.so
```

---

## PASO 8 — Setup automático de la escena ⚡

1. Menu: **Navigator → Setup Scene**
2. Guarda la escena como `SpaceFlight.unity`

Esto crea automáticamente:
- AR Session
- XR Origin + Camera Offset + Main Camera con ARCameraBackground
- LocalShip con todos los componentes conectados
- GameManager con SendaBridge, ShipSyncService, P2PChatSystem
- CockpitHUD Canvas WorldSpace
- PowerupSystem
- ARSessionManager, ARCompassTracker, ARSpaceManager

---

## PASO 9 — Asignar en el Inspector (lo que falta después del setup)

| Objeto | Campo | Asignar |
|--------|-------|---------|
| LocalShip → ShipAnimator | NozzlesLeft/Right | sub-mesh del modelo NV-01 |
| LocalShip → ShipAnimator | WingLeft/Right | sub-mesh del modelo NV-01 |
| LocalShip → ShipAnimator | ExhaustLeft/Right | ParticleSystem del modelo |
| GameManager → RemoteShipManager | RemoteShipPrefab | prefab NV-02 KRONOS |
| GameManager → RemoteShipManager | NameTagPrefab | Canvas prefab con TMP_Text |
| PowerupSystem | FuelPodPrefab … | prefabs PW-01 a PW-05 |
| ARSpaceManager | PlanetPrefabs | prefab PL-03 Marte |

---

## PASO 10 — Escena Lobby.unity (conexión QR)

1. File → New Scene → Save as `Lobby.unity`
2. Crear Canvas (Screen Space Overlay)
3. Añadir `LobbyManager.cs` a un GameObject
4. Asignar: MyQRImage (RawImage), ScanButton, StartGameButton
5. En OnClick de StartGameButton: cargar `SpaceFlight` con `SceneManager.LoadScene`

---

## ESTRUCTURA FINAL DE SCRIPTS

```
Assets/Scripts/
  Core/
    MissionDesign.cs
    MissionScore.cs
    PhysicsEngine.cs
    Navigator.Core.asmdef          ← compilación automática
  Network/
    SendaBridge.cs
    ShipSyncService.cs             ← contiene ShipStateNet
    P2PChatSystem.cs
    NasaHorizons.cs
    Navigator.Network.asmdef
  Game/
    ShipController.cs
    CollisionSystem.cs
    BurnoutSystem.cs
    FuelSystem.cs
    ShipCustomizer.cs
    ShipAnimator.cs
    RemoteShipManager.cs
    PowerupSystem.cs               ← contiene Powerup + PowerupSystem
    Navigator.Game.asmdef
  AR/
    ARSessionManager.cs
    ARShipController.cs
    ARCompassTracker.cs
    ARSpaceManager.cs
    Navigator.AR.asmdef
  UI/
    CockpitHUD.cs
    LobbyManager.cs
    MissionDesignUI.cs
    ScienceOpsUI.cs
    Navigator.UI.asmdef
  Editor/
    NavigatorSceneSetup.cs         ← Menu: Navigator → Setup Scene
    Navigator.Editor.asmdef
```

---

## JERARQUÍA DE ESCENA (SpaceFlight.unity)

```
AR Session                    [ARSession]
XR Origin
  └─ Camera Offset
       └─ Main Camera         [Camera] [ARCameraManager] [ARCameraBackground]
GameManager
  └─ SendaBridge
  └─ ShipSyncService
  └─ P2PChatSystem
  └─ RemoteShipManager
ARSessionManager
ARCompassTracker
ARSpaceManager
LocalShip                     tag: LocalShip
  └─ [Rigidbody]
  └─ ShipController
  └─ PhysicsEngine
  └─ FuelSystem
  └─ BurnoutSystem
  └─ CollisionSystem
  └─ ShipCustomizer
  └─ ShipAnimator
  └─ ARShipController
CockpitHUD                    [Canvas WorldSpace]
PowerupSystem
MissionScore
MissionDesignUI               [Canvas ScreenSpace — activar al inicio]
```

---

*Todos los scripts C# ya están escritos y auditados. Solo falta Unity + modelos Meshy.*
