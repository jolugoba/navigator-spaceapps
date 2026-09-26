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

## 6. Qué cambia respecto a la app (y qué no)

| | App Android | Web |
|---|---|---|
| Juego, física, diseño, informe, campaña, estilos | ✅ | ✅ igual |
| P2P | Iroh (1 escaneo) | WebRTC (2 escaneos, uno por lado) |
| Giroscopio | Nativo | Sensor del navegador (en iPhone pide permiso al primer toque) |
| Cámara de fondo | ✅ | ✅ (el navegador pide permiso) |
| Vibración | ✅ | ✅ en Android · ❌ iPhone (Safari no la permite) |
| "¿Dónde está esta noche?" (NASA Horizons) | ✅ | ✅ con internet · sin conexión no carga datos |

## 7. Si algo falla

- **La cámara no abre**: la página debe estar en **https://** y hay que aceptar el permiso.
- **No conectan**: los dos en la misma red; algunas redes de escuela bloquean conexiones entre
  dispositivos → usar el punto de acceso de un celular.
- **iPhone se cierra al cargar**: Safari limita la memoria; usa el perfil *Web - Mobile* y los
  modelos optimizados.
- Probado aquí en navegadores reales (Chromium): conexión por códigos, 3 jugadores, mensajes a
  todos y reenviados por el anfitrión, y modo sin servidores STUN (el caso sin internet).
