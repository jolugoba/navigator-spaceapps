# NAVIGATOR — Juego Espacial Multiplayer P2P
**Guacalita Inc. · 2026 NASA Space Apps Challenge**
**Challenge objetivo:** Space Mission Design Game / Build a Junior Astronaut Mission Trainer

---

## Visión del Juego

NAVIGATOR es un simulador espacial en primera persona de movimiento libre, estilo Need for Speed pero en el espacio. El jugador pilota su nave desde el cockpit, con control total en 3D, posiciones planetarias reales de NASA, y multijugador P2P cifrado post-cuánticamente sin ningún servidor central. Hasta N jugadores se conectan escaneando un QR, ven las naves de los demás en tiempo real y pueden comunicarse, hacer carreras o misiones cooperativas.

**Sin gafas VR. Sin servidor. Sin internet requerido. Solo teléfonos.**

---

## Experiencia de Juego

### Vista primera persona — Cockpit
- El jugador está sentado dentro de su nave, ve el espacio desde el interior del cockpit
- Giroscopio del teléfono = mover la cabeza dentro del cockpit (mirar izquierda, derecha, arriba, abajo) independiente de hacia donde vuela la nave
- Estrellas reales donde realmente están en el cielo (catálogo astronómico)
- Planetas visibles a su posición real actual (NASA Horizons API)
- HUD superpuesto: velocidad, destino, combustible, radar de naves cercanas, comunicaciones

### Control de Vuelo — Modo Need for Speed
- **Joystick izquierdo:** pitch y yaw (hacia dónde apunta la nave)
- **Joystick derecho:** roll y aceleración lateral
- **Botón boost/nitro:** aceleración máxima
- **Sin freno automático:** física newtoniana simplificada — si aceleras, sigues moviéndote; hay que contrafrenar
- **Modo arcade:** física suficientemente real para ser emocionante, no tan compleja como Kerbal
- El control se siente como un auto de carrera pero en 3 dimensiones y sin gravedad

### Misiones
1. **Definir misión:** elegir planeta destino (Marte, Júpiter, Venus, etc.)
2. **Lanzamiento:** animación de cohete saliendo desde la posición del jugador
3. **Vuelo libre:** el jugador controla la nave manualmente durante el viaje
4. **Llegada:** efectos visuales al alcanzar el planeta
5. **Tiempo real vs escala:** el juego usa escala de tiempo acelerada para que los viajes sean jugables en minutos, no años

### Multijugador P2P
- Abres la app → aparece tu QR con tu NodeId
- Otro jugador escanea tu QR → conexión cifrada post-cuántica automática
- Desde ese momento ambos ven las naves del otro en el espacio, en tiempo real
- Chat de voz/texto cifrado entre naves
- Se pueden conectar N jugadores en red mesh (no solo 2)
- Con 10 naves: ves las 10 en el espacio, etiquetadas con el nombre del piloto
- Interacción al acercarse: acoplar naves, pasar recursos, chat de proximidad

### Modos de Juego
| Modo | Descripción |
|---|---|
| **Libre** | Exploración sin objetivo, vuela a donde quieras |
| **Misión** | Define destino, llega primero, tiempo récord |
| **Carrera** | Varios jugadores, mismo destino, primero en llegar gana |
| **Cooperativo** | Misión conjunta, recursos compartidos entre naves |
| **Defensa** | Oleadas de asteroides/amenazas, proteger la base (OrbitalDefense adaptado) |

---

## Arquitectura Técnica

```
┌─────────────────────────────────────────────────────────────────┐
│                        NAVIGATOR APP (Flutter)                   │
│                                                                  │
│  ┌─────────────────┐   ┌──────────────────┐   ┌─────────────┐  │
│  │   RENDER 3D     │   │   JUEGO / ECS    │   │  RED P2P    │  │
│  │ filament_scene  │   │  fluorite ECS    │   │  senda_p2p  │  │
│  │ (tcna-packages) │   │  (ref: fluorite) │   │  (senda_otf)│  │
│  └────────┬────────┘   └────────┬─────────┘   └──────┬──────┘  │
│           │                     │                      │         │
│  ┌────────▼─────────────────────▼──────────────────────▼──────┐ │
│  │                    NAVIGATOR CORE                           │ │
│  │  SpaceWorld · CockpitView · FlightController                │ │
│  │  MissionSystem · ShipSyncService · StarCatalog              │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌──────────────────┐   ┌─────────────────┐                     │
│  │  GIROSCOPIO      │   │  NASA DATA      │                     │
│  │  MethodChannel   │   │  Horizons API   │                     │
│  │  (Kotlin nativo) │   │  (posiciones    │                     │
│  │  ENABLE_GYRO /   │   │   planetarias)  │                     │
│  │  DISABLE_GYRO    │   │                 │                     │
│  └──────────────────┘   └─────────────────┘                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Stack Tecnológico Completo

### Ya construido — se reutiliza directamente

#### 1. Motor 3D — `filament_scene` (tcna-packages-2.0)
**Ubicación:** `C:\laragonbk\www\dada\tcna-packages-2.0\packages\filament_scene`
**Importar en pubspec:**
```yaml
filament_scene:
  path: ../tcna-packages-2.0/packages/filament_scene
```

**Lo que nos da:**
- `SceneView` widget — renderiza la escena 3D en Flutter
- `SceneController` — actualiza escena en runtime
- `GlbModel` — carga modelos 3D de naves, planetas (`.glb`)
- `KtxSkybox` / `HdrSkybox` — fondo estelar inmersivo
- `Light` — sol, luces dinámicas, efectos de motor
- `TransformEntity.setLocalPosition()` — mover naves en tiempo real
- `TransformEntity.setLocalRotationFromEuler()` — rotar naves
- PBR rendering — materiales realistas para naves y planetas

**Ejemplo de uso en NAVIGATOR:**
```dart
// Cockpit view con skybox estelar
final scene = Scene(
  skybox: KtxSkybox(assetPath: 'assets/skybox/stars_milky_way.ktx'),
  indirectLight: KtxIndirectLight(assetPath: 'assets/ibl/space_ibl.ktx'),
  lights: [
    Light(id: 1, type: LightType.sun, colorTemperature: 5778.0,
          direction: Vector3(sunDx, sunDy, sunDz), castShadows: true),
  ],
);
```

#### 2. Red P2P Post-Cuántica — `senda_p2p_chat` (senda_otf)
**Ubicación:** `C:\laragonbk\www\dada\senda_otf\senda_p2p_chat`
**Importar en pubspec:**
```yaml
senda_p2p_chat:
  path: ../senda_otf/senda_p2p_chat
```

**APIs clave para NAVIGATOR:**
```dart
final p2p = SendaP2PClient();

// Mostrar mi QR para que otros se conecten
String get nodeId  // → mostrar como QR en pantalla

// Conectar con otro jugador
final contact = await p2p.connectToNode(scannedNodeId);

// ENVIAR estado de mi nave (posición, velocidad, rotación)
await p2p.send(contact, jsonEncode({
  'type': 'ship_state',
  'pos': [x, y, z],
  'vel': [vx, vy, vz],
  'rot': [pitch, yaw, roll],
  'mission': 'MARS',
  'player': 'SAL',
}));

// RECIBIR estados de otras naves
p2p.onMessage.listen((msg) {
  final data = jsonDecode(msg.text);
  if (data['type'] == 'ship_state') {
    // Actualizar posición de la nave del otro jugador en la escena
    _updateRemoteShip(msg.fromNodeId, data);
  }
});

// Detectar nuevas conexiones
p2p.onPeerConnected.listen((contact) {
  // Nuevo jugador entró — crear su nave en la escena
  _spawnRemoteShip(contact.nodeId);
});

// Detectar desconexiones
p2p.onPeerDisconnected.listen((nodeId) {
  // Jugador se fue — quitar su nave de la escena
  _removeRemoteShip(nodeId);
});
```

**Protocolo de mensajes NAVIGATOR:**
```dart
// Tipos de mensaje:
// 'ship_state'   — posición/velocidad/rotación cada ~50ms
// 'ship_chat'    — mensaje de texto entre jugadores
// 'ship_event'   — evento especial (llegó a planeta, boost, etc.)
// 'mission_sync' — sincronizar estado de misión
// 'lobby_info'   — info del jugador (nombre, nave elegida)
```

#### 3. Motor ECS + Giroscopio — `fluorite_test`
**Ubicación:** `C:\laragonbk\www\dada\fluorite_test`
**No se importa como paquete — se usa como referencia de código**

**Lo que se extrae/adapta:**

*ECS para NAVIGATOR:*
```dart
// Componentes que necesita NAVIGATOR (basados en fluorite ECS)
class ShipTransform extends Component {
  Vec3 position;     // Posición en el espacio (unidades de juego)
  double pitch;      // Rotación X
  double yaw;        // Rotación Y
  double roll;       // Rotación Z
}

class ShipVelocity extends Component {
  Vec3 linear;       // Velocidad lineal (m/s escalado)
  Vec3 angular;      // Velocidad angular
}

class ShipThrust extends Component {
  double main;       // Motor principal (0.0 - 1.0)
  double lateral;    // Motor lateral
  double boost;      // Nitro
}

class ShipOwner extends Component {
  final String nodeId;    // ID Senda del jugador
  final String displayName;
  final bool isLocal;     // true = nave del jugador local
}

class MissionTarget extends Component {
  final String planetId;  // 'MARS', 'JUPITER', etc.
  double distanceAU;      // Distancia actual en UA
  DateTime? arrivedAt;
}
```

*Giroscopio (MethodChannel de fluorite_test):*
```dart
// En Kotlin (MainActivity.kt) — MISMO PATRÓN que fluorite_test
// Registrar canal:
val channel = MethodChannel(flutterEngine.dartExecutor.binaryMessenger,
    "com.guacalita.navigator/gyro")

// Habilitar giroscopio
channel.setMethodCallHandler { call, result ->
  when (call.method) {
    "ENABLE_GYRO" -> {
      sensorManager.registerListener(gyroListener,
          sensorManager.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR),
          SensorManager.SENSOR_DELAY_GAME)
      result.success(null)
    }
    "DISABLE_GYRO" -> {
      sensorManager.unregisterListener(gyroListener)
      result.success(null)
    }
  }
}

// Enviar datos de giroscopio a Flutter
channel.invokeMethod("GYRO_UPDATE", mapOf(
    "azimuth" to azimuth,  // Mirar izquierda/derecha
    "pitch" to pitch,      // Mirar arriba/abajo
    "roll" to roll         // Inclinar
))
```

```dart
// En Flutter — recibir giroscopio para mover la cámara del cockpit
final _gyroChannel = MethodChannel('com.guacalita.navigator/gyro');

void _initGyro() {
  _gyroChannel.invokeMethod('ENABLE_GYRO');
  _gyroChannel.setMethodCallHandler((call) async {
    if (call.method == 'GYRO_UPDATE') {
      final azimuth = call.arguments['azimuth'] as double;
      final pitch = call.arguments['pitch'] as double;
      _updateCockpitView(azimuth, pitch);
    }
  });
}
```

### Por construir — nuevo en NAVIGATOR

#### 4. NASA Horizons API — Posiciones Planetarias Reales
```dart
// Obtiene posición actual de planetas en coordenadas Az/Alt desde ubicación del jugador
class NasaHorizonsService {
  // GET https://ssd.jpl.nasa.gov/api/horizons.api
  // Parámetros: COMMAND (planeta), CENTER (ubicación GPS), QUANTITIES (Az, Alt, distancia)
  
  Future<PlanetPosition> getPlanetPosition({
    required String planetId,   // '499' = Marte, '599' = Júpiter, etc.
    required double lat,
    required double lon,
    required DateTime time,
  });
}

class PlanetPosition {
  final double azimuthDeg;      // Donde está en el cielo (0-360°)
  final double altitudeDeg;     // Altura sobre el horizonte
  final double distanceAU;      // Distancia en Unidades Astronómicas
  final String planetName;
}
```

**IDs de planetas NASA:**
| Planeta | ID |
|---|---|
| Mercurio | 199 |
| Venus | 299 |
| Marte | 499 |
| Júpiter | 599 |
| Saturno | 699 |
| Urano | 799 |
| Neptuno | 899 |

#### 5. Star Catalog — Fondo Estelar
```dart
// Catálogo simplificado de las 9,096 estrellas visibles a simple vista (Hipparcos)
// Se renderiza como skybox dinámico o partículas 3D
// Opción más simple: skybox HDR/KTX de la Vía Láctea (asset estático de alta calidad)
// Opción avanzada: posiciones reales en tiempo real (RA/Dec → Az/Alt con ubicación GPS)
```

**Asset recomendado para hackathon:** Skybox HDR de la Vía Láctea (disponible gratis en HDRI Haven).

#### 6. Flight Controller — Joysticks
```dart
// Joysticks táctiles adaptados de flu_doom-main
class FlightController extends StatefulWidget {
  final void Function(double pitch, double yaw) onSteer;   // Dirección
  final void Function(double roll, double thrust) onThrust; // Impulso
  final VoidCallback onBoost;
  final VoidCallback onFire;
  final VoidCallback onComm;   // Abrir chat
}
```

#### 7. Ship Sync Service — Multiplayer en Tiempo Real
```dart
// Envía estado de nave cada 50ms a todos los peers conectados
// Recibe estados de todos los peers y actualiza sus naves en la escena
class ShipSyncService {
  final SendaP2PClient _p2p;
  final Map<String, RemoteShip> _remoteShips = {};

  Timer? _broadcastTimer;

  void startBroadcast(ShipState localShip) {
    _broadcastTimer = Timer.periodic(
      const Duration(milliseconds: 50),
      (_) => _broadcast(localShip),
    );
  }

  Future<void> _broadcast(ShipState ship) async {
    final msg = jsonEncode(ship.toJson());
    for (final contact in _p2p.contacts) {
      await _p2p.send(contact, msg);
    }
  }
}
```

---

## Estructura del Proyecto navigator/

```
navigator/
├── lib/
│   ├── main.dart                      # Punto de entrada
│   ├── core/
│   │   ├── space_world.dart           # ECS del espacio (basado en fluorite ECS)
│   │   ├── components.dart            # ShipTransform, ShipVelocity, etc.
│   │   ├── systems.dart               # FlightSystem, SyncSystem, etc.
│   │   └── vec3.dart                  # Matemáticas 3D
│   ├── flight/
│   │   ├── flight_controller.dart     # Joysticks + controles táctiles
│   │   ├── cockpit_view.dart          # Vista primera persona + giroscopio
│   │   ├── physics_engine.dart        # Física newtoniana simplificada
│   │   └── gyro_service.dart          # MethodChannel giroscopio
│   ├── render/
│   │   ├── space_scene.dart           # Escena filament_scene configurada
│   │   ├── ship_renderer.dart         # Renderizar naves locales y remotas
│   │   ├── planet_renderer.dart       # Renderizar planetas en posición real
│   │   └── star_skybox.dart           # Skybox estelar
│   ├── network/
│   │   ├── ship_sync_service.dart     # Broadcast/receive posiciones
│   │   ├── lobby_service.dart         # Descubrimiento de jugadores
│   │   └── nav_protocol.dart          # Tipos de mensajes P2P
│   ├── nasa/
│   │   ├── horizons_service.dart      # NASA Horizons API
│   │   └── planet_positions.dart      # Cache de posiciones planetarias
│   ├── mission/
│   │   ├── mission_manager.dart       # Definir y rastrear misiones
│   │   └── mission_hud.dart           # HUD de misión activa
│   ├── ui/
│   │   ├── main_menu.dart             # Menú principal
│   │   ├── lobby_screen.dart          # QR + lista de jugadores conectados
│   │   ├── hud_overlay.dart           # HUD en vuelo (velocidad, mapa, chat)
│   │   ├── mission_select.dart        # Elegir planeta destino
│   │   └── chat_overlay.dart          # Chat en vuelo
│   └── models/
│       ├── ship_state.dart            # Estado serializable de la nave
│       ├── player_info.dart           # Info del jugador
│       └── planet_data.dart           # Datos de planetas
├── android/
│   └── app/src/main/kotlin/.../
│       └── MainActivity.kt            # MethodChannel giroscopio
├── assets/
│   ├── skybox/
│   │   └── milky_way.ktx              # Fondo estelar (HDRI)
│   ├── ibl/
│   │   └── space_ibl.ktx              # Iluminación indirecta espacial
│   └── models/
│       ├── ships/
│       │   ├── ship_player.glb        # Nave del jugador
│       │   ├── ship_remote.glb        # Nave de otros jugadores
│       │   └── ship_[N].glb           # Variantes para elegir
│       └── planets/
│           ├── mars.glb
│           ├── earth.glb
│           ├── jupiter.glb
│           └── saturn.glb
└── pubspec.yaml
```

---

## pubspec.yaml (borrador)

```yaml
name: navigator
description: Space multiplayer P2P game - NASA Space Apps 2026
version: 1.0.0+1

environment:
  sdk: ^3.7.0
  flutter: ^3.27.0

dependencies:
  flutter:
    sdk: flutter

  # Motor 3D (reutilizado de tcna-packages-2.0)
  filament_scene:
    path: ../tcna-packages-2.0/packages/filament_scene

  # Red P2P post-cuántica (reutilizado de senda_otf)
  senda_p2p_chat:
    path: ../senda_otf/senda_p2p_chat

  # QR scanner para conexión P2P
  mobile_scanner: ^7.0.0

  # QR display
  qr_flutter: ^4.1.0

  # HTTP para NASA Horizons API
  http: ^1.2.0

  # Serialización JSON
  json_annotation: ^4.9.0

  # Joysticks táctiles
  flutter_joystick: ^0.0.3

  # Ubicación GPS (para posiciones astronómicas reales)
  geolocator: ^13.0.0

  # Sensores (giroscopio fallback Dart si no MethodChannel)
  sensors_plus: ^6.0.0

  # Almacenamiento local
  shared_preferences: ^2.3.2

  # Pantalla completa
  window_manager: ^0.5.1

dev_dependencies:
  flutter_test:
    sdk: flutter
  flutter_lints: ^6.0.0
```

---

## Flujo de Experiencia Completo

```
1. ABRIR APP
   └─ Pantalla principal con fondo estelar animado
   └─ Botones: [JUGAR SOLO] [CONECTAR JUGADORES]

2. LOBBY — CONECTAR JUGADORES
   └─ Muestra QR con NodeId propio
   └─ Botón "Escanear QR" para agregar otro jugador
   └─ Lista de jugadores conectados con nombres de nave
   └─ Cuando todos listos: [INICIAR]

3. SELECCIÓN DE MISIÓN
   └─ Mapa del sistema solar (posiciones reales hoy)
   └─ Elegir planeta destino
   └─ Ver distancia real, tiempo estimado de viaje (escala del juego)
   └─ [LANZAR MISIÓN]

4. LANZAMIENTO
   └─ Animación de cuenta regresiva
   └─ Cohete despega, vista pasa a primera persona del cockpit
   └─ Giroscopio activado — girar teléfono = mirar alrededor

5. VUELO LIBRE
   └─ Joystick izquierdo: dirección
   └─ Joystick derecho: empuje lateral
   └─ Botón BOOST: aceleración máxima
   └─ Estrellas reales, sol, planetas visibles en posición correcta
   └─ Otras naves visibles en el espacio, con nombre del piloto encima
   └─ Radar en HUD: puntos de naves cercanas
   └─ Chat: tocar nave de otro jugador → mensaje directo cifrado

6. LLEGADA AL PLANETA
   └─ Efectos visuales de entrada orbital
   └─ Mensaje de misión completada
   └─ Ranking de llegada si era carrera
   └─ Opciones: explorar, nueva misión, volver al lobby

7. MODO DEFENSA (OrbitalDefense adaptado)
   └─ Oleadas de asteroides/amenazas atacan la base
   └─ Varios jugadores defienden cooperativamente
   └─ Cada nave tiene sus propias armas y posición en el espacio
```

---

## Ventajas Competitivas para NASA Space Apps

| Característica | NAVIGATOR | Competencia típica |
|---|---|---|
| P2P post-cuántico sin servidor | ✅ Senda (ya construido) | ❌ Servidor centralizado |
| Posiciones planetarias NASA reales | ✅ Horizons API | ⚠️ Datos estáticos |
| Primera persona libre en 3D | ✅ Filament PBR | ❌ Vista 2D o isométrica |
| Giroscopio cockpit | ✅ Kotlin nativo | ❌ Solo touch |
| Multijugador local sin internet | ✅ Iroh P2P WiFi | ❌ Necesita internet |
| Stack Flutter multiplataforma | ✅ Android/iOS | ❌ Una sola plataforma |

---

## Datos NASA Disponibles Gratis

| Dataset | URL | Para qué |
|---|---|---|
| Horizons API | ssd.jpl.nasa.gov/horizons | Posiciones planetas en tiempo real |
| Eyes on Solar System | eyes.nasa.gov | Referencia visual de escala |
| NASA 3D Resources | nasa3d.arc.nasa.gov | Modelos 3D de planetas y naves |
| NSSDCA Photo Gallery | nssdc.gsfc.nasa.gov | Texturas de planetas |
| Exoplanet Archive | exoplanetarchive.ipac.caltech.edu | Planetas extra-solares |
| Solar Dynamics Observatory | sdo.gsfc.nasa.gov | Texturas del sol en tiempo real |

---

## Modelo 3D Assets Necesarios

| Asset | Formato | Fuente sugerida |
|---|---|---|
| Nave jugador (principal) | `.glb` PBR | NASA 3D Resources / Sketchfab |
| Nave otros jugadores (variantes) | `.glb` | NASA 3D Resources |
| Marte | `.glb` con textura | NASA 3D Resources (disponible) |
| Tierra | `.glb` con textura | NASA 3D Resources (disponible) |
| Júpiter | `.glb` con textura | NASA 3D Resources (disponible) |
| Sol | `.glb` emisivo | NASA 3D Resources |
| Skybox Vía Láctea | `.ktx` | HDRI Haven (gratis CC0) |
| Efectos motor | partículas Dart | Construir |

---

## Plan de Desarrollo — Hackathon (7-10 días)

### Día 1-2: Cimientos
- [ ] Crear proyecto Flutter `navigator/`
- [ ] Integrar `filament_scene` — escena espacial básica con skybox estelar
- [ ] Integrar `senda_p2p_chat` — conexión QR entre 2 teléfonos
- [ ] MethodChannel giroscopio (copiar patrón de fluorite_test HolographicPage)

### Día 3-4: Vuelo
- [ ] Cockpit view en primera persona (Filament + cámara controlada por código)
- [ ] Joysticks táctiles (FlightController)
- [ ] Física de vuelo libre newtoniana simplificada
- [ ] Giroscopio mueve la cámara del cockpit

### Día 5: Multijugador
- [ ] ShipSyncService — broadcast posición cada 50ms por Senda
- [ ] Ver nave de otro jugador en la escena en tiempo real
- [ ] Chat entre jugadores (overlay en vuelo)
- [ ] Lobby screen con QR

### Día 6: Datos NASA
- [ ] NASA Horizons API — posiciones reales de planetas
- [ ] Planetas renderizados donde realmente están hoy
- [ ] Misión: elegir planeta, seguir ruta

### Día 7: Pulido
- [ ] HUD completo (radar, velocidad, misión, chat)
- [ ] Efectos: motor, boost, llegada al planeta
- [ ] Menú principal, selección de nave
- [ ] Tests con múltiples jugadores

### Día 8-10: Presentación
- [ ] Video demo 2 minutos (requerido por Devpost)
- [ ] Llenar formulario NASA Space Apps
- [ ] APK de demo

---

## Notas Técnicas Importantes

### Sincronización de posición entre naves
- Coordenadas en espacio de juego (float), no astronómicas reales (muy grandes)
- Planetas a escala relativa del juego, no a escala 1:1 del sistema solar
- Cada 50ms broadcast de posición — con Senda P2P la latencia es muy baja en LAN

### Giroscopio vs AR
- **NO usar AR** (camera feed) — primera persona pura
- Giroscopio controla solo la cámara del cockpit (donde mira el piloto)
- La dirección de vuelo es independiente — se controla con joystick
- Esto permite el estilo Need for Speed: la nave va en una dirección mientras el piloto mira a otro lado

### Física simplificada
- No implementar física orbital de Kerbal (muy compleja para hackathon)
- Física newtoniana simple: F = ma, sin gravedad de planetas, friccción de espacio cero
- "Nitro espacial" para carreras emocionantes
- Colisiones básicas entre naves (rebote suave)

### Red P2P
- Senda funciona en LAN WiFi local sin internet
- Para hackathon: todos conectados al mismo hotspot
- Protocolo de mensajes ligero: JSON serializado, ~200 bytes por paquete de posición
- Si hay lag: interpolación suave entre posiciones recibidas

---

## Challenge NASA Space Apps — Criterios de Evaluación

Los jueces evalúan:
1. **Relevancia con el desafío** — misiones espaciales reales, datos NASA ✅
2. **Creatividad e innovación** — P2P post-cuántico, primera persona libre ✅
3. **Implementación técnica** — Flutter + Filament + Rust FFI ✅
4. **Potencial de impacto** — educación, accesible en móvil ✅
5. **Presentación** — video demo, documentación ✅

**Challenge más apropiado:** "Space Mission Design Game"
> "Create an interactive game that allows participants to design, manage, and simulate a complete space mission while making engineering decisions, managing limited resources, and evaluating how each choice shapes the success of their mission."

NAVIGATOR cumple esto con: libre exploración, misiones reales, física de vuelo, multijugador cooperativo.

---

*Guacalita Inc. · Todos los derechos reservados*
*Diseñado y conceptualizado por SAL · Documentado 2026-09-20*
