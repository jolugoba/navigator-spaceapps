# NAVIGATOR 2.0 — Estado generación Meshy v3 (2026-09-26)

**Créditos usados (estimado):** ~140 de 730 disponibles
**Nota:** Meshy no soporta imágenes 2D vía API. Solo genera modelos 3D (.glb).

---

## ✅ TODO DESCARGADO — 20/20

### A — Pods → `Assets/Models/Pods/`

| Archivo | MB |
|---|---|
| `PD-01_Fuel_FINAL.glb` | 19.4 |
| `PD-02_Boost_FINAL.glb` | 17.5 |
| `PD-03_Shield_FINAL.glb` | 43.2 |
| `PD-04_EMP_FINAL.glb` | 42.6 |
| `PD-05_Gravity_FINAL.glb` | 21.7 |

### B — Sondas NASA → `Assets/Models/Objects/`

| Archivo | MB |
|---|---|
| `OB-11_Messenger_FINAL.glb` | 32.7 |
| `OB-12_Magellan_FINAL.glb` | 39.9 |
| `OB-13_MRO_FINAL.glb` | 41.6 |

### C — Telescopios + ISS → `Assets/Models/Objects/`

| Archivo | MB |
|---|---|
| `OB-14_Hubble_FINAL.glb` | 126.4 |
| `OB-15_JWST_FINAL.glb` | 43.0 |
| `OB-16_ISS_FINAL.glb` | 23.5 |

### D — Lunas → `Assets/Models/Moons/`

| Archivo | MB |
|---|---|
| `MN-01_Phobos_FINAL.glb` | 9.0 |
| `MN-02_Io_FINAL.glb` | 5.4 |
| `MN-03_Titan_FINAL.glb` | 4.7 |
| `MN-04_Enceladus_FINAL.glb` | 4.0 |
| `MN-05_Miranda_FINAL.glb` | 14.9 |
| `MN-06_Triton_FINAL.glb` | 8.3 |

### E — Reserva → `Assets/Models/Objects/`

| Archivo | MB |
|---|---|
| `OB-17_DART_FINAL.glb` | 8.2 |
| `LV-01_SLS_FINAL.glb` | 20.7 |
| `OB-18_Rover_FINAL.glb` | 67.2 |

---

## Para activar en Unity

1. **Primero reduce el peso** (ver abajo). Luego abre Unity y deja que reimporte.
2. En Unity: **Navigator → Setup Scene**.
3. Los pods reemplazan las figuras de colores.
4. Las sondas, los telescopios y las lunas aparecen como puntos escaneables.
5. La reserva **ya está conectada**, también como puntos escaneables:
   - **DART**, entre los asteroides;
   - **SLS**, junto al despegue;
   - **Perseverance**, junto a Marte.

## ⚠️ Tamaño: hay que reducirlo antes de hacer la app

Los 20 modelos suman **unos 600 MB**, y el Hubble solo pesa 126 MB. Así la app sería enorme y lenta en un celular.
- *Texture Compression* de Unity **no** lo resuelve: los polígonos siguen ahí y el repositorio sigue pesado.
- Meshy ignoró `target_polycount` en modo *preview*.

**Solución (probada):** el script `tools/optimize_models.mjs` reduce polígonos y texturas de cada modelo.
- Pods y lunas: texturas de 512 px.
- Sondas: 1024 px.
- Planetas: 2048 px.

En la prueba bajó un modelo de 3,8 MB a 0,6 MB, sin extensiones especiales, así que glTFast lo abre igual.

```bash
# desde la raíz del repo (necesita Node 18+)
node tools/optimize_models.mjs --dry   # muestra qué va a reducir
node tools/optimize_models.mjs         # lo hace
```

Los originales quedan en `unity_project/models_original/`, que no se sube a git. Si un modelo se ve mal, copia de vuelta su original.
Revisa en Unity cómo se ven los pods y las sondas. Si alguno perdió demasiada forma, sube su `ratio` en `profile()` dentro del script y vuelve a correrlo sobre el original.

## 🔑 Clave de Meshy

La clave se había escrito otra vez en este documento y ya la borré. Pero quedó en el historial de git, junto con las veces anteriores.
**Cámbiala en meshy.ai → API** y no la pegues en archivos del repo. Si un script la necesita, guárdala en una variable de entorno (`MESHY_API_KEY`).
