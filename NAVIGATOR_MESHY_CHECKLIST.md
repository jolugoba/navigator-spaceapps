# NAVIGATOR 2.0 — CHECKLIST MAESTRO MESHY
**Guacalita Inc. — Lista única para crear todos los modelos 3D**
**Fecha: 2026-09-22 | Fuente: todos los 5 documentos consolidados**

---

## CÓMO USAR ESTE DOCUMENTO

1. Abre Meshy.ai
2. Sigue la lista en orden de prioridad
3. Para cada modelo: copia el PROMPT exacto en Meshy
4. Al terminar Meshy: descarga GLB/FBX
5. Columna "ANIMACIÓN" te dice si necesitas Mixamo después o si Unity lo hace

---

## CONTEO TOTAL

| Categoría | Cantidad | En Mixamo después | En Unity después |
|---|---|---|---|
| Personajes | 4 | 4 (todos) | — |
| Naves | 4 | — | 4 |
| Entornos/estructuras | 5 | — | 5 |
| Planetas | 7 | — | 2 con sub-mesh |
| Objetos espacio | 5 | — | 5 |
| Instrumentos científicos | 4 | — | 4 |
| Powerups (racing) | 5 | — | 5 |
| **TOTAL** | **34 modelos GLB** | **4** | **25** |

> Agujero Negro (PL-08) = shader Unity puro, no crear en Meshy
> Burbuja escudo (EF-01) = sphere Unity, no crear en Meshy

---

## PRIORIDAD 1 — SIN ESTO NO HAY DEMO
*Crear estos primero. Con estos 6 ya puedes hacer la primera prueba.*

---

### ✅ NV-01 — NAVE DEL JUGADOR (exterior)
**Categoría:** Nave principal  
**Prompt para Meshy:**
```
Sci-fi player spaceship, compact fighter design, two rear main engines with glowing nozzles,
short delta wings, cargo bay at the bottom, glass cockpit dome at the front,
white and gray hull with cyan accents. Hard surface. Futuristic but realistic NASA aesthetic.
4 SEPARATE MATERIALS: fuselage (white/gray), wings and details (secondary color),
engine nozzles (glow zone), cockpit glass (dark translucent).
Sub-mesh the engine nozzles as a separate part named "nozzles".
```
**Animaciones:** Unity hace todo — engine glow (shader), wing tilt (keyframe), landing gear (keyframe)  
**Notas críticas:** Los 4 materiales separados son OBLIGATORIOS para personalización de colores

---

### ✅ EN-01 — COCKPIT INTERIOR (vista primera persona)
**Categoría:** Entorno interior  
**Prompt para Meshy:**
```
Spaceship cockpit interior, first-person pilot view, retro-futuristic style.
One single model that includes: pilot seat with harnesses, main control dashboard,
large panoramic front window with frame, side system screens, central joystick,
side panels with switches and buttons.
Named sub-meshes required: "lever_main" (central joystick), "radar_screen" (circular display),
"btn_launch" (large red button), "btn_boost" (small lever), "fuel_needle" (gauge needle),
"comms_screen" (side rectangular display).
Realistic sci-fi. Colors: dark metal, cyan displays, orange warning lights.
```
**Animaciones:** Unity keyframe — palanca se inclina, radar gira 360°, botón baja 5mm, aguja combustible 0°-270°, pantalla parpadea  
**Notas críticas:** TODO en 1 solo modelo GLB. Los sub-meshes con nombre exacto = Unity los anima por nombre

---

### ✅ PL-03 — MARTE
**Categoría:** Planeta  
**Prompt para Meshy:**
```
Planet Mars, realistic, sphere, red-orange rocky surface with visible canyons and volcanoes,
white polar ice caps at both poles, thin reddish atmosphere glow at the edges.
Highly detailed surface texture. Sub-mesh the polar caps as "polar_caps" for separate material.
```
**Animaciones:** Ninguna en Meshy. Unity rota el planeta con transform.Rotate()  
**Notas críticas:** Casquetes polares como sub-mesh (Unity les aplica material blanco separado)

---

### ✅ PJ-01 — EL CADETE (jugador)
**Categoría:** Personaje principal  
**Prompt para Meshy:**
```
Space cadet astronaut, young, full spacesuit with helmet, reflective visor (face not visible),
NASA patches and "NAVIGATOR" patch on the suit, clean modern design, neutral gender appearance.
T-pose. Full body. Gloves included. Color: white suit with gray details and cyan highlights.
Realistic proportions, game-ready, humanoid rig compatible.
```
**Después en Mixamo:** idle_cockpit, idle_standing, walk, sit_down, reach_lever, press_button, look_radar, celebrate, death  
**Notas críticas:** Visor opaco (no modelar cara). T-pose obligatorio para Mixamo. Proporciones humanoides estándar

---

### ✅ PW-01 — POD COMBUSTIBLE (powerup racing)
**Categoría:** Powerup  
**Prompt para Meshy:**
```
Floating space fuel pod, sphere shape, bright green with a fuel symbol carved on the surface,
subtle pulsing glow effect suggestion in the geometry. Sci-fi aesthetic.
Two sub-meshes: "pod_body" (main sphere), "pod_glow" (outer layer for separate emission material).
Small, about 0.5m diameter in game scale.
```
**Animaciones:** Unity code — flotación suave arriba/abajo (sine wave), shader pulso de luz verde  
**Notas críticas:** Sub-mesh exterior separado para que Unity aplique emission shader independiente

---

### ✅ OB-01 — ASTEROIDE (3 tamaños)
**Categoría:** Obstáculo  
**Prompt para Meshy:**
```
Space asteroid, irregular rocky shape, rough surface with craters and cracks,
dark gray with slight brownish tones, realistic space rock texture.
Medium size, game-ready. Not a perfect sphere — highly irregular.
```
**Animaciones:** Unity code — rotación lenta aleatoria en 3 ejes  
**Notas críticas:** Un solo modelo. Unity lo escala x1 (pequeño), x3 (mediano), x6 (grande) para las 3 variaciones

---

## PRIORIDAD 2 — CARRERA COMPLETA
*Con estos ya tienes la carrera multiplayer funcionando.*

---

### NV-02 — NAVE RIVAL KRONOS (exterior)
**Categoría:** Nave antagonista  
**Prompt para Meshy:**
```
Sci-fi enemy spaceship, aggressive design, same proportions as a standard fighter but:
sharper edges, angular aggressive shapes, side weapon pods instead of cargo bay,
black hull with red accents, dark visor cockpit. Hard surface. Menacing aesthetic.
4 SEPARATE MATERIALS: fuselage (black), wings/details (red), engine nozzles (red glow), cockpit (dark).
Sub-mesh engine nozzles as "nozzles".
```
**Animaciones:** Unity — igual que NV-01 (engine glow, wing tilt)  
**Notas críticas:** Mismas proporciones exactas que NV-01 para que las colisiones funcionen igual

---

### EN-05 — ARCO DE SALIDA ESPACIAL (línea de salida)
**Categoría:** Estructura de carrera  
**Prompt para Meshy:**
```
Space race starting arch, two tall futuristic pillars connected by an arc at the top,
traffic light style indicator lights on the pillars (3 lights: red, yellow, green),
"NAVIGATOR" logo text on the arch, minimalist futuristic style, metallic silver/white.
Sub-mesh the lights as "start_lights" for color animation.
Appears floating in space for AR racing start.
```
**Animaciones:** Unity keyframe — luces rojas → amarillas → verdes (countdown), flash blanco al despegar  
**Notas críticas:** Sub-mesh "start_lights" obligatorio para cambiar colores por código

---

### PW-02 — POD BOOST (powerup racing)
**Categoría:** Powerup  
**Prompt para Meshy:**
```
Floating space boost pod, sphere shape, bright yellow with lightning bolt symbol,
more intense glow than the fuel pod, electric energy aesthetic, metallic base with electric lines in relief.
Two sub-meshes: "pod_body", "pod_glow".
```
**Animaciones:** Unity — rotación + flotación, shader destellos eléctricos  

---

### PW-03 — POD ESCUDO (powerup racing)
**Categoría:** Powerup  
**Prompt para Meshy:**
```
Floating space shield pod, sphere shape, semi-translucent blue, hexagonal pattern on the surface,
sci-fi energy barrier aesthetic, subtle defensive look.
Two sub-meshes: "pod_body", "pod_glow".
```
**Animaciones:** Unity — rotación lenta, shader hexágonos brillan al acercarse  

---

### PW-04 — POD EMP (powerup racing)
**Categoría:** Powerup  
**Prompt para Meshy:**
```
Floating space EMP pod, sphere shape, dark red with electric arc lines in relief on the surface,
threatening and aggressive look, electric energy emanating from cracks.
Two sub-meshes: "pod_body", "pod_glow".
```
**Animaciones:** Unity — vibración leve + rotación, shader arcos eléctricos rojos  

---

### PL-04 — JÚPITER
**Categoría:** Planeta  
**Prompt para Meshy:**
```
Planet Jupiter, realistic sphere, prominent horizontal bands in orange, brown, cream, and white,
large Great Red Spot visible in the southern hemisphere, realistic gas giant appearance.
No sub-meshes needed. Highly detailed texture.
```
**Animaciones:** Unity rota más rápido que otros planetas (10h real = rotación notablemente rápida en juego)

---

### PL-05 — SATURNO + ANILLOS
**Categoría:** Planeta con sub-mesh  
**Prompt para Meshy:**
```
Planet Saturn with prominent rings, realistic. Planet sphere: yellow-golden with soft horizontal bands.
Rings: flat disk around the equator, slightly inclined, with visible ring divisions.
CRITICAL: rings as a SEPARATE sub-mesh named "saturn_rings". Planet body named "saturn_body".
Rings should be semi-transparent with particle-like texture showing individual ring particles.
```
**Animaciones:** Unity — anillos rotan en plano ligeramente diferente al planeta  
**Notas críticas:** Sub-mesh separado es crítico — Unity les aplica transparencia y rotación diferente

---

### NV-03 — COHETE LANZADOR (cinemática intro)
**Categoría:** Nave cinemática  
**Prompt para Meshy:**
```
Space rocket, Falcon 9 / Starship hybrid style, tall and slim, bottom engine nozzles cluster,
"NAVIGATOR" logo on the side, realistic aerospace design. Only appears in launch cinematic.
Sub-mesh the engine nozzle cluster as "rocket_nozzles".
```
**Animaciones:** Unity keyframe — sube verticalmente (partículas humo en Unity), separación primera etapa  

---

## PRIORIDAD 3 — POLISH Y CONTENIDO COMPLETO

---

### PJ-02 — COMANDANTE REYES (NPC Mission Control)
**Categoría:** Personaje NPC — solo busto  
**Prompt para Meshy:**
```
Female astronaut commander, ~45 years old, NASA dark blue uniform, communication headset,
authoritative and confident expression. BUST ONLY (shoulders up). Detailed face.
Short hair, professional appearance. Communication screen frame around her as part of the model.
T-pose upper body for Mixamo.
```
**Después en Mixamo:** idle_talk (upper body only), point, thumbsup  
**Notas críticas:** Solo de hombros para arriba — aparece en una pantalla del cockpit, no en el mundo 3D

---

### PJ-03 — AGENTE KRONOS (antagonista)
**Categoría:** Personaje NPC enemigo  
**Prompt para Meshy:**
```
Enemy space pilot, antagonist. Same body proportions as the cadet but:
black suit with red lines, dark helmet with red visor, private agency logo on shoulder.
T-pose. Full body. Menacing aesthetic but same rig structure as PJ-01 for Mixamo reuse.
```
**Después en Mixamo:** idle_cockpit, taunt, defeated (reusar skeleton del Cadete)  
**Notas críticas:** Proporciones idénticas al Cadete — se reutiliza el skeleton de Mixamo

---

### PJ-04 — DR. NAKAMURA (NPC científico)
**Categoría:** Personaje NPC — solo busto  
**Prompt para Meshy:**
```
Male scientist, ~55 years old, white lab coat over a spacesuit, glasses, tablet in left hand.
Academic appearance. BUST ONLY (shoulders up). Detailed face. Friendly expression.
T-pose upper body.
```
**Después en Mixamo:** idle_talk, point, write on tablet  

---

### EN-03 — ESTACIÓN ESPACIAL HUB (lobby)
**Categoría:** Entorno espacial  
**Prompt para Meshy:**
```
Space station hub, ISS-inspired but more modern, modular design with large central module,
extending arms, solar panels at the extremities, interior windows with warm light glow,
docking ports for player ships, NASA / NAVIGATOR logos visible.
Sub-mesh solar panels as "solar_panels" for rotation.
Medium-large scale. View from exterior only.
```
**Animaciones:** Unity code — paneles solares rotan hacia el "sol", luces de atraque parpadean (shader)

---

### EN-04 — BASE EN PLANETA (objetivo final)
**Categoría:** Estructura en superficie  
**Prompt para Meshy:**
```
Small scientific base on a planet surface, central dome, communication antennas,
rover vehicles parked nearby, NASA logo visible, modular habitat pods connected.
Appears on planet surface when player arrives. Medium detail, seen from distance.
Sub-mesh main antenna as "main_antenna".
```
**Animaciones:** Unity code — antena rota lentamente, luces cúpula parpadean

---

### PL-01 — MERCURIO
**Prompt para Meshy:**
```
Planet Mercury, realistic sphere, gray-brown surface with many craters of different sizes,
no atmosphere, heavily cratered barren look, slightly smaller than other planets.
```
**Animaciones:** Ninguna. Unity rota.

---

### PL-02 — VENUS
**Prompt para Meshy:**
```
Planet Venus, realistic sphere, thick dense cloud layers in yellow-orange tones,
no visible surface (completely covered by clouds), warm atmospheric glow at edges.
Multiple cloud layers visible as texture depth.
```
**Animaciones:** Ninguna. Unity rota.

---

### PL-06 — URANO + ANILLOS
**Prompt para Meshy:**
```
Planet Uranus, pale blue-cyan uniform sphere, almost no surface details,
thin faint rings around the equator, slightly tilted axis.
CRITICAL: rings as SEPARATE sub-mesh named "uranus_rings".
```
**Animaciones:** Unity — sub-mesh anillos rotan

---

### PL-07 — NEPTUNO
**Prompt para Meshy:**
```
Planet Neptune, deep intense blue sphere, visible dark spot (Great Dark Spot),
slight atmospheric haze at edges, more intense blue than Uranus.
```
**Animaciones:** Ninguna. Unity rota.

---

### OB-02 — PORTAL / AGUJERO DE GUSANO
**Prompt para Meshy:**
```
Wormhole portal ring, large circular ring structure floating in space,
glowing edges, sci-fi energy aesthetic, the interior is empty (Unity puts shader there).
Only the outer ring as geometry. Metallic with energy conduits on the frame.
```
**Animaciones:** Unity code — anillo rota lentamente, Unity shader interior (distorsión espacio)

---

### OB-03 — BALIZA DE MISIÓN
**Prompt para Meshy:**
```
Mission beacon, octahedron or diamond geometric shape, small, floating in space,
pulsing light indicator, futuristic marker design, metallic with glowing facets.
Sub-mesh "beacon_light" for emission animation.
```
**Animaciones:** Unity code — rotación lenta, pulso de luz

---

### OB-04 — SATÉLITE EN ÓRBITA
**Prompt para Meshy:**
```
Scientific satellite, small, solar panels extended, box-shaped main body,
antennas and sensors visible, realistic space probe aesthetic.
Sub-mesh solar panels as "solar_panels".
```
**Animaciones:** Unity code — órbita alrededor del planeta, paneles rotan hacia el sol

---

### OB-05 — ESCOMBROS DE NAVE (5 piezas separadas)
**Crear como 5 prompts separados en Meshy:**

**Pieza 1 — Panel roto:**
```
Broken spaceship panel, irregular torn metal piece, burned and bent edges, debris fragment.
```
**Pieza 2 — Motor quemado:**
```
Burned spaceship engine nozzle, damaged and melted, space debris fragment.
```
**Pieza 3 — Ventana fracturada:**
```
Cracked spaceship cockpit glass fragment, broken with jagged edges, debris piece.
```
**Pieza 4 — Trozo de ala:**
```
Broken spaceship wing piece, torn metal, damaged aerospace component, debris.
```
**Pieza 5 — Casco de nave:**
```
Spaceship hull fragment, bent and burned metal panel, outer armor piece, debris.
```
**Animaciones:** Unity code — cada pieza rota en dirección aleatoria, shader bordes con brillo metal quemado

---

### NV-04 — CÁPSULA DE SUMINISTROS
**Prompt para Meshy:**
```
Supply capsule, small cylindrical shape with extended solar panels, floating beacon light,
sci-fi resupply pod aesthetic, compact and functional design.
Sub-mesh "beacon" for blinking light.
```
**Animaciones:** Unity code — rotación lenta constante, beacon parpadeante (shader)

---

### IC-01 — CÁMARA CIENTÍFICA
**Prompt para Meshy:**
```
Scientific camera on extensible tripod, large camera head like a telescope on top,
foldable tripod legs, space-grade equipment aesthetic, deployed position.
Sub-mesh tripod as "tripod" and camera head as "camera_head".
```
**Animaciones:** Unity keyframe — trípode se extiende desde posición compacta, cámara rota horizontalmente

---

### IC-02 — ANTENA DE TRANSMISIÓN
**Prompt para Meshy:**
```
Parabolic transmission antenna with support structure, classic dish antenna shape,
space-grade, cables and support arm visible, deployed position.
Sub-mesh dish as "antenna_dish" for tracking rotation.
```
**Animaciones:** Unity keyframe — despliega desde posición cerrada, apunta hacia la Tierra (rota lentamente)

---

### IC-03 — MAGNETÓMETRO EN BRAZO
**Prompt para Meshy:**
```
Magnetometer on extensible robotic arm, cylindrical sensor at the tip of an articulated arm,
small rotor/spinner at the very tip, scientific instrument aesthetic.
Sub-mesh arm as "sensor_arm", sensor rotor as "sensor_rotor".
```
**Animaciones:** Unity keyframe — brazo se extiende, rotor gira constantemente

---

### IC-04 — PANEL CIENTÍFICO COMPLETO
**Prompt para Meshy:**
```
Complete scientific instrument panel, rectangular platform with multiple integrated instruments:
small antenna, sensors, solar cells, data recorders. Deploys as one unit.
Compact deployable science package. Space mission equipment aesthetic.
```
**Animaciones:** Unity keyframe — deploy general (se extiende), antena integrada gira

---

### PW-05 — POD GRAVITY ASSIST (powerup racing)
**Categoría:** Powerup especial  
**Prompt para Meshy:**
```
Gravitational vortex pod, NOT a sphere — twisted spiral or torus shape,
gravitational field visual aesthetic, dark purple/violet with swirling lines in relief,
3D spiral form suggesting a gravity well or vortex. Unusual and memorable shape.
Sub-mesh outer ring as "vortex_ring".
```
**Animaciones:** Unity — espiral rota constantemente, Unity shader distorsión gravitacional

---

## RESUMEN FINAL DE ANIMACIONES

### Lo que Meshy NO puede hacer (Unity / Mixamo lo hacen):
```
Mixamo (4 personajes):
  PJ-01 El Cadete:        idle_cockpit, idle_standing, walk, sit_down,
                           reach_lever, press_button, look_radar, celebrate, death
  PJ-02 Comandante Reyes: idle_talk, point, thumbsup
  PJ-03 Agente KRONOS:    idle_cockpit, taunt, defeated
  PJ-04 Dr. Nakamura:     idle_talk, point, write_tablet

Unity Shaders (no son modelos):
  Motor exhaust: partículas cyan/naranja detrás nave
  Explosión: bola de fuego + shockwave + escombros
  Boost trail: estela brillante al activar turbo
  Impacto escudo: destello al recibir daño
  Burbuja escudo activo: esfera hexagonal semitransparente (EF-01)
  Agujero negro: disco de acreción + lensing (PL-08)
  Atmósfera planeta: shader glow alrededor de planetas
  Nebulosa: nube de gas volumétrica
  Polvo espacial: partículas finas al avanzar
```

---

## CHECKLIST DE PROGRESO

Marca cada uno al terminarlo en Meshy:

**PRIORIDAD 1 (empieza aquí):**
- [ ] NV-01 — Nave del jugador
- [ ] EN-01 — Cockpit interior
- [ ] PL-03 — Marte
- [ ] PJ-01 — El Cadete
- [ ] PW-01 — Pod Combustible
- [ ] OB-01 — Asteroide

**PRIORIDAD 2:**
- [ ] NV-02 — Nave KRONOS
- [ ] EN-05 — Arco de salida
- [ ] PW-02 — Pod Boost
- [ ] PW-03 — Pod Escudo
- [ ] PW-04 — Pod EMP
- [ ] PL-04 — Júpiter
- [ ] PL-05 — Saturno + Anillos
- [ ] NV-03 — Cohete lanzador

**PRIORIDAD 3:**
- [ ] PJ-02 — Comandante Reyes
- [ ] PJ-03 — Agente KRONOS
- [ ] PJ-04 — Dr. Nakamura
- [ ] EN-03 — Estación espacial
- [ ] EN-04 — Base en planeta
- [ ] PL-01 — Mercurio
- [ ] PL-02 — Venus
- [ ] PL-06 — Urano + Anillos
- [ ] PL-07 — Neptuno
- [ ] OB-02 — Portal/Agujero gusano
- [ ] OB-03 — Baliza de misión
- [ ] OB-04 — Satélite en órbita
- [ ] OB-05 (×5) — Escombros de nave (5 piezas)
- [ ] NV-04 — Cápsula de suministros
- [ ] IC-01 — Cámara científica
- [ ] IC-02 — Antena de transmisión
- [ ] IC-03 — Magnetómetro en brazo
- [ ] IC-04 — Panel científico completo
- [ ] PW-05 — Pod Gravity Assist

---

## DESPUÉS DE MESHY — FLUJO COMPLETO

```
MESHY → descarga GLB/FBX
   ↓
MIXAMO (solo personajes PJ-01 a PJ-04)
   → Sube GLB al Mixamo, asigna rig automático
   → Descarga FBX con animaciones
   ↓
UNITY 6 (todos los demás)
   → Importa GLB/FBX en Assets/
   → Unity aplica shaders, código de animación, y VFX
   → Claude implementa todos los sistemas C#
   ↓
PLAYTEST en AR con cámara del celular
```

---

*Checklist creado por SAL / Guacalita Inc.*  
*Prompts Meshy y flujo Unity: Claude (asistente)*
