# NAVIGATOR 2.0 — NASA Space Apps Challenge 2026

**Challenge:** *Space Mission Design Game* · **Team:** Guacalita Inc.

An augmented-reality, multiplayer mission game: you **design** a spacecraft under real engineering
constraints, **pick the launch date** from real planetary orbits, **fly** it with real gravity assists,
**operate** the science at the planet and get a **debrief** that explains how every decision shaped
the result — and what would have happened if you had chosen differently.

> 🇪🇸 Versión en español más abajo.

## How it answers the challenge

| Challenge asks for | Navigator | Code |
|---|---|---|
| Mission objectives | 7 planets × flyby / orbit / landing — each costs ΔV and multiplies science | `Core/MissionDesign.cs` |
| Spacecraft design | Engine (chemical / ion / nuclear), propellant, power (solar / RTG / reactor), antenna, launcher, 8 instruments | `Core/MissionDesign.cs` |
| Engineering trade-offs | More propellant → more mass → bigger, pricier launcher → fewer instruments (Tsiolkovsky) | `DeltaVms`, `IsMassOk`, `IsBudgetOk` |
| Orbital constraints | **Real launch windows:** NASA/JPL Keplerian elements + Lambert's problem (porkchop scan). Off-window launches cost thousands of m/s; waiting costs money | `Core/Orbits.cs` |
| Power | Solar power falls as 1/r² — useless past Jupiter; RTGs work anywhere | `EffectivePowerW` |
| Communications | Science only scores if the antenna sends it home before the pass ends; light-time delay per planet | `UI/ScienceOpsUI.cs` |
| Manage limited resources | Cruise anomalies (solar storm, failure, blackout, navigation error) force decisions with fuel, power and time | `UI/MissionEventsUI.cs` |
| Evaluate success | Score + debrief: what each decision did, **"what if…?"** recomputed with the real equations, and **you vs the real NASA mission** (Mariner, MESSENGER, Magellan, MRO, Perseverance, Juno, Cassini, Voyager) | `UI/MissionReport.cs` |

## Real physics, validated

- **Launch windows** — Kepler's equation on JPL elements, universal-variable Lambert solver,
  patched conics (300 km LEO departure, elliptical capture). Validated against real windows:
  Mars Nov 2026 & Dec 2028, Venus Jul 2026 & Mar 2028, Jupiter every ~13 months, Mercury every
  ~116 days; Earth–Mars Hohmann 259 days. Details: [`NAVIGATOR_FISICA_REAL.md`](NAVIGATOR_FISICA_REAL.md).
- **Rocket equation** — ΔV = Isp·g₀·ln(m_wet/m_dry), per engine.
- **Gravity** — each planet pulls with a = μ/r²; skim the assist ring for free ΔV (like Voyager 2).
- **Free exploration** — the Solar System where the planets really are *today*.

## Modes

- **Campaign / world map** — 8 missions, from Mars flyby to Neptune, with star goals.
- **Multiplayer P2P, no server** — friends join by QR; race, scan and tag each other (Iroh/Senda).
- **Free flight** — open, shared Solar System with a scanner logbook of discoveries.
- **Teacher mode** — a teacher runs a class session, sets missions and quizzes, sees results.
- **Hangar** — 10 selectable ships. **Languages** — English / Spanish.

## Repository

| Folder | What it is |
|---|---|
| [`unity_project/NavigatorUnity/`](NAVIGATOR_UNITY_QUICKSTART.md) | The game (Unity 6, URP). |
| [`flutter_app/`](flutter_app/README.md) | The installable app (Flutter) embedding the Unity game. |
| `lib/`, `android/`, `ios/` (root) | Earlier Flutter prototype, reference only. |

**Run it:** open the Unity project → menu **Navigator → Setup Scene** → Play.
See [`NAVIGATOR_UNITY_QUICKSTART.md`](NAVIGATOR_UNITY_QUICKSTART.md) and [`flutter_app/README.md`](flutter_app/README.md).
**Design & equations:** [`NAVIGATOR_GAME_DESIGN_COMPLETE.md`](NAVIGATOR_GAME_DESIGN_COMPLETE.md).
**Credits & licenses:** [`CREDITS.md`](CREDITS.md) · **Submission kit:** [`SPACE_APPS_SUBMISSION.md`](SPACE_APPS_SUBMISSION.md).

---

## 🇪🇸 En español

Juego multijugador de misiones espaciales en realidad aumentada: diseñas la nave (motor, energía, antena,
instrumentos, presupuesto), **eliges la fecha de lanzamiento con órbitas reales**, la pilotas con
asistencias gravitatorias reales, operas la ciencia en el planeta y recibes un informe con
**"¿y si hubieras…?"** y la comparación con la misión real de la NASA.

**Empezar:** [`NAVIGATOR_UNITY_QUICKSTART.md`](NAVIGATOR_UNITY_QUICKSTART.md), luego [`flutter_app/README.md`](flutter_app/README.md).
**Diseño y ecuaciones:** [`NAVIGATOR_GAME_DESIGN_COMPLETE.md`](NAVIGATOR_GAME_DESIGN_COMPLETE.md) · [`NAVIGATOR_FISICA_REAL.md`](NAVIGATOR_FISICA_REAL.md).
**Créditos y licencias:** [`CREDITS.md`](CREDITS.md).
