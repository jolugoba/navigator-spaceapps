# NAVIGATOR 2.0 — Estado generación Meshy v3 (2026-09-26)

**Clave usada:** `msy_7piEK37EPJrZ1a9e4SXtHAqoUIqBRrL6cM2n`
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

1. En Unity: **Navigator → Setup Scene**
2. Los pods reemplazan automáticamente los puntos de colores
3. Las sondas, telescopios y lunas aparecen como landmarks escaneables
4. Los modelos de reserva (DART, SLS, Rover) están listos — pídele a Claude que los conecte cuando quieras

## Nota sobre el tamaño

Los modelos salen 4-126 MB porque Meshy genera en alta resolución.
El `target_polycount: 20000` se envió en todas las llamadas pero Meshy lo ignora en modo preview.
Para reducir el tamaño en la app Android: Project Settings → Player → Texture Compression.
