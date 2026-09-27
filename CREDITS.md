# Navigator 2.0 — Credits / Créditos

Navigator was made for the **NASA Space Apps Challenge 2026** by Guacalita Inc.
The same list appears in the app (lobby → **CREDITS**).

## NASA data and science

| What | Source |
|---|---|
| Planetary orbits for launch windows and the free-flight Solar System | NASA/JPL "Approximate Positions of the Planets" (E. M. Standish), Keplerian elements — https://ssd.jpl.nasa.gov/planets/approx_pos.html |
| Real-time planet positions ("Where is it tonight?", planet placement) | NASA/JPL **Horizons** System — https://ssd.jpl.nasa.gov/horizons/ |
| Mission facts, ΔV, orbit-insertion burns, probe histories, planet and moon facts | NASA Science (science.nasa.gov), NASA/JPL mission pages, NSSDCA |
| Real flyby routes (MESSENGER, Galileo, Cassini, Voyager 2) | NASA/JPL mission trajectories |

## Sky images (`unity_project/NavigatorUnity/Assets/Art/Sky`)

**Before submitting, check the license of each image on its source page.** Rows marked ⚠️ are **not** NASA public domain, or their author still has to be confirmed. If a license asks for attribution (CC BY), this credit line is enough. If a license doesn't allow the use (or can't be confirmed), replace the image with a NASA one.

| File | Source file | Credit | License |
|---|---|---|---|
| `nebula_01.jpg` | Wikimedia Commons: *Orion Nebula – Hubble 2006 mosaic* | NASA, ESA, M. Robberto (STScI/ESA) and the Hubble Space Telescope Orion Treasury Project Team | Public domain (NASA/Hubble) |
| `nebula_02.jpg` | Wikimedia Commons: *Comets Kick up Dust in Helix Nebula (PIA09178)* | NASA/JPL-Caltech (Spitzer) | Public domain (NASA) |
| `nebula_03.jpg` | Wikimedia Commons: *NGC 3372a-full.jpg* | ⚠️ confirm author on the file page | ⚠️ confirm |
| `nebula_04.jpg` | Wikimedia Commons: *Pillars of creation 2014 HST WFC3-UVIS full-res denoised* | NASA, ESA and the Hubble Heritage Team (STScI/AURA); ⚠️ "denoised" is a derivative made by a Commons user | ⚠️ confirm the derivative's license |
| `nebula_05.jpg` | ESA/Hubble *heic1310a* (Ring Nebula) | NASA, ESA, and the Hubble Heritage (STScI/AURA)-ESA/Hubble Collaboration | ⚠️ CC BY 4.0: attribution required |
| `nebula_06.jpg` | Wikimedia Commons: *Horsehead_Nebula.jpg* | ⚠️ confirm author on the file page | ⚠️ confirm |
| `blackhole_01.jpg` | Wikimedia Commons: *Black hole – Messier 87 crop max res* | Event Horizon Telescope Collaboration | ⚠️ CC BY 4.0: attribution required |
| `galaxy_01.jpg` | Wikimedia Commons: *Andromeda_Galaxy_560mm_FL.jpg* | ⚠️ amateur astrophotograph: confirm author | ⚠️ confirm (probably CC BY-SA). If in doubt, replace with a NASA/Hubble image of Andromeda |
| `galaxy_02.jpg` | Wikimedia Commons: *Messier51_sRGB.jpg* | NASA, ESA, S. Beckwith (STScI) and the Hubble Heritage Team (STScI/AURA) | Public domain (NASA/Hubble); confirm on the file page |
| `galaxy_03.jpg` | Wikimedia Commons: *Hubble Deep Field South (1998-41-735)* | NASA, ESA and the HDF-S team (STScI) | Public domain (NASA/Hubble) |

## 3D models

Ships, planets, moons, probes and objects in `Assets/Models`: generated with **Meshy AI** (meshy.ai) by the team.

## Software and libraries

| Component | License |
|---|---|
| Unity 6 (URP, TextMeshPro, Input System) | Unity Terms of Service |
| glTFast (Unity) | Apache 2.0 |
| ZXing.Net (QR codes) | Apache 2.0 |
| jsQR 1.4.0 (QR reader in the web build) — Cosmo Wolfe | Apache 2.0 |
| flutter_embed_unity (James Allen) | MIT (`Assets/FlutterEmbed/LICENSE.txt`) |
| Flutter, shared_preferences, permission_handler | BSD / MIT |
| Senda P2P (Guacalita Inc.), built on Iroh (n0) | Iroh: MIT / Apache 2.0 |
| Liberation Sans font (TextMeshPro default) | SIL Open Font License |

All sounds are synthesized in code (`Game/AudioFX.cs`); no audio files are used.
