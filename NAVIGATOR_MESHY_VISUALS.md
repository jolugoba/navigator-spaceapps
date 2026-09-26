# NAVIGATOR 2.0 — Pedido de arte a Meshy (cielo, gravedad, misiones reales)

**Para quién:** la persona que genera en Meshy.
**Cuánto:** 30 imágenes como máximo, más 6 modelos 3D opcionales. Están en orden de prioridad: si solo alcanza para 10, haz las 10 primeras.
**Qué pasa si falta arte:** el juego ya funciona sin nada de esto. Las nebulosas, la galaxia y el hoyo negro se generan por código. Cuando pones el arte de Meshy en la carpeta indicada y corres **Navigator → Setup Scene**, el juego lo usa automáticamente.

---

## Reglas para TODAS las imágenes del cielo (la parte más importante)

El cielo se dibuja **sumado sobre la cámara del celular** (mezcla aditiva): lo negro no se ve y lo brillante brilla sobre el cuarto. Por eso cada imagen debe cumplir esto:

| Regla | Por qué |
|---|---|
| **Fondo 100 % negro puro (#000000)** | Lo negro se vuelve transparente sobre la cámara. |
| **El objeto centrado, y que se desvanezca hacia los bordes** | Si toca el borde, se ve un cuadrado recortado en el cielo. |
| **Sin estrellas brillantes en las esquinas, sin texto, sin marco, sin marca de agua** | Las estrellas ya las pone el juego. |
| **Cuadrada, 1024×1024 o 2048×2048, PNG** | Un PNG con canal alfa es aún mejor. |
| **Nada de planetas ni naves dentro de la imagen** | Esos objetos son 3D. |

**Dónde guardarlas:** `unity_project/NavigatorUnity/Assets/Art/Sky/`. Crea la carpeta si no existe.
**Nombres:** empiezan por `nebula_`, `galaxy_` o `blackhole_`. El código busca por ese prefijo.

### Prioridad 1 — Nebulosas (6 imágenes) → `nebula_01.png` … `nebula_06.png`

Prompt base, igual para todas (pega esto y añade la variante):

> `astrophotography of a single isolated nebula, centered, soft wispy gas clouds fading smoothly into a pure black background at all edges, no stars in the corners, no text, no frame, high dynamic range, Hubble / James Webb style, 1:1`

| Archivo | Variante que añades al prompt |
|---|---|
| `nebula_01.png` | `vivid magenta and violet hydrogen emission nebula, glowing core` |
| `nebula_02.png` | `teal and cyan oxygen nebula with delicate filaments` |
| `nebula_03.png` | `Orion-nebula-like, warm pink and orange with a bright white heart` |
| `nebula_04.png` | `Carina-like towering dust pillars backlit in gold and blue (Pillars of Creation style)` |
| `nebula_05.png` | `planetary nebula ring like the Helix Nebula, blue center, red outer ring` |
| `nebula_06.png` | `faint blue reflection nebula with dark dust lanes` |

### Prioridad 2 — Hoyo negro (2 imágenes) → `blackhole_01.png`, `blackhole_02.png`

| Archivo | Prompt |
|---|---|
| `blackhole_01.png` | `supermassive black hole seen slightly edge-on, thin bright accretion disk, gravitational lensing arching the far side of the disk over the black shadow, one side brighter from Doppler beaming (Interstellar Gargantua style), scientifically accurate, centered, pure black background, fades to black at edges, 1:1` |
| `blackhole_02.png` | `Event Horizon Telescope style image of the M87* black hole, glowing orange ring around a dark shadow, soft and blurry, centered on pure black, 1:1` |

> Solo se usa `blackhole_01`. La 02 queda de repuesto (es la foto real del EHT, útil para la presentación).
> Si Meshy entrega JPG (sin transparencia), el juego lo suma como brillo. Si entrega **PNG con transparencia y el centro negro opaco**, se ve la sombra real del hoyo negro tapando las estrellas, que es lo ideal.

### Prioridad 3 — Galaxias (3 imágenes) → `galaxy_01.png` … `galaxy_03.png`

| Archivo | Prompt |
|---|---|
| `galaxy_01.png` | `spiral galaxy tilted 60 degrees like Andromeda, bright yellow core, blue star-forming arms, dust lanes, centered, pure black background, fades to black at edges, 1:1` |
| `galaxy_02.png` | `face-on barred spiral galaxy, pinkish H-II regions along the arms, centered on pure black, 1:1` |
| `galaxy_03.png` | `distant elliptical galaxy, soft golden glow, small, centered on pure black, 1:1` |

---

## Modelos 3D opcionales (asistencias gravitatorias y "tu nave vs. la real")

Formato: **.glb** exportado desde Meshy. Si Meshy da dos versiones, usa la texturizada PBR y ponle `_FINAL` al final del nombre.

| Prio | Archivo | Carpeta | Prompt | Dónde se usa |
|---|---|---|---|---|
| 4 | `PL-08_Earth_FINAL.glb` | `Assets/Models/Planets/` | `planet Earth, realistic, blue oceans, white clouds, continents, sphere, PBR` | **Ya se usa:** asistencia gravitatoria con la Tierra (rutas de Galileo y Cassini). Hoy se ve una esfera azul de reemplazo. |
| 5 | `PL-09_Moon_FINAL.glb` | `Assets/Models/Planets/` | `the Moon, realistic grey craters, sphere, PBR` | Futuro: primera misión de práctica. |
| 6 | `OB-06_Voyager_FINAL.glb` | `Assets/Models/Objects/` | `NASA Voyager space probe, large white high-gain dish antenna, RTG boom, magnetometer boom, accurate, PBR` | Futuro: tarjeta "tu nave vs. la real" (Urano y Neptuno). |
| 7 | `OB-07_Cassini_FINAL.glb` | `Assets/Models/Objects/` | `NASA Cassini-Huygens spacecraft, gold foil body, big white dish antenna, Huygens probe disk on the side, PBR` | Futuro: igual, para Saturno. |
| 8 | `OB-08_Juno_FINAL.glb` | `Assets/Models/Objects/` | `NASA Juno spacecraft, three long solar panel wings, hexagonal body, PBR` | Futuro: igual, para Júpiter. |
| 9 | `OB-09_Comet_FINAL.glb` | `Assets/Models/Objects/` | `icy comet nucleus, dark irregular rock with bright ice patches, PBR` | Futuro: peligro que cruza la ruta (con cola de partículas hecha por código). |

---

## Resto del cupo de imágenes (hasta 30) — pantallas e insignias

Guárdalas en `Assets/Art/UI/`. **Todavía no están conectadas al código.** Cuando las tengas, pídele a Claude que las conecte.

| # | Archivo | Prompt |
|---|---|---|
| 12–17 | `badge_firstlight.png`, `badge_venus.png`, `badge_rover.png`, `badge_jupiter.png`, `badge_saturn.png`, `badge_neptune.png` | `round embroidered NASA-style mission patch for a {Mars orbiter / Venus probe / Mars rover / Jupiter orbiter / Saturn probe / Neptune flyby} mission, bold simple shapes, no text, transparent background` |
| 18 | `lobby_bg.png` (9:16) | `deep space vista, colorful nebula at top, dark bottom for UI, portrait 9:16, no planets, no text` |
| 19 | `debrief_bg.png` (9:16) | `mission control room at night, blue screens, blurred, dark, portrait 9:16, no text` |
| 20 | `victory_bg.png` (9:16) | `spacecraft orbiting a planet at sunrise, epic, dark lower half, portrait 9:16, no text` |
| 21–24 | `tut_1.png` … `tut_4.png` | Ilustraciones del tutorial: `flat vector illustration of {holding a phone up to aim a spaceship / pressing a big THRUST button / a spaceship swinging around a planet (gravity assist) / scientific instruments scanning a planet}, dark blue background` |
| 25–30 | reserva | Guárdalas para lo que aparezca después de probar (por ejemplo, más nebulosas `nebula_07+`: el juego usa todas las que encuentre). |

---

## Checklist rápido

1. Genera primero, en este orden: `nebula_01`–`06`, `blackhole_01`, `galaxy_01`–`03`, y luego el modelo de la Tierra.
2. Copia los archivos a las carpetas indicadas, con los **nombres exactos**.
3. En Unity, corre **Navigator → Setup Scene**. Las texturas del cielo se configuran solas (bordes *Clamp* y alfa como transparencia).
4. Dale Play: las nebulosas, la galaxia y el hoyo negro de Meshy reemplazan a los generados por código.
