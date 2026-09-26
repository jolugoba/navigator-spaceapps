# Navigator 2.0 — Guía rápida de Unity

Del repositorio clonado a jugar en dos celulares. El diseño completo del juego está en
[`NAVIGATOR_GAME_DESIGN_COMPLETE.md`](NAVIGATOR_GAME_DESIGN_COMPLETE.md).

---

## 1. Requisitos

- **Unity 6000.6.3f1** con el módulo **Android Build Support** (OpenJDK + SDK/NDK)
- **Git LFS** (`git lfs install` antes de clonar; los `.glb`, `.dll` y `.so` están en LFS)
- Celulares Android 8.0+ (API 26) con giroscopio. **No** necesitan ARCore.

## 2. Abrir el proyecto

1. Unity Hub → **Add** → `unity_project/NavigatorUnity`
2. Al abrir, Package Manager descarga los paquetes de `Packages/manifest.json`, entre ellos
   **glTFast** (importa los `.glb` de Meshy). La primera importación de los 51 modelos tarda unos minutos.
3. **Window → TextMeshPro → Import TMP Essential Resources** → *Import TMP Essentials*.

### Player Settings (ya vienen configurados; verificar si algo falla)
| Ajuste | Valor |
|---|---|
| Active Input Handling | **Both** (el giroscopio usa `Input.gyro`) |
| Default Orientation | **Portrait** (lo fija también *Setup Scene*) |
| Scripting Backend (Android) | **IL2CPP** |
| Target Architectures | **ARM64** |
| Minimum API Level | **26** |

## 3. Crear la escena

1. Abrir `Assets/Scenes/SpaceFlight.unity`
2. Menú **Navigator → Setup Scene**
3. **Ctrl+S**

*Setup Scene* se puede repetir cuando quieras: reconstruye todo sin duplicar. Hace esto:
- Cámara con `GyroCameraRig` (giroscopio + persecución) y `CameraFeedBackground` (cámara trasera de fondo)
- Nave local con física, combustible, Burnout, colisiones y controles AR
- Planeta destino, brújula + NASA Horizons, pods, asteroides, explosiones
- Red P2P (Senda), naves rivales con etiqueta de nombre
- Asigna los modelos GLB (`NV-01`, `NV-02`, `PL-01…07`, `OB-01`)
- Construye la UI completa: Lobby, Diseño de misión, Cuenta atrás, HUD, Ciencia, Scoreboard
- Agrega la escena a la lista de build

Si el diálogo final dice **"Modelos GLB asignados: 0"**, glTFast no está instalado:
Package Manager → *Add package by name* → `com.unity.cloud.gltfast` → y vuelve a ejecutar *Setup Scene*.

## 4. Probar en el Editor

**Play** → **JUGAR SOLO** → ajusta el diseño → **LANZAR MISIÓN**.

| Tecla | Acción |
|---|---|
| Clic izquierdo o **W** | Acelerar |
| **Espacio** | Boost |
| Clic derecho + arrastrar, flechas | Mirar / girar la nave |

En Windows, el Editor carga `Assets/Plugins/senda_core.dll` y el QR funciona.
En macOS no hay librería nativa para el Editor: se juega en modo solo.

## 5. Build Android

1. **File → Build Profiles → Android → Switch Platform**
2. **Build** (o *Build And Run* con el celular conectado por USB)
3. `libsenda_core.so` ya está en `Assets/Plugins/Android/libs/{arm64-v8a, armeabi-v7a, x86_64}`

Unity agrega solo los permisos de **cámara** (`WebCamTexture`) y **ubicación** (`Input.location`).

## 6. Jugar en dos celulares

1. Ambos abren la app: cada uno ve su QR.
2. El jugador B toca **Escanear QR** y apunta al QR de A.
3. Cuando dice "2 jugador(es) conectados", ambos tocan **¡DESPEGAR!**.
4. Cada uno diseña su nave → **LANZAR** → apunta el celular hacia el planeta y mantén el dedo en pantalla.
5. Al llegar, espera a que terminen los instrumentos → **COMPLETAR MISIÓN** → Scoreboard.

Con GPS y datos móviles, el planeta aparece en su **dirección real en el cielo** (NASA JPL Horizons).
Sin GPS aparece al frente, un poco arriba.

## 7. Problemas comunes

| Síntoma | Causa / solución |
|---|---|
| Textos invisibles o cuadrados rosas | Falta TMP Essentials (paso 2.3) |
| Planetas son esferas de colores | glTFast no instalado o `.glb` sin LFS (`git lfs pull`) |
| Fondo negro en vez de la cámara | Permiso de cámara denegado: Ajustes → Apps → Navigator → Permisos |
| "Red P2P no disponible" | Falta la librería nativa para esa plataforma; se puede jugar solo |
| La nave no gira al mover el celular | El celular no tiene giroscopio |
| El planeta está "detrás" | Sigue la flecha del HUD o gira sobre ti mismo |
| iOS no compila | Falta `libsenda_core.a` (librería estática para `__Internal`) |
