# Navigator — app Flutter con Unity embebido

Esta carpeta es **la app que se instala en el celular**. Es una app Flutter que muestra el juego de Unity
(`unity_project/NavigatorUnity`) a pantalla completa, con el plugin
[`flutter_embed_unity`](https://pub.dev/packages/flutter_embed_unity) 2.0.

```
Flutter (flutter_app/)                     Unity (unity_project/NavigatorUnity/)
┌──────────────────────────────┐            ┌──────────────────────────────┐
│ Inicio: nombre del piloto,   │            │ Diseño de misión, vuelo AR,  │
│ idioma, INICIAR MISIÓN,      │ ─ nombre ─▶│ asistencias gravitatorias,   │
│ bitácora de misiones         │ ─ idioma ─▶│ ciencia, puntajes, P2P       │
│                              │◀─ mission ─│ (Assets/UI/FlutterBridge.cs) │
│ EmbedUnity (pantalla llena)  │◀── exit ───│                              │
└──────────────────────────────┘            └──────────────────────────────┘
```

| Mensaje | Dirección | Contenido |
|---|---|---|
| `SetLanguage("en"/"es")` | Flutter → Unity | El juego cambia de idioma |
| `SetPlayerName("Ana")` | Flutter → Unity | Nombre que ven los rivales y el marcador |
| `{"type":"ready"}` | Unity → Flutter | La escena cargó |
| `{"type":"mission", "planet", "objective", "score", "arrived", "stars", "costM", "timeS", "fuel", "science", …}` | Unity → Flutter | Resultado de cada misión; se guarda en la bitácora |
| `{"type":"exit"}` | Unity → Flutter | Atrás en el lobby: vuelve al inicio de Flutter |

---

## Requisitos

| Herramienta | Versión |
|---|---|
| Flutter | **3.35 o más nuevo** (probado con 3.47.5) |
| Unity | **6000.3 LTS** (6000.3.0f1 o más nuevo) o 6000.0 LTS (6000.0.58f2 o más nuevo) — ver aviso abajo |
| Android Studio | con Android SDK y **NDK r28** (el que Flutter pide por defecto) |
| JDK | 17 |
| iOS (opcional) | Mac con Xcode |

> ⚠️ **Versión de Unity.** El proyecto está abierto con Unity **6000.6.3f1**. El plugin solo garantiza
> **6000.0 LTS y 6000.3 LTS** porque usa funciones internas de Unity que cambian entre versiones.
> Con 6.6 puede funcionar, pero si la app se cierra al abrir el juego, instala Unity 6000.3 LTS desde Unity Hub,
> abre el proyecto con esa versión y exporta de nuevo.

---

## Android — paso a paso

1. **Unity:** abre `unity_project/NavigatorUnity` y corre **Navigator → Setup Scene** (como siempre).
2. **Unity:** menú **Navigator → Flutter → Export Android → flutter_app**.
   - La primera vez cambia la plataforma a Android. Cuando termine, **vuelve a elegir el mismo menú**.
   - Configura solo lo que el plugin exige: IL2CPP, ARMv7 + ARM64, *Application Entry Point = Activity*
     y *Export project*.
   - Exporta a `flutter_app/android/unityLibrary` y copia `unityStreamingAssets` a `android/gradle.properties`.
   - Al terminar sale "Listo: Unity quedó exportado…".
3. **Terminal**, en esta carpeta:
   ```bash
   flutter pub get
   flutter run            # con el celular conectado por USB (depuración USB activada)
   ```
   Para un APK que puedas compartir: `flutter build apk --release` → `build/app/outputs/flutter-apk/app-release.apk`.

Cada vez que cambies algo en Unity, repite el paso 2 y luego `flutter run`.

> Si compilas **antes** de exportar Unity, la app igual compila y abre. Al tocar *INICIAR MISIÓN*
> muestra "Falta exportar Unity" con estas instrucciones, en lugar de cerrarse.

## iOS — paso a paso (Mac)

1. **Unity:** **Navigator → Flutter → Export iOS → flutter_app** (requiere el módulo iOS de Unity).
2. `flutter pub get`, luego `cd ios && pod install`.
3. Abre `ios/Runner.xcworkspace` (**el workspace**, no el `.xcodeproj`) y:
   - *File → Add Files to "Runner"* → `ios/unityLibrary/Unity-iPhone.xcodeproj`.
   - *Runner → General → Frameworks, Libraries and Embedded Content → +* → `UnityFramework.framework` (*Embed & Sign*).
   - *Runner → Build Phases*: arrastra **Thin Binary** debajo de **Embed Frameworks**.
4. `flutter run` con el iPhone conectado.

---

## Problemas comunes

| Síntoma | Solución |
|---|---|
| `Could not get unknown property 'unityStreamingAssets'` | Vuelve a exportar desde Unity (paso 2): el menú escribe esa línea en `android/gradle.properties`. |
| `minSdkVersion … cannot be smaller than version …` | El menú de exportar lo corrige solo. A mano: `minSdk` en `android/app/build.gradle.kts` = *Minimum API Level* de Unity (hoy 26). |
| `NDK … ndkPath … different version` | El exportador quita el `ndkPath` de Unity. Instala en Android Studio el NDK que pida el error. |
| La app se cierra al girar el celular | `android:configChanges` ya está completo y la app está bloqueada en vertical. No lo quites del `AndroidManifest.xml`. |
| La app se cierra al abrir el juego | Usa Unity 6000.3 LTS (ver aviso de versión) y vuelve a exportar. |
| Pantalla negra en el juego | Permisos: la app pide cámara y ubicación al tocar INICIAR MISIÓN. Revísalos en Ajustes del sistema. |

`android/unityLibrary` y `ios/unityLibrary` pesan cientos de MB y **no se suben a git** (están en `.gitignore`):
cada máquina los genera con el paso 2.

## Código

| Archivo | Qué hace |
|---|---|
| `lib/main.dart` | App, tema, idioma guardado |
| `lib/src/home_screen.dart` | Inicio: nombre, idioma, INICIAR MISIÓN, bitácora |
| `lib/src/game_screen.dart` | `EmbedUnity` a pantalla completa, mensajes con Unity |
| `lib/src/unity_host.dart` | Canal con Unity y `navigator/host` (¿está Unity en esta build?) |
| `lib/src/mission_log.dart` | Resultados de misiones guardados en el celular |
| `android/app/src/main/kotlin/…/MainActivity.kt` | `FakeUnityPlayerActivity` + canal `hasUnity` |
| `unity_project/…/Assets/UI/FlutterBridge.cs` | Lado Unity del puente |
| `unity_project/…/Assets/FlutterEmbed/` | Scripts del plugin (MIT, v2.0.0) + menú *Navigator → Flutter* |

La carpeta `lib/` de la raíz del repositorio es la versión Flutter anterior del juego, hecha con `filament_scene`.
Queda como referencia: depende de paquetes locales que no están en el repositorio, así que no compila sola.
