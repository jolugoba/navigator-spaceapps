// Navigator 2.0 — Loc.cs
// Game-wide localization. English is the default; the choice is saved in PlayerPrefs.
// Loc.T("key") for fixed text, Loc.F("key", args) for text with values ({0}, {1}…).
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Core
{
    public enum Lang { English = 0, Spanish = 1 }

    public static class Loc
    {
        const string PrefKey = "navigator.lang";

        static bool _loaded;
        static Lang _current = Lang.English;

        public static event Action Changed;

        public static Lang Current
        {
            get
            {
                // Read lazily: PlayerPrefs may not be touched from static constructors
                if (!_loaded) { _current = (Lang)PlayerPrefs.GetInt(PrefKey, (int)Lang.English); _loaded = true; }
                return _current;
            }
        }

        public static void Set(Lang lang)
        {
            _loaded  = true;
            _current = lang;
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Toggle() => Set(Current == Lang.English ? Lang.Spanish : Lang.English);

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (!Table.TryGetValue(key, out var e)) return key;
            return Current == Lang.Spanish ? e.es : e.en;
        }

        public static string F(string key, params object[] args) => string.Format(T(key), args);

        public static string Planet(string id)      => T("planet." + id);
        public static string Objective(string id)   => T("objective." + id);
        public static string Instrument(ScienceInstrument i) => T("inst." + i);

        static readonly Dictionary<string, (string en, string es)> Table = new()
        {
            // ── Common ────────────────────────────────────────────────────────
            { "lang.button",        ("ESPAÑOL", "ENGLISH") },
            { "player.default",     ("Cadet", "Cadete") },
            { "rival",              ("Rival", "Rival") },
            { "you",                ("You", "Tú") },

            // ── Planets / objectives / instruments ───────────────────────────
            { "planet.mercury", ("Mercury", "Mercurio") },
            { "planet.venus",   ("Venus", "Venus") },
            { "planet.mars",    ("Mars", "Marte") },
            { "planet.jupiter", ("Jupiter", "Júpiter") },
            { "planet.saturn",  ("Saturn", "Saturno") },
            { "planet.uranus",  ("Uranus", "Urano") },
            { "planet.neptune", ("Neptune", "Neptuno") },

            { "objective.flyby",   ("Flyby", "Sobrevuelo") },
            { "objective.orbit",   ("Orbit", "Órbita") },
            { "objective.landing", ("Landing", "Aterrizaje") },

            { "inst.WideAngleCamera",   ("Wide-angle camera", "Cámara gran angular") },
            { "inst.NarrowAngleCamera", ("Narrow-angle camera", "Cámara teleobjetivo") },
            { "inst.UVSpectrometer",    ("UV spectrometer", "Espectrómetro UV") },
            { "inst.InfraredMapper",    ("Infrared mapper", "Mapeador infrarrojo") },
            { "inst.Magnetometer",      ("Magnetometer", "Magnetómetro") },
            { "inst.ParticleDetector",  ("Particle detector", "Detector de partículas") },
            { "inst.RadioScience",      ("Radio science", "Radio ciencia") },
            { "inst.RadarAltimeter",    ("Radar altimeter", "Radar altímetro") },

            { "engine.Chemical", ("Chemical", "Químico") },
            { "engine.Ion",      ("Ion", "Iónico") },
            { "engine.Nuclear",  ("Nuclear", "Nuclear") },
            { "fuel.Chemical",   ("LOX / LH2 (liquid oxygen + hydrogen)", "LOX / LH2 (oxígeno + hidrógeno líquido)") },
            { "fuel.Ion",        ("Ionized xenon (noble gas)", "Xenón ionizado (gas noble)") },
            { "fuel.Nuclear",    ("LH2 heated by a reactor (NTR)", "LH2 calentado por reactor (NTR)") },
            { "nasa.engine.Chemical", ("Space Shuttle, SLS: high thrust, ideal for nearby planets.", "Space Shuttle, SLS: alto empuje, ideal para planetas cercanos.") },
            { "nasa.engine.Ion",      ("Dawn probe (Vesta + Ceres): 8× more fuel-efficient.", "Sonda Dawn (Vesta + Ceres): 8× más eficiente en combustible.") },
            { "nasa.engine.Nuclear",  ("NASA NERVA program (tested): best thrust/Isp balance.", "Programa NERVA (NASA, probado): mejor balance empuje/Isp.") },

            { "power.Solar",          ("Solar", "Solar") },
            { "power.RTG",            ("RTG", "RTG") },
            { "power.NuclearReactor", ("Reactor", "Reactor") },
            { "nasa.power.Solar",          ("Juno uses 72 m² of panels at Jupiter. At Neptune they give 0.1%.", "Juno usa 72 m² de paneles en Júpiter. En Neptuno dan el 0.1 %.") },
            { "nasa.power.RTG",            ("Voyager, Cassini, New Horizons: constant power at any distance.", "Voyager, Cassini, New Horizons: potencia constante a cualquier distancia.") },
            { "nasa.power.NuclearReactor", ("Kilopower (NASA): compact reactor for the outer planets.", "Kilopower (NASA): reactor compacto para planetas exteriores.") },

            { "antenna.LowGain",    ("Low gain", "Baja") },
            { "antenna.MediumGain", ("Medium gain", "Media") },
            { "antenna.HighGain",   ("High gain", "Alta") },

            // ── Real missions ────────────────────────────────────────────────
            { "mission.none",       ("none", "ninguna") },
            { "mission.first",      ("none yet: you would be the first", "ninguna todavía: serías la primera") },
            { "mission.proposal",   ("{0} (NASA proposal)", "{0} (propuesta NASA)") },

            // ── Lobby ────────────────────────────────────────────────────────
            { "lobby.subtitle",     ("NASA Space Apps 2026 · AR space race · serverless P2P", "NASA Space Apps 2026 · Carrera espacial AR · P2P sin servidor") },
            { "lobby.qrhint",       ("Your rival scans this code to connect", "Tu rival escanea este código para conectarse") },
            { "lobby.starting",     ("Starting P2P network…", "Iniciando red P2P…") },
            { "lobby.offline",      ("P2P network unavailable: you can play solo", "Red P2P no disponible: puedes jugar solo") },
            { "lobby.scan",         ("Scan QR", "Escanear QR") },
            { "lobby.cancel",       ("Cancel", "Cancelar") },
            { "lobby.campermission",("Allow camera access and tap Scan again.", "Permite el uso de la cámara y vuelve a tocar Escanear.") },
            { "lobby.aim",          ("Point the camera at the other player's QR…", "Apunta la cámara al QR del otro jugador…") },
            { "lobby.connecting",   ("Connecting…", "Conectando…") },
            { "lobby.connected",    ("Connected to {0}…", "Conectado a {0}…") },
            { "lobby.connecterror", ("Connection error. Try again.", "Error de conexión. Intenta de nuevo.") },
            { "lobby.solo",         ("PLAY SOLO", "JUGAR SOLO") },
            { "lobby.launch",       ("LIFT OFF!", "¡DESPEGAR!") },
            { "lobby.waiting",      ("Waiting for players…\nShow your QR or scan another player's", "Esperando jugadores…\nMuestra tu QR o escanea el de otro jugador") },
            { "lobby.players",      ("{0}/{1} players connected: ready for lift-off!\nMore players can scan any connected phone.", "{0}/{1} jugadores conectados: ¡listos para despegar!\nMás jugadores pueden escanear cualquier celular conectado.") },

            // ── Mission design ───────────────────────────────────────────────
            { "design.title",       ("MISSION DESIGN", "DISEÑO DE MISIÓN") },
            { "design.subtitle",    ("Every kilogram counts: Tsiolkovsky decides if you get there", "Cada kilo cuenta: Tsiolkovsky decide si llegas") },
            { "design.destination", ("DESTINATION", "DESTINO") },
            { "design.objective",   ("OBJECTIVE", "OBJETIVO") },
            { "design.engine",      ("ENGINE", "MOTOR") },
            { "design.fuel",        ("FUEL", "COMBUSTIBLE") },
            { "design.deltav",      ("ΔV · TSIOLKOVSKY ROCKET EQUATION", "ΔV · ECUACIÓN DE TSIOLKOVSKY") },
            { "design.power",       ("POWER", "ENERGÍA") },
            { "design.antenna",     ("ANTENNA · COMMUNICATIONS", "ANTENA · COMUNICACIONES") },
            { "design.launcher",    ("LAUNCH VEHICLE", "LANZADOR") },
            { "design.instruments", ("SCIENCE INSTRUMENTS", "INSTRUMENTOS CIENTÍFICOS") },
            { "design.launchbtn",   ("LAUNCH MISSION", "LANZAR MISIÓN") },
            { "design.solarwarn",   ("Solar panels are nearly useless this far from the Sun", "Paneles solares casi inútiles tan lejos del Sol") },
            { "design.signal",      ("Signal: {0}", "Señal: {0}") },
            { "design.required",    ("Required: {0:N0} m/s ({1})", "Requerido: {0:N0} m/s ({1})") },
            { "design.dv.cruise",   ("{0:N0} cruise", "{0:N0} viaje") },
            { "design.dv.insert",   (" + {0:N0} insertion", " + {0:N0} inserción") },
            { "design.dv.descent",  (" + {0:N0} descent", " + {0:N0} descenso") },
            { "design.obj.flyby",   ("Fly past: no extra cost · science ×1", "Pasas de largo: sin costo extra · ciencia ×1") },
            { "design.obj.orbit",   ("+{0:N0} m/s to brake into orbit · science ×1.5", "+{0:N0} m/s para frenar en órbita · ciencia ×1.5") },
            { "design.obj.landing", ("+{0:N0} m/s, +{1:F0} kg and +${2:F0}M lander · science ×2", "+{0:N0} m/s, +{1:F0} kg y +${2:F0}M de módulo · ciencia ×2") },
            { "design.obj.nosurface", ("{0} has no solid surface", "{0} no tiene superficie sólida") },
            { "design.ok",          ("OK", "OK") },
            { "design.short",       ("TOO LOW", "INSUFICIENTE") },
            { "design.poweravail",  ("{0:F0} W available", "{0:F0} W efectivos") },
            { "design.powerused",   ("Load: {0:F0} W", "Consumo: {0:F0} W") },
            { "design.antennarate", ("{0} real · your {1:F0} Mb take {2:F0} s to arrive", "{0} reales · tus {1:F0} Mb tardan {2:F0} s en llegar") },
            { "design.capacity",    ("Capacity: {0:N0} kg", "Capacidad: {0:N0} kg") },
            { "design.instmass",    ("Mass: {0:F0} kg", "Masa: {0:F0} kg") },
            { "design.instpts",     ("{0:N0} science pts possible", "{0:N0} pts de ciencia posibles") },
            { "design.massfrac",    ("Mass fraction: {0:F2}", "Fracción de masa: {0:F2}") },
            { "err.budget",         ("Over budget", "Presupuesto excedido") },
            { "err.mass",           ("Too heavy for the launch vehicle", "Nave demasiado pesada para el lanzador") },
            { "err.power",          ("Power draw exceeds generation", "El consumo supera la generación eléctrica") },
            { "err.objective",      ("You can't land on a gas giant", "No se puede aterrizar en un gigante gaseoso") },
            { "err.deltav",         ("Not enough ΔV: more fuel or a better engine", "ΔV insuficiente: más combustible o mejor motor") },

            // ── Countdown / HUD ──────────────────────────────────────────────
            { "count.liftoff",      ("LIFT-OFF!", "¡DESPEGUE!") },
            { "count.sub",          ("Destination: {0}\nPoint your phone at the planet · Hold a finger to thrust", "Destino: {0}\nApunta el celular hacia el planeta · Mantén el dedo para acelerar") },
            { "hud.speed",          ("SPEED", "VELOCIDAD") },
            { "hud.fuel",           ("FUEL", "COMBUSTIBLE") },
            { "hud.hull",           ("HULL {0}/3", "CASCO {0}/3") },
            { "hud.signal",         ("Signal delay: {0}", "Retardo de señal: {0}") },
            { "hud.lowfuel",        ("LOW FUEL", "COMBUSTIBLE BAJO") },
            { "hud.nofuel",         ("OUT OF FUEL", "SIN COMBUSTIBLE") },
            { "hud.controls", ("Joystick or tilt the phone to steer · hold THRUST · Q/E or LB/RB: barrel roll", "Joystick o inclina el celular para girar · mantén ACELERAR · Q/E o LB/RB: tonel") },
            { "hud.hit.light",      ("BUMP", "ROCE") },
            { "hud.hit.medium",     ("IMPACT · FUEL LEAK −10%", "IMPACTO · FUGA DE COMBUSTIBLE −10%") },
            { "hud.hit.heavy",      ("HEAVY HIT · HULL −1 · FUEL −25%", "GOLPE FUERTE · CASCO −1 · COMBUSTIBLE −25%") },
            { "hud.hit.shielded",   ("SHIELD ABSORBED THE HIT", "EL ESCUDO ABSORBIÓ EL GOLPE") },
            { "hud.takedown",       ("TAKEDOWN!", "¡TAKEDOWN!") },
            { "hud.emp",            ("EMP! BURNOUT BLOCKED", "¡EMP! BURNOUT BLOQUEADO") },

            // ── Fail reasons ─────────────────────────────────────────────────
            { "fail.destroyed",     ("Ship destroyed", "Nave destruida") },
            { "fail.nofuel",        ("Out of fuel", "Sin combustible") },
            { "fail.timeout",       ("Time ran out", "Tiempo agotado") },
            { "fail.notarrived",    ("Did not arrive", "No llegó") },

            // ── Science ops ──────────────────────────────────────────────────
            { "sci.first",          ("#1 FIRST TO ARRIVE", "#1 PRIMERO EN LLEGAR") },
            { "sci.nth",            ("#{0} TO ARRIVE", "#{0} EN LLEGAR") },
            { "sci.objective",      ("{0} · science ×{1:0.#}", "{0} · ciencia ×{1:0.#}") },
            { "sci.signal",         ("Real signal: {0} one way · {1:F0} Mb to send", "Señal real: {0} de ida · {1:F0} Mb por enviar") },
            { "sci.downlink",       ("{0} antenna · {1:0.#} Mb/s · light {2:0.#} s", "Antena {0} · {1:0.#} Mb/s · luz {2:0.#} s") },
            { "sci.gravity",        ("Surface gravity {0:F2} m/s²", "Gravedad superficial {0:F2} m/s²") },
            { "sci.atmosphere",     ("Atmosphere: {0}", "Atmósfera: {0}") },
            { "sci.window",         ("Comm window {0}:{1:00}", "Ventana de comunicación {0}:{1:00}") },
            { "sci.windowclosed",   ("Window closed", "Ventana cerrada") },
            { "sci.power",          ("POWER", "ENERGÍA") },
            { "sci.waitpower",      ("WAITING FOR POWER", "ESPERANDO ENERGÍA") },
            { "sci.pipeline",       ("INSTRUMENTS: SCAN → SEND → EARTH", "INSTRUMENTOS: ESCANEO → ENVÍO → TIERRA") },
            { "sci.st.power",       ("Waiting for power", "Esperando energía") },
            { "sci.st.scan",        ("Scanning {0:P0}", "Escaneando {0:P0}") },
            { "sci.st.queued",      ("Queued to send", "En cola de envío") },
            { "sci.st.sending",     ("Sending {0:F0}/{1:F0} Mb", "Enviando {0:F0}/{1:F0} Mb") },
            { "sci.st.transit",     ("Travelling to Earth {0:F0} s", "Viajando a la Tierra {0:F0} s") },
            { "sci.st.received",    ("+{0:N0} pts", "+{0:N0} pts") },
            { "sci.st.lost",        ("Lost: window closed", "Perdido: sin ventana") },
            { "sci.received",       ("{0:N0} pts received", "{0:N0} pts recibidos") },
            { "sci.total",          ("TOTAL {0:N0}", "TOTAL {0:N0}") },
            { "sci.fuel",           ("Fuel {0:P0}", "Combustible {0:P0}") },
            { "sci.busy",           ("Operations in progress…", "Operaciones en curso…") },
            { "sci.complete",       ("COMPLETE MISSION", "COMPLETAR MISIÓN") },
            { "atm.mercury",        ("None", "Sin atmósfera") },
            { "atm.venus",          ("Dense CO2, 92 atm, 465 °C", "CO2 densa, 92 atm, 465 °C") },
            { "atm.mars",           ("Thin CO2, 0.006 atm", "CO2 delgada, 0.006 atm") },
            { "atm.jupiter",        ("H2 + He, no solid surface", "H2 + He, sin superficie sólida") },
            { "atm.saturn",         ("H2 + He, icy rings", "H2 + He, anillos de hielo") },
            { "atm.uranus",         ("H2 + He + CH4, ice giant", "H2 + He + CH4, gigante helado") },
            { "atm.neptune",        ("H2 + He + CH4, 2,100 km/h winds", "H2 + He + CH4, vientos de 2,100 km/h") },

            // ── Scoreboard ───────────────────────────────────────────────────
            { "sb.pos1",            ("#1 FIRST", "#1 PRIMERO") },
            { "sb.pos2",            ("#2 SECOND", "#2 SEGUNDO") },
            { "sb.pos3",            ("#3 THIRD", "#3 TERCERO") },
            { "sb.arrived",         ("Arrived", "Llegó") },
            { "sb.science",         ("{0:N0} science pts", "{0:N0} pts de ciencia") },
            { "sb.fuel",            ("Fuel: {0:P0}", "Combustible: {0:P0}") },
            { "sb.takedowns",       ("{0} takedowns", "{0} takedowns") },
            { "sb.waiting",         ("Waiting for rival…", "Esperando al rival…") },
            { "sb.rivalflying",     ("Your rival is still on their mission", "Tu rival sigue en misión") },
            { "sb.rivaldone",       ("Mission finished", "Misión terminada") },
            { "sb.win",             ("VICTORY!", "¡VICTORIA!") },
            { "sb.draw",            ("DRAW", "EMPATE") },
            { "sb.lose",            ("DEFEAT", "DERROTA") },
            { "sb.win.sub",         ("Mission accomplished!", "¡Misión completada con éxito!") },
            { "sb.draw.sub",        ("Neck and neck!", "¡Igualados al milímetro!") },
            { "sb.lose.sub",        ("Better luck next time, cadet.", "Mejor suerte la próxima, cadete.") },
            { "sb.failed",          ("MISSION FAILED", "MISIÓN FALLIDA") },
            { "sb.success",         ("MISSION ACCOMPLISHED", "MISIÓN CUMPLIDA") },
            { "sb.failed.sub",      ("{0}. Check your ΔV and fuel.", "{0}. Revisa tu ΔV y combustible.") },
            { "sb.success.sub",     ("{0:N0} points for NASA", "{0:N0} puntos para la NASA") },
            { "sb.again",           ("IMPROVE DESIGN AND RETRY", "MEJORAR DISEÑO Y REPETIR") },
            { "sb.menu",            ("Main menu", "Menú principal") },

            // ── Ranking / campaign ───────────────────────────────────────────
            { "sb.ranking", ("RANKING", "CLASIFICACIÓN") },
            { "sb.place",   ("You finished #{0} of {1}.", "Terminaste #{0} de {1}.") },
            { "sb.posn",    ("#{0}", "#{0}") },
            { "sb.next",    ("NEXT MISSION", "SIGUIENTE MISIÓN") },
            { "camp.button",   ("CAMPAIGN · NASA PROGRAM", "CAMPAÑA · PROGRAMA NASA") },
            { "camp.title",    ("NASA PROGRAM", "PROGRAMA NASA") },
            { "camp.subtitle", ("Six missions from Mars to Neptune. Earn at least one star to unlock the next.", "Seis misiones de Marte a Neptuno. Gana al menos una estrella para desbloquear la siguiente.") },
            { "camp.back",     ("BACK", "VOLVER") },
            { "camp.total",    ("{0} / {1} stars", "{0} / {1} estrellas") },
            { "camp.goal1",    ("goal {0:N0} pts", "meta {0:N0} pts") },
            { "camp.locked",   ("Locked: earn a star in mission {0}", "Bloqueada: gana una estrella en la misión {0}") },
            { "camp.brief",    ("MISSION {0} · {1}\n{2} Budget ${3:N0}M · goal {4:N0} pts", "MISIÓN {0} · {1}\n{2} Presupuesto ${3:N0}M · meta {4:N0} pts") },
            { "camp.missionN", ("MISSION {0} · {1}", "MISIÓN {0} · {1}") },
            { "camp.goals",    ("Goals: ● {0:N0} · ●● {1:N0} · ●●● {2:N0} pts", "Metas: ● {0:N0} · ●● {1:N0} · ●●● {2:N0} pts") },
            { "camp.failedgoal", ("Goal not reached: improve the design and retry.", "No alcanzaste la meta: mejora el diseño y reintenta.") },
            { "camp.newbest",  ("New best!", "¡Nuevo récord!") },
            { "camp.unlocked", ("Next mission unlocked.", "Siguiente misión desbloqueada.") },
            { "camp.firstlight.name",  ("First Light", "Primera luz") },
            { "camp.firstlight.brief", ("Fly past Mars on a tight budget. Pick instruments that matter at Mars.", "Sobrevuela Marte con poco presupuesto. Elige instrumentos que valgan en Marte.") },
            { "camp.venus.name",       ("Beneath the Clouds", "Bajo las nubes") },
            { "camp.venus.brief",      ("Orbit Venus and map a surface no camera can see.", "Orbita Venus y mapea una superficie que ninguna cámara puede ver.") },
            { "camp.rover.name",       ("Red Planet Touchdown", "Aterrizaje en el planeta rojo") },
            { "camp.rover.brief",      ("Land on Mars: you need ΔV for cruise, orbit insertion and descent.", "Aterriza en Marte: necesitas ΔV para el viaje, la inserción orbital y el descenso.") },
            { "camp.jupiter.name",     ("King of Planets", "El rey de los planetas") },
            { "camp.jupiter.brief",    ("Orbit Jupiter like Juno. Sunlight is weak out here.", "Orbita Júpiter como Juno. Aquí la luz del Sol es débil.") },
            { "camp.saturn.name",      ("Lord of the Rings", "El señor de los anillos") },
            { "camp.saturn.brief",     ("Orbit Saturn like Cassini. Radio science through the rings is gold.", "Orbita Saturno como Cassini. La radio ciencia a través de los anillos vale oro.") },
            { "camp.neptune.name",     ("The Last Giant", "El último gigante") },
            { "camp.neptune.brief",    ("Fly past Neptune like Voyager 2. Only an efficient engine gets there.", "Sobrevuela Neptuno como Voyager 2. Solo un motor eficiente llega.") },

            // ── Mission report ───────────────────────────────────────────────
            { "rep.title",          ("MISSION REPORT", "INFORME DE MISIÓN") },
            { "rep.header",         ("{0} to {1} · closest real mission: {2}", "{0} a {1} · misión real parecida: {2}") },
            { "rep.prop.nofuel",    ("Propulsion: you ran out of fuel. ΔV {0:N0} of {1:N0} m/s required: more fuel or a higher-Isp engine (ion) gives more thrust time.", "Propulsión: te quedaste sin combustible. ΔV {0:N0} de {1:N0} m/s requeridos: más combustible o un motor de mayor Isp (iónico) da más tiempo de empuje.") },
            { "rep.prop.tight",     ("Propulsion: tight ΔV ({0:P0} margin). You arrived with {1:P0} fuel; a bigger margin allows more boost and manoeuvres.", "Propulsión: ΔV justo ({0:P0} de margen). Llegaste con {1:P0} de combustible; un margen mayor permite más boost y maniobras.") },
            { "rep.prop.good",      ("Propulsion: ΔV {0:N0} m/s, {1:P0} above required. You arrived with {2:P0} fuel (+{3} pts).", "Propulsión: ΔV {0:N0} m/s, {1:P0} sobre lo requerido. Llegaste con {2:P0} de combustible (+{3} pts).") },
            { "rep.obj.paid",       ("Objective: {0} cost +{1:N0} m/s{2} and multiplied science ×{3:0.#}.", "Objetivo: {0} costó +{1:N0} m/s{2} y multiplicó la ciencia ×{3:0.#}.") },
            { "rep.obj.lander",     (" and a {0:F0} kg lander", " y {0:F0} kg de módulo") },
            { "rep.obj.flyby",      ("Objective: a flyby is cheap, but an orbit would give ×1.5 science (+{0:N0} m/s).", "Objetivo: un sobrevuelo es barato, pero una órbita daría ×1.5 de ciencia (+{0:N0} m/s).") },
            { "rep.fail",           ("Flight: {0} after {1:F0} s. No arrival means no science and no fuel bonus.", "Vuelo: {0} a los {1:F0} s. Sin llegada no hay ciencia ni bono de combustible.") },
            { "rep.power.wait",     ("Power: {0:F0} W could not run everything at once; instruments waited {1:F0} s.", "Energía: {0:F0} W no alcanzaron para operar todo a la vez; los instrumentos esperaron {1:F0} s.") },
            { "rep.power.solar",    (" This far from the Sun an RTG performs better.", " A esta distancia del Sol un RTG rinde más.") },
            { "rep.power.good",     ("Power: {0:F0} W available, {1:F0} W peak load. No waiting.", "Energía: {0:F0} W disponibles, {1:F0} W de consumo máximo. Sin esperas.") },
            { "rep.sci.none",       ("Science: you carried no instruments. Every kilo of instrument is points.", "Ciencia: no llevaste instrumentos. Cada kilo de instrumento son puntos.") },
            { "rep.comm.lost",      ("Communications: {0} of {1} instruments reached Earth; {2:N0} pts were lost because the antenna could not send {3:F0} Mb in time. A higher-gain antenna would have saved them.", "Comunicaciones: llegaron {0} de {1} instrumentos; se perdieron {2:N0} pts porque la antena no envió {3:F0} Mb a tiempo. Una antena de mayor ganancia los habría salvado.") },
            { "rep.comm.good",      ("Communications: all {0} instruments sent {1:F0} Mb to Earth ({2:N0} pts).", "Comunicaciones: los {0} instrumentos enviaron {1:F0} Mb a la Tierra ({2:N0} pts).") },
            { "rep.budget",         ("Resources: ${0:N0}M of ${3:N0}M and {1:N0} of {2:N0} kg of the launcher.", "Recursos: ${0:N0}M de ${3:N0}M y {1:N0} de {2:N0} kg del lanzador.") },
            { "rep.budget.spare",   (" ${0:N0}M left over: you could have carried more instruments.", " Sobraron ${0:N0}M: cabían más instrumentos.") },
            // ── Science value per planet ─────────────────────────────────────
            { "scihint.mercury", ("Mercury: its surprising magnetic field (×2 magnetometer) and polar ice (radar, infrared) are the prize.", "Mercurio: su sorprendente campo magnético (magnetómetro ×2) y el hielo polar (radar, infrarrojo) son el premio.") },
            { "scihint.venus",   ("Venus: clouds hide the surface. Radar sees through them (×2) and UV reveals the clouds; cameras see almost nothing (×0.3).", "Venus: las nubes tapan la superficie. El radar las atraviesa (×2) y el UV revela las nubes; las cámaras casi no ven (×0.3).") },
            { "scihint.mars",    ("Mars: high-resolution cameras and radar hunting buried ice are worth ×2, like MRO's HiRISE and SHARAD.", "Marte: la cámara de alta resolución y el radar que busca hielo enterrado valen ×2, como HiRISE y SHARAD de MRO.") },
            { "scihint.jupiter", ("Jupiter: huge magnetic field and radiation belts. Magnetometer and particle detector ×2, like Juno. No surface for radar.", "Júpiter: enorme campo magnético y cinturones de radiación. Magnetómetro y detector de partículas ×2, como Juno. Sin superficie para el radar.") },
            { "scihint.saturn",  ("Saturn: radio science through the rings is worth ×2, like Cassini's occultations. Cameras shine on the rings.", "Saturno: la radio ciencia a través de los anillos vale ×2, como las ocultaciones de Cassini. Las cámaras brillan con los anillos.") },
            { "scihint.uranus",  ("Uranus: its tipped, off-centre magnetic field is a mystery (magnetometer ×2). No surface for radar.", "Urano: su campo magnético inclinado y descentrado es un misterio (magnetómetro ×2). Sin superficie para el radar.") },
            { "scihint.neptune", ("Neptune: magnetometer ×2 and cameras ×1.5 for Triton's geysers, as Voyager 2 saw. No surface for radar.", "Neptuno: magnetómetro ×2 y cámaras ×1.5 para los géiseres de Tritón, como vio Voyager 2. Sin superficie para el radar.") },
            { "sci.st.failed",   ("Failed during cruise", "Falló en el crucero") },
            { "rep.sci.missed",  ("Science strategy: at {0} the {1} is worth ×{2:0.#}; it would have added {3:N0} pts.", "Estrategia científica: en {0} el {1} vale ×{2:0.#}; habría sumado {3:N0} pts.") },
            { "rep.ops",         ("Operations · {0}", "Operaciones · {0}") },
            { "rep.ops.bonus",   ("Cruise science: +{0:N0} pts from your decisions en route.", "Ciencia en crucero: +{0:N0} pts por tus decisiones en el viaje.") },

            // ── Cruise operations events ─────────────────────────────────────
            { "hud.gyro.on",  ("GYRO ON", "GIRO ON") },
            { "hud.gyro.off", ("GYRO OFF", "GIRO OFF") },
            { "hud.thrust",   ("THRUST", "ACELERAR") },
            { "hud.boost",    ("BOOST", "BOOST") },
            { "hud.safemode", ("SAFE MODE · ENGINES OFF {0:0} s", "MODO SEGURO · MOTORES APAGADOS {0:0} s") },
            { "pod.Fuel",          ("+15% FUEL", "+15% COMBUSTIBLE") },
            { "pod.Boost",         ("BURNOUT BAR FULL!", "¡BARRA DE BURNOUT LLENA!") },
            { "pod.Shield",        ("SHIELD · 10 s", "ESCUDO · 10 s") },
            { "pod.EMP",           ("EMP SENT TO RIVALS", "EMP ENVIADO A LOS RIVALES") },
            { "pod.GravityAssist", ("GRAVITY ASSIST · +200 pts", "ASISTENCIA GRAVITATORIA · +200 pts") },
            { "ev.header", ("MISSION CONTROL · ANOMALY", "CONTROL DE MISIÓN · ANOMALÍA") },
            { "tut.tag",   ("MISSION BRIEFING", "BRIEFING DE MISIÓN") },
            { "ev.timer", ("{0} s to decide", "{0} s para decidir") },
            { "ev.ok",     ("CONTINUE", "CONTINUAR") },
            { "ev.lucky",  ("Luck was on your side: no damage.", "La suerte estuvo de tu lado: sin daños.") },
            { "ev.lost",   ("The {0} was lost.", "Se perdió el {0}.") },

            { "ev.SolarStorm.title", ("SOLAR STORM", "TORMENTA SOLAR") },
            { "ev.SolarStorm.body",  ("A coronal mass ejection is heading your way. Its charged particles can fry electronics.", "Una eyección de masa coronal viene hacia ti. Sus partículas cargadas pueden quemar la electrónica.") },
            { "ev.SolarStorm.payoff",("Your particle detector could measure the storm: rare, valuable science.", "Tu detector de partículas podría medir la tormenta: ciencia rara y valiosa.") },
            { "ev.SolarStorm.a", ("SAFE MODE\n<size=75%>Engines off 5 s, instruments protected</size>", "MODO SEGURO\n<size=75%>Motores apagados 5 s, instrumentos protegidos</size>") },
            { "ev.SolarStorm.b",     ("KEEP OPERATING\n<size=75%>Risk losing an instrument</size>", "SEGUIR OPERANDO\n<size=75%>Riesgo de perder un instrumento</size>") },
            { "ev.SolarStorm.ra", ("safe mode: engines off 5 s, every instrument protected.", "modo seguro: motores apagados 5 s, todos los instrumentos protegidos.") },
            { "ev.SolarStorm.rbonus",("your particle detector measured the storm: +{0:N0} pts.", "tu detector de partículas midió la tormenta: +{0:N0} pts.") },

            { "ev.Debris.title", ("DEBRIS FIELD", "CAMPO DE ESCOMBROS") },
            { "ev.Debris.body",  ("Radar shows micrometeoroids crossing your path.", "El radar muestra micrometeoritos cruzando tu ruta.") },
            { "ev.Debris.a",     ("DODGE BURN\n<size=75%>−8% fuel, no risk</size>", "MANIOBRA EVASIVA\n<size=75%>−8% combustible, sin riesgo</size>") },
            { "ev.Debris.b",     ("FLY THROUGH\n<size=75%>60% chance: −1 hull, −25% fuel</size>", "ATRAVESAR\n<size=75%>60% de probabilidad: −1 casco, −25% combustible</size>") },
            { "ev.Debris.ra",    ("dodge burn, spent 8% fuel.", "maniobra evasiva, gastaste 8% de combustible.") },
            { "ev.Debris.rhit",  ("hit by debris: −1 hull, −25% fuel.", "impacto de escombros: −1 casco, −25% combustible.") },

            { "ev.InstrumentGlitch.title", ("INSTRUMENT FAULT", "FALLA DE INSTRUMENTO") },
            { "ev.InstrumentGlitch.body",  ("The {0} stopped responding. A reboot needs 20 W of spare power; you have {1:F0} W to spare.", "El {0} dejó de responder. Reiniciarlo necesita 20 W libres; tienes {1:F0} W de margen.") },
            { "ev.InstrumentGlitch.a",     ("REBOOT\n<size=75%>Needs 20 W spare power</size>", "REINICIAR\n<size=75%>Necesita 20 W libres</size>") },
            { "ev.InstrumentGlitch.b",     ("SWITCH IT OFF\n<size=75%>Lose this instrument</size>", "APAGARLO\n<size=75%>Pierdes este instrumento</size>") },
            { "ev.InstrumentGlitch.rfixed",("{0} rebooted thanks to your power margin.", "{0} reiniciado gracias a tu margen de energía.") },
            { "ev.InstrumentGlitch.rnopower",("not enough spare power: the {0} is lost.", "sin energía de sobra: se perdió el {0}.") },
            { "ev.InstrumentGlitch.roff",  ("the {0} was switched off for good.", "el {0} quedó apagado para siempre.") },

            { "ev.NavError.title", ("NAVIGATION ERROR", "ERROR DE NAVEGACIÓN") },
            { "ev.NavError.body",  ("Tracking shows you're drifting off course. Every real mission does trajectory correction manoeuvres (TCMs).", "El seguimiento muestra que te desvías del rumbo. Toda misión real hace maniobras de corrección de trayectoria (TCM).") },
            { "ev.NavError.a",     ("CORRECT NOW\n<size=75%>−5% fuel</size>", "CORREGIR AHORA\n<size=75%>−5% combustible</size>") },
            { "ev.NavError.b",     ("CORRECT LATER\n<size=75%>−15% fuel: errors grow with distance</size>", "CORREGIR DESPUÉS\n<size=75%>−15% combustible: el error crece con la distancia</size>") },
            { "ev.NavError.ra",    ("early correction, only 5% fuel.", "corrección temprana, solo 5% de combustible.") },
            { "ev.NavError.rb",    ("late correction cost 15% fuel: small early burns are cheaper.", "la corrección tardía costó 15% de combustible: corregir temprano es más barato.") },

            { "ev.CommBlackout.title", ("COMMUNICATIONS BLACKOUT", "APAGÓN DE COMUNICACIONES") },
            { "ev.CommBlackout.body",  ("The Sun sits between you and Earth (solar conjunction). The Deep Space Network can't hear you.", "El Sol quedó entre tú y la Tierra (conjunción solar). La Red de Espacio Profundo no te escucha.") },
            { "ev.CommBlackout.payoff",("Your high-gain antenna keeps a thin link alive.", "Tu antena de alta ganancia mantiene un enlace débil.") },
            { "ev.CommBlackout.a", ("WAIT FOR THE LINK\n<size=75%>Engines off 5 s</size>", "ESPERAR EL ENLACE\n<size=75%>Motores apagados 5 s</size>") },
            { "ev.CommBlackout.b",     ("FLY ON AUTOPILOT\n<size=75%>40% chance of a navigation mishap</size>", "SEGUIR EN PILOTO AUTOMÁTICO\n<size=75%>40% de probabilidad de un percance</size>") },
            { "ev.CommBlackout.rhigh", ("the high-gain antenna kept the link. No delay.", "la antena de alta ganancia mantuvo el enlace. Sin demora.") },
            { "ev.CommBlackout.ra", ("waited for the link: engines off 5 s.", "esperaste el enlace: motores apagados 5 s.") },
            { "ev.CommBlackout.rhit",  ("the autopilot clipped something: −10% fuel.", "el piloto automático rozó algo: −10% de combustible.") },

            { "ev.SolarFade.title", ("SOLAR PANELS FADING", "PANELES SOLARES DEBILITÁNDOSE") },
            { "ev.SolarFade.body",  ("Far from the Sun your panels give less and less power. Something has to give.", "Lejos del Sol tus paneles dan cada vez menos energía. Algo hay que sacrificar.") },
            { "ev.SolarFade.a",     ("SHED LOAD\n<size=75%>Switch off your most power-hungry instrument</size>", "REDUCIR CARGA\n<size=75%>Apagar tu instrumento que más consume</size>") },
            { "ev.SolarFade.b",     ("RUN ON RESERVES\n<size=75%>−30 W available for science</size>", "USAR RESERVAS\n<size=75%>−30 W disponibles para ciencia</size>") },
            { "ev.SolarFade.ra",    ("switched off the {0} to save power.", "apagaste el {0} para ahorrar energía.") },
            { "ev.SolarFade.rb",    ("running on reserves: 30 W less for science.", "usando reservas: 30 W menos para ciencia.") },

            // ── First-mission tutorial ───────────────────────────────────────
            { "tut.next",  ("NEXT", "SIGUIENTE") },
            { "tut.back",  ("BACK", "ATRÁS") },
            { "tut.skip",  ("SKIP", "SALTAR") },
            { "tut.start", ("DESIGN MY MISSION", "DISEÑAR MI MISIÓN") },
            { "tut.1.title", ("You are the mission designer", "Eres el diseñador de la misión") },
            { "tut.1.body",  ("NASA gives you $1,000 million to send a probe to another planet and bring back as much science as possible.\n\nEvery choice has a cost in money, mass or power. There is no perfect design: only trade-offs.", "La NASA te da $1,000 millones para enviar una sonda a otro planeta y traer la mayor cantidad de ciencia posible.\n\nCada decisión cuesta dinero, masa o energía. No existe el diseño perfecto: solo compromisos.") },
            { "tut.2.title", ("ΔV: the currency of space", "ΔV: la moneda del espacio") },
            { "tut.2.body",  ("ΔV (delta-V) is how much your probe can change its speed. Each planet needs a minimum ΔV to reach it.\n\nTsiolkovsky's rocket equation:\n<b>ΔV = Isp × g0 × ln(wet mass / dry mass)</b>\n\nMore fuel gives more ΔV… but also more mass to push.", "ΔV (delta-V) es cuánto puede cambiar su velocidad tu sonda. Cada planeta exige un ΔV mínimo.\n\nEcuación de Tsiolkovsky:\n<b>ΔV = Isp × g0 × ln(masa húmeda / masa seca)</b>\n\nMás combustible da más ΔV… pero también más masa que empujar.") },
            { "tut.3.title", ("Engines: thrust vs efficiency", "Motores: empuje contra eficiencia") },
            { "tut.3.body",  ("<b>Chemical</b>: strong and cheap, but inefficient (Isp 450 s).\n<b>Ion</b>: 6× more efficient (Isp 3,000 s) but weak and slow, like NASA's Dawn.\n<b>Nuclear</b>: in between, powerful but heavy and expensive.\n\nThe outer planets are only reachable with an efficient engine.", "<b>Químico</b>: fuerte y barato, pero poco eficiente (Isp 450 s).\n<b>Iónico</b>: 6× más eficiente (Isp 3,000 s) pero débil y lento, como Dawn de la NASA.\n<b>Nuclear</b>: intermedio, potente pero pesado y caro.\n\nLos planetas exteriores solo se alcanzan con un motor eficiente.") },
            { "tut.4.title", ("Power: the Sun fades with distance", "Energía: el Sol se apaga con la distancia") },
            { "tut.4.body",  ("Solar power falls with the square of the distance: at Jupiter you get 1/27 of what you get at Earth.\n\nThat's why Voyager and Cassini used <b>RTGs</b> (nuclear batteries). Your instruments can only run if there's power left for them.", "La energía solar cae con el cuadrado de la distancia: en Júpiter recibes 1/27 de lo que recibes en la Tierra.\n\nPor eso Voyager y Cassini usaron <b>RTG</b> (baterías nucleares). Tus instrumentos solo funcionan si queda energía para ellos.") },
            { "tut.5.title", ("Science depends on the planet", "La ciencia depende del planeta") },
            { "tut.5.body",  ("Each instrument is worth more at some planets. At Venus, cameras see only clouds but radar sees the surface.\n\nLook for the <color=#52EB8C>×2</color> next to each instrument: that is the right tool for that world.", "Cada instrumento vale más en ciertos planetas. En Venus las cámaras solo ven nubes, pero el radar ve la superficie.\n\nBusca el <color=#52EB8C>×2</color> junto a cada instrumento: es la herramienta correcta para ese mundo.") },
            { "tut.6.title", ("Data must reach Earth", "Los datos deben llegar a la Tierra") },
            { "tut.6.body",  ("Science only counts when it arrives on Earth. Your antenna sets how fast you send data, and light takes minutes or hours to cross the solar system.\n\nA cheap antenna with many instruments means lost data.", "La ciencia solo cuenta cuando llega a la Tierra. Tu antena define qué tan rápido envías datos, y la luz tarda minutos u horas en cruzar el sistema solar.\n\nUna antena barata con muchos instrumentos significa datos perdidos.") },
            { "tut.7.title", ("Mission objective", "Objetivo de la misión") },
            { "tut.7.body",  ("<b>Flyby</b>: cheap, ×1 science.\n<b>Orbit</b>: extra ΔV to brake, ×1.5 science.\n<b>Landing</b>: even more ΔV plus a 250 kg lander, ×2 science.\n\nWhen the design is valid, LAUNCH MISSION turns on.", "<b>Sobrevuelo</b>: barato, ciencia ×1.\n<b>Órbita</b>: ΔV extra para frenar, ciencia ×1.5.\n<b>Aterrizaje</b>: aún más ΔV y un módulo de 250 kg, ciencia ×2.\n\nCuando el diseño es válido, se enciende LANZAR MISIÓN.") },
            { "tut.8.title", ("Fly and operate", "Vuela y opera") },
            { "tut.8.body",  ("Point your phone at the planet and hold a finger on the screen to thrust. Two fingers = boost.\n\nDuring the cruise, mission control will face real problems: solar storms, faults, blackouts. Your design decides how bad they get.\n\nAt the end, a report explains how each choice shaped your result.", "Apunta el celular al planeta y mantén un dedo en la pantalla para acelerar. Dos dedos = boost.\n\nDurante el crucero, control de misión enfrentará problemas reales: tormentas solares, fallas, apagones. Tu diseño decide qué tan graves son.\n\nAl final, un informe explica cómo cada decisión moldeó tu resultado.") },

            { "rep.race",           ("Race: {0} takedown(s), hull {1}/3, {2:F0} s flight.", "Carrera: {0} takedown(s), casco {1}/3, vuelo de {2:F0} s.") },
        };
    }
}
