# Navigator en la web (Unity WebGL) — offline y multijugador local

El mismo juego de Unity, exportado como página web. Se abre desde un link en cualquier celular,
se puede **instalar** y después **funciona sin internet**, y dos o más celulares juegan juntos
**sin servidor**, en la misma red Wi-Fi (o con un celular compartiendo datos), aunque esa red no
tenga internet.

## 1. Antes de compilar

1. Optimiza los modelos para que la página cargue rápido:
   `cd tools && npm install && node optimize_models.mjs` (baja los modelos de 1,387 MB a 133 MB).
2. En Unity: **Navigator → Setup Scene**.

## 2. Compilar

- **Navigator → Web → Build**: configura todo y compila en `unity_project/NavigatorUnity/WebBuild/`.
- O, si prefieres tus perfiles: **Navigator → Web → Prepare** y luego **Build** en el perfil
  **Web - Mobile - Release** (texturas ASTC para celular).

Lo que configura *Prepare*:

| Ajuste | Por qué |
|---|---|
| Plantilla **PWA** | Se puede instalar y un *service worker* guarda el juego → **abre sin internet** |
| **Gzip + decompression fallback** | Funciona en cualquier hosting (GitHub Pages, itch.io, Netlify) sin configurar cabeceras |
| Data caching | La segunda vez carga desde el celular |
| `senda_core.dll` fuera de la build web | Es una librería nativa; en el navegador el P2P usa **WebRTC** |

## 3. Publicar (necesita HTTPS: la cámara y el giroscopio no funcionan sin él)

- **itch.io** (lo más fácil): crea un proyecto *HTML*, sube `WebBuild` en un .zip y marca
  "This file will be played in the browser".
- **GitHub Pages**: sube el contenido de `WebBuild` a un repositorio (o a la rama `gh-pages`)
  y activa Pages. Ningún archivo puede pasar de 100 MB (con los modelos optimizados no pasa).
- **Netlify**: arrastra la carpeta `WebBuild` a app.netlify.com/drop.

## 4. Jugar offline

1. Abre el link una vez con internet.
2. En Chrome (Android): menú ⋮ → **Instalar app** / *Agregar a pantalla principal*.
   En iPhone (Safari): Compartir → **Agregar a inicio**.
3. Desde ahí se abre como una app, **con o sin internet**.

## 5. Conectar dos celulares (sin internet)

Ambos en la misma Wi-Fi, o uno comparte datos (punto de acceso) y el otro se conecta a él.

1. **Celular A** (anfitrión) abre el lobby: su QR ya es un código para unirse.
2. **Celular B** toca **ESCANEAR** y apunta al QR de A.
   → El QR de B cambia a amarillo: *"Ahora tu amigo escanea ESTE QR"*.
3. **Celular A** toca **ESCANEAR** y apunta al QR de B.
4. ¡Conectados! Para un tercer jugador se repite con A (su QR ya se renovó solo).

Todos los mensajes pasan por el anfitrión, así que cada jugador ve a todos los demás.

## 6. Pantalla horizontal y controles

- El juego es **siempre horizontal**. En Android, al primer toque la página pasa a pantalla completa y
  se bloquea en horizontal; instalada como app abre directo en horizontal. En iPhone (Safari no deja
  bloquear) aparece "Gira tu celular" mientras esté vertical.
- **Dos joysticks**, como en los shooters del celular: el **izquierdo mueve** (arriba acelera, abajo
  frena, a los lados te deslizas) y el **derecho mira 360°**; la nave va hacia donde miras. Inclinar el
  celular también mira (se puede apagar con GYRO). Botones redondos: **DISPARAR** (el anillo muestra la
  recarga), **BOOST** (mantener) y **ESCANEAR**.
- Con control Bluetooth: stick izquierdo mueve, derecho mira, gatillos aceleran / boost.

## 7. Qué cambia respecto a la app (y qué no)

| | App Android | Web |
|---|---|---|
| Juego, física, diseño, informe, campaña, estilos | ✅ | ✅ igual |
| P2P | Iroh (1 escaneo) | WebRTC (2 escaneos, uno por lado) |
| Giroscopio | Nativo | Sensor del navegador (en iPhone pide permiso al primer toque) |
| Cámara de fondo y escáner QR | ✅ | ✅ la abre el navegador (pide permiso); el QR lo lee el navegador (BarcodeDetector en Android, jsQR en iPhone/PC) |
| GPS y brújula ("¿Dónde está?") | ✅ | ✅ el navegador pregunta "¿permitir ubicación?"; brújula del sensor de orientación |
| Vibración | ✅ | ✅ en Android · ❌ iPhone (Safari no la permite) |
| "¿Dónde está esta noche?" (NASA Horizons) | ✅ | ✅ con internet · sin conexión no carga datos. Si el navegador bloquea la llamada directa a la NASA, la pide a tu servidor en `/nasa-horizons/` (ya está en `space-nginx.conf`) |

## 8. Si algo falla

- **"The file 'level0' is corrupted"** o comportamiento raro tras recompilar: el navegador mezcló
  la build vieja guardada offline con la nueva. Desde esta versión cada build usa nombres únicos
  y una versión de caché nueva, así que ya no pasa; para limpiar lo que quedó de antes:
  Chrome → F12 → **Application → Storage → Clear site data** y recarga.

- **La cámara no abre**: la página debe estar en **https://** y hay que aceptar el permiso. Si lo negaste:
  candado de la barra de direcciones → Permisos → Cámara → Permitir, y recarga.
- **"Ubicación bloqueada"** en *¿Dónde está?*: igual, candado → Ubicación → Permitir. Además el GPS
  del celular debe estar encendido.
- **iPhone, brújula**: Safari pide permiso de "movimiento y orientación" al primer toque; acéptalo.
- Nota técnica: Unity `WebCamTexture` y `Input.location` no sirven en el navegador (el primero llenaba
  la consola de `glCopySubTextureCHROMIUM: Offset overflows texture dimensions` y no se podía volver a
  abrir; el segundo nunca pide permiso). La versión web usa `NavCam.jspre` / `BrowserCamera.cs` /
  `DeviceLocation.cs`. Probado en Chromium con cámara simulada: QR de 139 caracteres leído en 0,3 s,
  cámara que se cierra y se vuelve a abrir, GPS y brújula.
- Los avisos de *shaders de post-procesado eliminados* y `JS_FileSystem_Sync` son normales y no afectan.
- **No conectan**: los dos en la misma red; algunas redes de escuela bloquean conexiones entre
  dispositivos → usar el punto de acceso de un celular.
- **iPhone se cierra al cargar**: Safari limita la memoria; usa el perfil *Web - Mobile* y los
  modelos optimizados.
- Probado aquí en navegadores reales (Chromium): conexión por códigos compactos (~140 caracteres,
  QR pequeño), 3 jugadores, mensajes a todos y reenviados por el anfitrión, con IPs y con nombres
  mDNS (como los celulares), y sin servidores STUN (el caso sin internet).
