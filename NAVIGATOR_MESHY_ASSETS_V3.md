# NAVIGATOR 2.0 — Pedido Meshy v3 (20 modelos 3D)

**Qué es:** la tercera tanda de modelos para Meshy (text-to-3D), pensada para lo nuevo del juego: pods que se entienden de un vistazo, cosas reales para **ESCANEAR** en el camino (sondas, telescopios, lunas) y una reserva para lo que viene.

**Qué pasa si falta alguno:** el juego no se rompe.
- Los pods se ven como figuras simples de color (cápsula, disco, rombo…).
- Las lunas se ven como esferas de color.
- Las sondas y los telescopios simplemente no aparecen.

Cuando un modelo llega a su carpeta con el nombre exacto, **Navigator → Setup Scene** lo conecta solo.

## Reglas para todos

| Regla | Por qué |
|---|---|
| Formato **.glb**; si Meshy da dos versiones, usa la texturizada (PBR) y termina el nombre en `_FINAL` | El código prefiere `_FINAL` |
| **Nombre exacto** de la tabla (el prefijo, p. ej. `PD-01`, es lo que busca el código) | Si el prefijo cambia, no se conecta |
| **Pocos polígonos: ~20.000 caras** (en la API, `target_polycount: 20000` si tu versión lo acepta; si no, reduce en la web de Meshy) | Los modelos anteriores pesan 25-55 MB y en un celular eso se nota. Un pod o una luna no necesitan más |
| Un solo objeto, centrado, **sin base ni suelo ni fondo** | Se coloca flotando en el espacio |
| `art_style: realistic` (pods: `realistic` o `sculpture`), `negative_prompt: "cartoon, base, ground, text, logo"` | Coherente con el resto |

Carpetas (dentro de `unity_project/NavigatorUnity/Assets/`):
- `Models/Pods/` para los pods;
- `Models/Objects/` para sondas y telescopios;
- `Models/Moons/` para las lunas (créalas si no existen).

---

## A. Pods (5) — prioridad 1 · **ya conectados**

Es lo primero que ves al volar. Cada tipo debe distinguirse **por la forma**, no solo por el color. El juego igual les pone su halo de color y el nombre encima.

| # | Archivo | Prompt |
|---|---|---|
| 1 | `Models/Pods/PD-01_Fuel_FINAL.glb` | `sci-fi fuel canister pod floating in space, cylindrical tank with glowing green bands, handles and valves, hard-surface, clean PBR` |
| 2 | `Models/Pods/PD-02_Boost_FINAL.glb` | `sci-fi booster pod, compact rocket thruster module with orange glowing nozzle and fins, hard-surface, PBR` |
| 3 | `Models/Pods/PD-03_Shield_FINAL.glb` | `sci-fi shield generator orb, metal core with blue glowing energy rings around it, hard-surface, PBR` |
| 4 | `Models/Pods/PD-04_EMP_FINAL.glb` | `sci-fi EMP device, octahedron-shaped metal core with purple glowing coils and spikes, hard-surface, PBR` |
| 5 | `Models/Pods/PD-05_Gravity_FINAL.glb` | `sci-fi gravity well device, small dark sphere inside a golden gyroscope ring, yellow glowing accents, hard-surface, PBR` |

## B. Sondas reales junto a su planeta (3) — prioridad 2 · **ya conectadas**

Al llegar al planeta, la sonda real que lo exploró flota a su lado. El jugador la **escanea** y aprende qué hizo. Voyager 2, Cassini y Juno ya los tienes (OB-06, OB-07, OB-08): no hay que repetirlos.

| # | Archivo | Prompt | Aparece en |
|---|---|---|---|
| 6 | `Models/Objects/OB-11_Messenger_FINAL.glb` | `NASA MESSENGER spacecraft, box body behind a large white ceramic-cloth sunshade, two solar panel wings, accurate, PBR` | Mercurio |
| 7 | `Models/Objects/OB-12_Magellan_FINAL.glb` | `NASA Magellan spacecraft, large white radar dish on top, square solar panels, long boom, accurate, PBR` | Venus |
| 8 | `Models/Objects/OB-13_MRO_FINAL.glb` | `NASA Mars Reconnaissance Orbiter, large white high-gain dish, two big solar panel wings, gold foil body, accurate, PBR` | Marte |

## C. Telescopios y estación junto al despegue (3) — prioridad 2 · **ya conectados**

Aparecen cerca del punto de lanzamiento: lo primero que el jugador puede escanear.

| # | Archivo | Prompt |
|---|---|---|
| 9 | `Models/Objects/OB-14_Hubble_FINAL.glb` | `Hubble Space Telescope, silver cylindrical body with open aperture door, two long solar panel wings, accurate, PBR` |
| 10 | `Models/Objects/OB-15_JWST_FINAL.glb` | `James Webb Space Telescope, hexagonal gold segmented mirror above a large five-layer diamond-shaped silver sunshield, accurate, PBR` |
| 11 | `Models/Objects/OB-16_ISS_FINAL.glb` | `International Space Station, central truss with large solar array wings, pressurized modules, radiators, accurate, PBR` |

## D. Lunas (6) — prioridad 3 · **ya conectadas** (hoy son esferas de color)

Orbitan junto al planeta de destino y se escanean. Europa ya la tienes (PL-11).

| # | Archivo | Prompt | Planeta |
|---|---|---|---|
| 12 | `Models/Moons/MN-01_Phobos_FINAL.glb` | `Mars moon Phobos, small potato-shaped grey rocky moon with the large Stickney crater and grooves, realistic, PBR` | Marte |
| 13 | `Models/Moons/MN-02_Io_FINAL.glb` | `Jupiter moon Io, yellow sulfur surface with orange, red and black volcanic spots, sphere, realistic, PBR` | Júpiter |
| 14 | `Models/Moons/MN-03_Titan_FINAL.glb` | `Saturn moon Titan, hazy thick orange atmosphere sphere, realistic, PBR` | Saturno |
| 15 | `Models/Moons/MN-04_Enceladus_FINAL.glb` | `Saturn moon Enceladus, bright white icy sphere with blue tiger-stripe fractures at the south pole, realistic, PBR` | Saturno |
| 16 | `Models/Moons/MN-05_Miranda_FINAL.glb` | `Uranus moon Miranda, grey icy moon with patchwork terrain and huge cliffs, realistic, PBR` | Urano |
| 17 | `Models/Moons/MN-06_Triton_FINAL.glb` | `Neptune moon Triton, pinkish icy sphere with cantaloupe-textured terrain and dark geyser streaks, realistic, PBR` | Neptuno |

## E. Reserva (3) — prioridad 4 · **conectados como puntos escaneables**

Hoy aparecen para escanear: DART entre los asteroides, SLS junto al despegue y Perseverance junto a Marte. Más adelante pueden tener su propia mecánica.

| # | Archivo | Prompt | Idea |
|---|---|---|---|
| 18 | `Models/Objects/OB-17_DART_FINAL.glb` | `NASA DART spacecraft, box body with two long roll-out solar arrays and a high-gain antenna, accurate, PBR` | Mini-misión "desvía el asteroide" con el láser (defensa planetaria real, 2022) |
| 19 | `Models/Objects/LV-01_SLS_FINAL.glb` | `NASA SLS rocket on its own, orange core stage with two white solid rocket boosters, Orion capsule on top, accurate, PBR` | Despegue en la cuenta regresiva |
| 20 | `Models/Objects/OB-18_Rover_FINAL.glb` | `NASA Perseverance Mars rover, six wheels, mast camera, robotic arm, accurate, PBR` | Aparece en la superficie al completar la misión de aterrizaje |

---

## Orden recomendado

1. **Pods 1-5.** Resuelven lo de "no sé qué son esas bolas verdes y azules".
2. **Sondas y telescopios 6-11.** Hacen que el escáner muestre cosas reales de la NASA.
3. **Lunas 12-17.**
4. **Reserva 18-20.**

Después de copiar los archivos: en Unity, corre **Navigator → Setup Scene**. Revisa la Console: dice cuántos modelos asignó.
