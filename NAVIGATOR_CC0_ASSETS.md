# NAVIGATOR — Recursos CC0 (dominio público)

**CC0** quiere decir que el autor renunció a todos sus derechos: se puede usar, cambiar y publicar sin pedir permiso y sin tener que darle crédito. **Igual lo damos**, por respeto al autor y porque Space Apps pide declarar el contenido de terceros. Los créditos oficiales están en [`CREDITS.md`](CREDITS.md).

Aquí solo hay recursos **CC0**. Nada con licencias CC BY, CC BY-SA, "Standard", "Editorial" ni sin licencia.

---

## 1. Ya descargados y agregados al proyecto

Fuente: **Poly Haven** (polyhaven.com). Todo lo de Poly Haven es CC0: https://polyhaven.com/license

### Texturas de suelo → `Assets/Art/Ground/`

Cada mundo con superficie tiene dos archivos:
- `ground_<mundo>.jpg`: la foto pasada a gris neutro. Da el grano de cerca y el código le pone encima el color del mundo.
- `ground_<mundo>_normal.jpg`: el relieve (normal map). Se ve con las sombras de las piedritas y grietas.

Se repiten al tamaño real de la foto (2 a 4 metros). **Navigator → Setup Scene** los conecta solos.

| Mundo | Archivo | Textura original | Autores |
|---|---|---|---|
| Mercurio | `ground_mercury.jpg` | [Moon 01](https://polyhaven.com/a/moon_01): regolito lunar | Greg Zaal, Rico Cilliers, Jenelle van Heerden, Dario Barresi |
| Venus | `ground_venus.jpg` | [Dark Rock](https://polyhaven.com/a/dark_rock): roca oscura agrietada | Amal Kumar |
| Marte | `ground_mars.jpg` | [Gravelly Sand](https://polyhaven.com/a/gravelly_sand): arena con grava | Dario Barresi |
| Titán | `ground_titan.jpg` | [Coast Sand 04](https://polyhaven.com/a/coast_sand_04): arena con guijarros | Rob Tuytel, Dario Barresi |
| Tritón | `ground_triton.jpg` | [Snow 03](https://polyhaven.com/a/snow_03): escarcha granulada | Rob Tuytel |

Júpiter y Urano no tienen suelo, así que no llevan textura.

### Rocas 3D → `Assets/Models/Props/`

| Archivo | Modelo original | Autores | Dónde sale |
|---|---|---|---|
| `PR-02_MercuryBoulder_FINAL.glb` | [Moon Rock 03](https://polyhaven.com/a/moon_rock_03) | Greg Zaal, Rico Cilliers, Jenelle van Heerden | Rocas de Mercurio |
| `PR-04_MarsLayeredRock_FINAL.glb` | [Rock 07](https://polyhaven.com/a/rock_07) | Jenelle van Heerden | Sitios del delta y la orilla (Marte) |
| `PR-06_MarsBoulder_FINAL.glb` | [Rock 09](https://polyhaven.com/a/rock_09) | Jenelle van Heerden | Rocas de Marte y sitio del fondo del cráter |
| `PR-10_TritonFrostRock_FINAL.glb` | [Moon Rock 01](https://polyhaven.com/a/moon_rock_01) | Greg Zaal, Rico Cilliers, Jenelle van Heerden, Dario Barresi | Rocas de Tritón |

Son livianos (5.600 a 18.700 triángulos, texturas 1K, de 1,5 a 2,2 MB cada uno).

---

## 2. Para que descargues tú (Sketchfab, CC0)

Sketchfab pide una **cuenta gratis** para descargar. En la página del modelo: **Download 3D Model** → **glTF** → formato **GLB**. Luego renómbralo como dice la tabla y ponlo en la carpeta indicada.

Todos estos están marcados **CC0 Public Domain** en Sketchfab. Revisa que la página lo siga diciendo cuando lo bajes.

| Guardar como | Modelo | Autor | Enlace | Para qué |
|---|---|---|---|---|
| `Models/Vehicles/VH-03_EntryCapsule_FINAL.glb` | Apollo 11 Command Module Exterior | The Smithsonian Institution | https://sketchfab.com/3d-models/bf46ddbfb76b42d2af57d20625b1b603 | La cápsula de entrada (Marte, Venus, Titán). Es un escaneo de la cápsula real |
| `Models/Props/PR-06_MarsBoulder_FINAL.glb` *(opcional, reemplaza la de Poly Haven)* | Antarctic Meteorite Sample ALH 84001,55 | NASA Astromaterials 3D | https://sketchfab.com/3d-models/fed05a58c22949568ba3648f64052f03 | **Una roca de Marte de verdad**: el meteorito marciano ALH 84001 |
| `Models/Props/PR-02_MercuryBoulder_FINAL.glb` *(opcional, reemplaza la de Poly Haven)* | Apollo Lunar Sample 14316,0 | NASA Astromaterials 3D | https://sketchfab.com/3d-models/4efb5f99eaf94398a6ef9fa65b91c59d | Roca traída de la Luna por el Apolo 14, parecida a las de Mercurio |

**Importante después de bajarlos:**
1. Pesan mucho (90.000 a 240.000 caras). Ejecuta `cd tools && npm install && node optimize_models.mjs` para aligerarlos.
2. Si reemplazas una roca, **borra la de Poly Haven** con el mismo código y cambia su fila en `CREDITS.md`.
3. Agrega una fila en `CREDITS.md`, en la tabla **"Third-party models"**, con el archivo, el nombre del modelo, el autor, el enlace y "CC0".
4. En Unity: **Navigator → Assign Models** y luego **Navigator → Setup Scene**.

---

## 3. Lo que NO existe en CC0 (generarlo con Meshy)

Busqué en Poly Haven y en Sketchfab filtrando solo CC0 y no hay versiones realistas de estos. Usa los prompts de [`NAVIGATOR_MESHY_MUNDOS.md`](NAVIGATOR_MESHY_MUNDOS.md):

- **Vehículos:** VH-01 módulo de aterrizaje, VH-02 rover térmico, VH-04 sonda de Venus, VH-05 grúa aérea, VH-06 sonda atmosférica, VH-07 Dragonfly, VH-08 orbitador de Urano, VH-09 paracaídas, VH-10 saltador.
- **Objetos:** PR-01 trozo de hielo, PR-03 losa de Venus, PR-05 tubo de muestra, PR-07 guijarros de Titán, PR-08 escudo de calor, PR-09 géiser.

Si falta alguno, el juego usa una forma simple con el color correcto: nada se rompe.

---

## Dónde más buscar CC0

| Sitio | Qué tiene | Licencia |
|---|---|---|
| polyhaven.com | Texturas, modelos y cielos HDRI realistas | Todo CC0 |
| ambientcg.com | Texturas (suelo, roca, nieve, metal) | Todo CC0 |
| sketchfab.com | De todo, pero con licencias mezcladas: filtra por **CC0 Public Domain** en *Licenses* | Solo lo que diga CC0 |
