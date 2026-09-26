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
            { "lobby.players",      ("{0} players connected: ready for lift-off!", "{0} jugadores conectados: ¡listos para despegar!") },

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
            { "hud.controls",       ("1 finger: thrust · 2 fingers: boost · aim your phone to steer", "1 dedo: acelerar · 2 dedos: boost · apunta el celular para girar") },
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
            { "rep.budget",         ("Resources: ${0:N0}M of $1,000M and {1:N0} of {2:N0} kg of the launcher.", "Recursos: ${0:N0}M de $1,000M y {1:N0} de {2:N0} kg del lanzador.") },
            { "rep.budget.spare",   (" ${0:N0}M left over: you could have carried more instruments.", " Sobraron ${0:N0}M: cabían más instrumentos.") },
            { "rep.race",           ("Race: {0} takedown(s), hull {1}/3, {2:F0} s flight.", "Carrera: {0} takedown(s), casco {1}/3, vuelo de {2:F0} s.") },
        };
    }
}
