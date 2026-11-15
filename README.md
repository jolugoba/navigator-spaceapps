# NAVIGATOR 2.0 — NASA Space Apps Challenge 2026

**Challenge:** *Space Mission Design Game* · **Team:** Guacalita Inc.

An augmented-reality, multiplayer mission game: you **design** a spacecraft under real engineering
constraints, **pick the launch date** from real planetary orbits, **fly** it with real gravity assists,
**operate** the science at the planet and get a **debrief** that explains how every decision shaped
the result — and what would have happened if you had chosen differently.

> 🇪🇸 Versión en español más abajo.

## Try it now — no install

**[https://space.guacalita.co](https://space.guacalita.co)** — runs in the browser on any phone or desktop (Chrome / Edge recommended).
Camera and GPS required for AR and sky finder; grant them when the browser asks.

**Demo video (30 s):** [https://youtu.be/6afA6otEjbM](https://youtu.be/6afA6otEjbM)
**Presentation:** [`Navigator_2026_Presentation.pdf`](Navigator_2026_Presentation.pdf)

---

## How it answers the challenge

| Challenge asks for | Navigator |
|---|---|
| Mission objectives | 7 planets × flyby / orbit / landing — each costs ΔV and multiplies science |
| Spacecraft design | Engine (chemical / ion / nuclear), propellant, power (solar / RTG / reactor), antenna, launcher, 8 instruments |
| Engineering trade-offs | More propellant → more mass → bigger, pricier launcher → fewer instruments (Tsiolkovsky) |
| Orbital constraints | **Real launch windows:** NASA/JPL Keplerian elements + Lambert's problem (porkchop scan). Off-window launches cost thousands of m/s; waiting costs money |
| Power | Solar power falls as 1/r² — useless past Jupiter; RTGs work anywhere |
| Communications | Science only scores if the antenna sends it home before the pass ends; light-time delay per planet |
| Manage limited resources | Cruise anomalies (solar storm, failure, blackout, navigation error) force decisions with fuel, power and time |
| Evaluate success | Score + debrief: what each decision did, **"what if…?"** recomputed with the real equations, and **you vs the real NASA mission** (Mariner, MESSENGER, Magellan, MRO, Perseverance, Juno, Cassini, Voyager) |

---

## Real physics, validated

- **Launch windows** — Kepler's equation on JPL elements, universal-variable Lambert solver, patched conics (300 km LEO departure, elliptical capture). Validated against real windows: Mars Nov 2026 & Dec 2028, Venus Jul 2026 & Mar 2028, Jupiter every ~13 months, Mercury every ~116 days; Earth–Mars Hohmann 259 days.
- **Rocket equation** — ΔV = Isp·g₀·ln(m_wet/m_dry), per engine.
- **Gravity** — each planet pulls with a = μ/r²; skim the assist ring for free ΔV (like Voyager 2).
- **Free exploration** — the Solar System where the planets really are *today*.

---

## Modes

- **Solo / Campaign** — 8 missions on a world map, from Mars flyby to Neptune, with star goals.
- **Multiplayer P2P, no server** — friends join by QR code; no accounts, no server.
- **Teacher mode** — a teacher runs a class session, sets missions and sees results.
- **Free flight** — open, shared Solar System with a scanner logbook of discoveries.
- **Sky finder** — point your phone at the sky; shows where your planet really is tonight (NASA JPL Horizons).
- **Hangar** — 10 selectable ships. **Languages** — English / Spanish.

---

## NASA data used

| Data | Source | Used for |
|---|---|---|
| Approximate Keplerian elements (E. M. Standish) | NASA/JPL Solar System Dynamics | Launch windows, transfer times, free-flight Solar System |
| Horizons ephemerides | NASA/JPL Horizons | "Where is it tonight?" sky finder |
| Mission facts (mass, cost, trip time) | NASA Science, NASA/JPL, NSSDCA | Debrief comparison cards |
| Hubble / Spitzer images | NASA, ESA, STScI | Sky nebulae and galaxies |

---

## AI disclosure (required by Space Apps)

- 3D models were generated with **Meshy AI**.
- Code and documentation were developed with **Claude Code (Anthropic)**.
- All physics was validated by the team against published values.

---

## Repository structure

| Folder | What it is |
|---|---|
| `unity_project/NavigatorUnity/` | The game (Unity 6, URP) |
| `flutter_app/` | Installable app (Flutter) embedding the Unity game |

**Run it:** open the Unity project → menu **Navigator → Setup Scene** → Play.

---

## 🇪🇸 En español

Juego multijugador de misiones espaciales en realidad aumentada: diseñas la nave (motor, energía, antena, instrumentos, presupuesto), **eliges la fecha de lanzamiento con órbitas reales**, la pilotas con asistencias gravitatorias reales, operas la ciencia en el planeta y recibes un informe con **"¿y si hubieras…?"** y la comparación con la misión real de la NASA.

**Pruébalo ahora:** [https://space.guacalita.co](https://space.guacalita.co) — funciona en el navegador, sin instalar nada. Requiere cámara y GPS para la RA y el buscador de planetas.

**Modos:**
- **Solo / Campaña** — 8 misiones del mapa mundial, de Marte a Neptuno.
- **Multijugador sin servidor** — los jugadores se unen por código QR, sin cuentas.
- **Modo Profesor** — el profesor conecta a la clase, lanza preguntas y ve resultados.
- **Vuelo libre** — el Sistema Solar con los planetas donde están hoy.
- **¿Dónde está esta noche?** — apuntas el celular al cielo y ves tu planeta con datos de NASA JPL Horizons.
