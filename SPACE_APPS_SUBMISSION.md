# Space Apps 2026 — Submission kit (Navigator 2.0)

Text ready to paste into the project page on spaceappschallenge.org, the video script and the final
checklist. Fields in **[brackets]** are for the team to fill in.

---

## 1. Project name
**Navigator 2.0 — Design it. Launch it on time. Fly it. Learn why it worked.**

## 2. High-level summary (short field)

Navigator is a multiplayer augmented-reality game where players design a complete space mission
under the same constraints NASA engineers face. They choose the destination and objective, engine,
propellant, power, antenna, launcher and science instruments within a budget and the launcher's
mass limit. Then they pick the launch date from real planetary orbits (NASA/JPL elements +
Lambert's problem). Launching outside a window costs thousands of m/s; waiting costs money.
Players fly the mission in AR with real gravity assists, handle cruise anomalies and operate the
science. Data only counts if the antenna gets it home in time. At the end, a debrief explains how
every decision shaped the result, recomputes "what if you had chosen differently?" with real
equations and compares the player's spacecraft with the real NASA mission that flew the same
profile.

## 3. Project details (long field)

**What it does**
- **Design:** 7 planets × flyby / orbit / landing; chemical, ion or nuclear engine; solar, RTG or
  reactor power; 3 antennas; 4 launchers; 8 instruments whose value depends on the planet (radar
  sees through Venus' clouds, magnetometers shine at Jupiter). Every choice costs money, mass or
  power: there is no perfect design, only trade-offs.
- **Launch window:** a porkchop strip shows the extra ΔV for every launch date in the next
  synodic cycle. Green valleys are the windows.
- **Fly & operate:** AR flight with Newtonian gravity and assist rings. Cruise anomalies (solar
  storm, component failure, blackout, navigation error) are resolved with the resources the
  design left. At the planet, instruments need power and data must be downlinked before the pass
  ends, with the real light-time delay.
- **Debrief:** cause → effect for propulsion, objective, power, communications and budget. It
  includes "what if…" lines (launching in the window, exact propellant missing or spare, ion
  engine, RTG vs solar, high-gain antenna) and a card comparing you with Mariner, MESSENGER,
  Magellan, MRO, Perseverance, Juno, Cassini or Voyager.
- **Modes:** 8-mission campaign on a world map; serverless peer-to-peer multiplayer (join by QR);
  a free, shared Solar System with the planets where they really are today; teacher mode for
  classrooms; 10 selectable ships; English and Spanish.

**How it works (real physics)**
- **Rocket equation:** Tsiolkovsky, ΔV = Isp·g₀·ln(m_wet/m_dry).
- **Solar power:** falls as 1/r².
- **Gravity:** a = μ/r².
- **Launch windows:** Kepler's equation on NASA/JPL approximate Keplerian elements, a
  universal-variable Lambert solver and patched conics (300 km LEO departure, elliptical capture).
- **Validation:** checked against real windows — Mars Nov 2026 and Dec 2028, Venus Jul 2026 and
  Mar 2028, Jupiter every ~13 months, Mercury every ~116 days, Earth–Mars Hohmann 259 days.

**Benefits**
- Students learn *why* missions look the way they do: why Juno carried giant solar panels while
  Voyager needed RTGs, why Mars missions leave every 26 months, why Cassini took 7 years.
- They learn it by making the decisions and seeing the consequences, not by reading about them.
- Teacher mode makes it usable in a class; P2P play needs no server or accounts.

**What we hope to achieve**
- A free classroom tool, bilingual from day one, that turns systems engineering and orbital
  mechanics into a game students want to replay: "improve the design and try again".

**Tools and languages**
- Unity 6 (C#, URP, Input System, glTFast), Flutter (host app, flutter_embed_unity).
- Iroh/Senda (peer-to-peer networking).
- Python (to validate the orbital mechanics before porting it to C#).

## 4. Space agency data used

| Data | Source | Used for |
|---|---|---|
| Approximate Keplerian elements of the planets (E. M. Standish) | NASA/JPL Solar System Dynamics — ssd.jpl.nasa.gov | Launch windows, transfer times, free-flight Solar System |
| Horizons ephemerides | NASA/JPL Horizons | "Where is it tonight?" sky finder |
| Mission facts (mass, cost, trip time, flybys, insertion burns) | NASA Science, NASA/JPL mission pages, NSSDCA | Debrief comparison, ΔV tables, scanner facts |
| Hubble / Spitzer images | NASA, ESA, STScI | Sky nebulae and galaxies |

## 5. Use of Artificial Intelligence (Space Apps requires disclosing it)

- 3D models (ships, planets, probes, objects) were generated with **Meshy AI** from the team's
  prompts. Models downloaded from the Meshy community library (if any) are by other authors;
  their names and licenses are listed in `CREDITS.md` ("Third-party models").
- Code and documentation were developed with the help of an **AI coding assistant (Claude Code)**.
- All physics was validated by the team against published values.
- **[Add or adjust anything else the team used.]**

---

## 6. Video script (≤ 30 s)

| Time | On screen | Narration / text |
|---|---|---|
| 0–4 s | Phone on a table, the Solar System appears in AR | "You are NASA's mission designer." |
| 4–10 s | Design screen: switch engine and fuel; the ΔV and budget bars react | "Every choice costs money, mass or power." |
| 10–15 s | LAUNCH WINDOW: drag the slider off the green valley; the ΔV jumps, then BEST DATE | "The planets move: launch on the right day, with real orbits." |
| 15–22 s | Flight: skim a planet's ring, "+ΔV gravity assist", a solar storm alert | "Fly it, use gravity, survive the cruise." |
| 22–27 s | Science downlink, then the debrief "WHAT IF…?" and "YOU vs THE REAL MISSION" (Juno) | "Then find out why it worked, and how you compare with NASA." |
| 27–30 s | Logo + "Navigator · Space Apps 2026" | — |

Tips:
- Record the screen on the phone and add the AR shot with a second phone.
- Use large text overlays: judges often watch without sound.

---

## 7. Final checklist before submitting

- [ ] Full test on the phone: design → window → flight → science → debrief (EN and ES).
- [ ] Multiplayer test with 2 phones (QR).
- [ ] `node tools/optimize_models.mjs` run and build size checked.
- [ ] Image licenses marked ⚠️ in `CREDITS.md` confirmed or replaced.
- [ ] Every model downloaded from the Meshy library listed in `CREDITS.md` with author and license.
- [ ] Meshy API key rotated (it appeared in chats).
- [ ] Repository public (or judges' access) and link on the project page.
- [ ] APK / build link on the page **[link]**.
- [ ] 30 s video uploaded **[link]**.
- [ ] Team members and their roles listed on the page **[names]**.
