# NAVIGATOR — Pedido Meshy para los 7 mundos (20 modelos)

**Para qué:** los vehículos y objetos de los mundos (aterrizar y explorar cada planeta). El diseño de cada mundo está en [`docs/mundos/`](docs/mundos/00_GUIA_MUNDOS.md).

**Son 20 modelos nuevos**, de un máximo de 30. Todo lo demás **se reutiliza** de lo que ya tenemos (lista al final).

**Si falta alguno, el juego no se rompe:** el código usa una forma simple (caja, cilindro o esfera) con el color correcto hasta que llegue el modelo.

## Reglas (las mismas del pedido v3)

| Regla | Por qué |
|---|---|
| Formato **.glb** texturizado (PBR); el nombre termina en `_FINAL` | El código busca `_FINAL` |
| **Nombre exacto** de la tabla | El prefijo (`VH-03`…) es lo que busca el código |
| **~20.000 caras** (`target_polycount: 20000`) | Tiene que ir fluido en el celular y en la web |
| Un solo objeto, **centrado, sin base ni suelo ni fondo** | Se coloca solo en el mundo |
| **Vehículos: de frente y con las ruedas o patas hacia abajo** | Para que avancen hacia donde miran |
| `art_style: realistic`, `negative_prompt: "cartoon, base, ground, platform, text, logo, people"` | Coherente con el resto |

Carpetas (dentro de `unity_project/NavigatorUnity/Assets/`), créalas si no existen:
- `Models/Vehicles/` para los vehículos (VH-);
- `Models/Props/` para los objetos del suelo (PR-).

---

## A. Vehículos (10) — prioridad por mundo

| # | Archivo | Prompt | Mundos |
|---|---|---|---|
| 1 | `Models/Vehicles/VH-03_EntryCapsule_FINAL.glb` | `planetary entry capsule, wide blunt heat shield cone underneath with scorched brown ablative surface, white conical backshell on top, NASA style aeroshell, realistic, PBR` | Marte, Venus, Titán |
| 2 | `Models/Vehicles/VH-09_Parachute_FINAL.glb` | `single large spacecraft parachute canopy fully inflated, orange and white disk-gap-band pattern, suspension lines gathered to one point below, realistic, PBR` | Todos los que tienen aire |
| 3 | `Models/Vehicles/VH-05_SkyCrane_FINAL.glb` | `NASA Mars sky crane descent stage, flat octagonal frame with eight downward rocket engines at the corners, fuel spheres, radar, gold foil, realistic, PBR` | Marte |
| 4 | `Models/Vehicles/VH-01_Lander_FINAL.glb` | `robotic planetary lander, compact octagonal body with gold foil, four long folding landing legs with round footpads, main rocket engine underneath, small thrusters, antenna dish, realistic, PBR` | Mercurio, Tritón |
| 5 | `Models/Vehicles/VH-02_ThermalRover_FINAL.glb` | `six-wheeled planetary rover with a large white ceramic sunshade parasol above the body, bright headlights at the front, robotic drill arm, mast camera, realistic, PBR` | Mercurio |
| 6 | `Models/Vehicles/VH-04_VenusLander_FINAL.glb` | `Soviet Venera style Venus lander, spherical armored pressure vessel on a round crushable landing ring, flat disk airbrake on top, short drill arm, titanium and white paint, realistic, PBR` | Venus |
| 7 | `Models/Vehicles/VH-06_AtmoProbe_FINAL.glb` | `atmospheric entry probe like the Galileo probe, compact cone with thick brown heat shield, instrument ports on the side, small parachute canister on top, realistic, PBR` | Júpiter, Urano |
| 8 | `Models/Vehicles/VH-07_Dragonfly_FINAL.glb` | `NASA Dragonfly Titan rotorcraft lander, car-sized body with eight rotors in four pairs, two landing skids, radioisotope generator at the back, instruments at the front, realistic, PBR` | Titán |
| 9 | `Models/Vehicles/VH-08_UranusOrbiter_FINAL.glb` | `outer planet orbiter spacecraft, large white high-gain dish antenna, long magnetometer boom, radioisotope generator, a conical atmospheric probe attached on its side, realistic, PBR` | Urano |
| 10 | `Models/Vehicles/VH-10_Hopper_FINAL.glb` | `small planetary hopper robot for icy moons, compact rounded body with three springy legs, cold gas thrusters on all sides, mast camera, sampling scoop, white and gold, realistic, PBR` | Tritón |

## B. Objetos del suelo (10)

| # | Archivo | Prompt | Mundo |
|---|---|---|---|
| 11 | `Models/Props/PR-04_MarsLayeredRock_FINAL.glb` | `Mars sedimentary rock outcrop with visible horizontal layers, reddish brown and tan, dusty, realistic, PBR` | Marte (sitio del delta) |
| 12 | `Models/Props/PR-06_MarsBoulder_FINAL.glb` | `dark grey volcanic basalt boulder with red dust in the cracks, Mars, realistic, PBR` | Marte (sitios y obstáculos) |
| 13 | `Models/Props/PR-05_SampleTube_FINAL.glb` | `NASA Perseverance sample tube, slim titanium cylinder with white cap and serial markings, realistic, PBR` | Marte (depósito) |
| 14 | `Models/Props/PR-08_HeatShield_FINAL.glb` | `discarded spacecraft heat shield, wide shallow cone, scorched and cracked brown ablative tiles, realistic, PBR` | Marte, Venus y Titán (cae al soltarlo) |
| 15 | `Models/Props/PR-01_IceChunk_FINAL.glb` | `chunk of dirty water ice mixed with dark regolith, translucent bluish white, frosty, realistic, PBR` | Mercurio (y Tritón) |
| 16 | `Models/Props/PR-02_MercuryBoulder_FINAL.glb` | `grey cratered rocky boulder, dusty dark grey regolith, sharp edges, airless surface, realistic, PBR` | Mercurio |
| 17 | `Models/Props/PR-03_VenusSlab_FINAL.glb` | `flat broken slabs of dark basalt lava rock, orange tinted, cracked plates, Venus surface, realistic, PBR` | Venus (placas a taladrar) |
| 18 | `Models/Props/PR-07_TitanPebbles_FINAL.glb` | `cluster of rounded water ice pebbles on dark brown organic sand, Titan surface, realistic, PBR` | Titán |
| 19 | `Models/Props/PR-09_GeyserVent_FINAL.glb` | `dark vent crater on pinkish nitrogen ice, black fan-shaped dust streak around the opening, icy moon Triton, realistic, PBR` | Tritón |
| 20 | `Models/Props/PR-10_TritonFrostRock_FINAL.glb` | `icy rock covered in pinkish white nitrogen frost with small dimples, bluish grey underneath, realistic, PBR` | Tritón |

---

## Lo que YA tenemos y se reutiliza (no pedir)

| Modelo existente | Uso en los mundos |
|---|---|
| `OB-18_Rover` (Perseverance) | El rover de Marte |
| `MN-01_Phobos` | Cruza el cielo de Marte |
| `MN-02_Io` | Cielo de Júpiter (inicio del descenso) |
| `MN-03_Titan` | Se ve desde arriba al bajar a Titán |
| `MN-05_Miranda` | Junto a Urano en la fase de órbita |
| `MN-06_Triton` | Vista desde arriba antes de bajar |
| `PL-04_Jupiter`, `PL-05_Saturn`, `PL-06_Uranus`, `PL-07_Neptune` | El planeta gigante en el cielo de cada mundo |
| `OB-08_Juno` | El orbitador que recibe la señal en Júpiter |
| `OB-13_MRO` | El relé de comunicaciones sobre Marte |
| `OB-07_Cassini` | Arriba durante el descenso a Titán |
| `OB-11_Messenger` | En el cielo de Mercurio (dato: encontró el hielo) |
| `OB-12_Magellan` | Relé sobre Venus |
| `OB-03_Beacon` | Marcador de los sitios de muestra |

## Terreno, cielo y texturas: sin Meshy

- El **terreno** (relieve, cráteres, dunas, placas), el **cielo** (degradado de color y niebla) y las **partículas** (polvo, lluvia de metano, géiseres, relámpagos) los genera el código con los colores de cada ficha. **No hay que descargar nada.**
- **Opcional, para que el suelo se vea más real:** una textura de suelo por mundo de **ambientCG.com** (gratis, licencia CC0, sin atribución obligatoria). Descarga la versión **1K-JPG** y ponla en `Assets/Art/Ground/` con este nombre:

| Archivo | Qué buscar en ambientCG |
|---|---|
| `ground_mercury.jpg` | "Ground" o "Rock", gris oscuro, sin plantas |
| `ground_venus.jpg` | "Rock" o "Lava", roca volcánica oscura y plana |
| `ground_mars.jpg` | "Ground" o "Sand", rojizo o café claro |
| `ground_titan.jpg` | "Sand" o "Ground", café oscuro |
| `ground_triton.jpg` | "Snow" o "Ice", blanco con algo de textura |

Si no están, el código genera la textura. **No son necesarias para que funcione.**

## Orden recomendado

1. **Marte**: 1, 2, 3, 11, 12, 13, 14. Con esto se construye el primer mundo completo.
2. **Mercurio y Tritón**: 4, 5, 10, 15, 16, 19, 20.
3. **Venus, Júpiter y Urano**: 6, 7, 9, 17.
4. **Titán**: 8, 18.

Después de copiar los archivos: **Navigator → Setup Scene** los conecta solos (la Console dice cuántos encontró).
