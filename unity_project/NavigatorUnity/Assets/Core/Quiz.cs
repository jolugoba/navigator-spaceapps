// Navigator 2.0 — Quiz.cs
// Classroom question bank: two questions per world, about the engineering idea that world teaches
// (ΔV, orbit insertion, gravity assists, solar power, light delay, efficient engines…).
// The teacher launches them between missions; answers decide who moves on.
// Guacalita Inc. — SAL / Claude

namespace Navigator.Core
{
    public class QuizQuestion
    {
        public int World;                         // campaign index it belongs to
        public (string en, string es) Text;
        public (string en, string es)[] Options;  // four
        public int Correct;

        public string Q            => Loc.Current == Lang.Spanish ? Text.es : Text.en;
        public string Option(int i) => i >= 0 && i < Options.Length ? (Loc.Current == Lang.Spanish ? Options[i].es : Options[i].en) : "";
    }

    public static class Quiz
    {
        public static readonly QuizQuestion[] Bank =
        {
            // 1 · First Light (Mars flyby)
            new() { World = 0, Correct = 0,
                Text = ("What does ΔV (delta-v) measure?", "¿Qué mide el ΔV (delta-v)?"),
                Options = new[] {
                    ("How much the ship can change its speed", "Cuánto puede cambiar la nave su velocidad"),
                    ("The ship's mass", "La masa de la nave"),
                    ("The price of the fuel", "El precio del combustible"),
                    ("The distance to the planet", "La distancia al planeta") } },
            new() { World = 0, Correct = 2,
                Text = ("Why can't you carry every instrument on a small budget?", "¿Por qué no puedes llevar todos los instrumentos con poco presupuesto?"),
                Options = new[] {
                    ("They don't fit in the rocket", "No caben en el cohete"),
                    ("NASA forbids it", "La NASA lo prohíbe"),
                    ("Each one adds cost, mass and power use", "Cada uno suma costo, masa y consumo de energía"),
                    ("Instruments use no power", "Los instrumentos no usan energía") } },

            // 2 · Venus orbit
            new() { World = 1, Correct = 1,
                Text = ("Why does mapping Venus need radar?", "¿Por qué mapear Venus necesita radar?"),
                Options = new[] {
                    ("It is too far away", "Está demasiado lejos"),
                    ("Its thick clouds hide the surface from cameras", "Sus nubes espesas ocultan la superficie a las cámaras"),
                    ("It has no surface", "No tiene superficie"),
                    ("It is always night there", "Allí siempre es de noche") } },
            new() { World = 1, Correct = 3,
                Text = ("What does an orbit need that a flyby doesn't?", "¿Qué necesita una órbita que un sobrevuelo no?"),
                Options = new[] {
                    ("Nothing extra", "Nada extra"),
                    ("Landing legs", "Patas de aterrizaje"),
                    ("A bigger antenna", "Una antena más grande"),
                    ("An insertion burn to slow down and be captured", "Un encendido de inserción para frenar y quedar capturado") } },

            // 3 · Mercury orbit
            new() { World = 2, Correct = 0,
                Text = ("Which planet gave MESSENGER a free gravity assist on its way to Mercury?", "¿Qué planeta le dio a MESSENGER una asistencia gravitatoria gratis rumbo a Mercurio?"),
                Options = new[] { ("Venus", "Venus"), ("Mars", "Marte"), ("Jupiter", "Júpiter"), ("The Moon", "La Luna") } },
            new() { World = 2, Correct = 2,
                Text = ("Close to the Sun, solar panels…", "Cerca del Sol, los paneles solares…"),
                Options = new[] {
                    ("stop working", "dejan de funcionar"),
                    ("must be much bigger", "deben ser mucho más grandes"),
                    ("make plenty of power but must survive extreme heat", "dan mucha energía pero deben resistir un calor extremo"),
                    ("freeze", "se congelan") } },

            // 4 · Mars landing
            new() { World = 3, Correct = 1,
                Text = ("Why does a landing need more ΔV than a flyby?", "¿Por qué un aterrizaje necesita más ΔV que un sobrevuelo?"),
                Options = new[] {
                    ("The lander is lighter", "El módulo es más liviano"),
                    ("You must brake into orbit and then brake again to descend", "Hay que frenar para entrar en órbita y frenar otra vez para descender"),
                    ("Mars has no gravity", "Marte no tiene gravedad"),
                    ("Mars is closer", "Marte está más cerca") } },
            new() { World = 3, Correct = 0,
                Text = ("Besides rockets, what slows landers down at Mars?", "Además de los cohetes, ¿qué frena a los módulos en Marte?"),
                Options = new[] {
                    ("Parachutes in its thin atmosphere", "Paracaídas en su atmósfera delgada"),
                    ("Ocean water", "El agua de un océano"),
                    ("Magnets", "Imanes"),
                    ("Nothing at all", "Nada") } },

            // 5 · Jupiter orbit
            new() { World = 4, Correct = 3,
                Text = ("Why is solar power weak at Jupiter?", "¿Por qué la energía solar es débil en Júpiter?"),
                Options = new[] {
                    ("Its clouds block the Sun", "Sus nubes tapan el Sol"),
                    ("Radiation melts the panels", "La radiación derrite los paneles"),
                    ("There is no sunlight there", "Allí no llega luz del Sol"),
                    ("Sunlight is about 27 times weaker than at Earth", "La luz del Sol es unas 27 veces más débil que en la Tierra") } },
            new() { World = 4, Correct = 1,
                Text = ("Which NASA spacecraft orbits Jupiter today?", "¿Qué nave de la NASA orbita Júpiter hoy?"),
                Options = new[] { ("Cassini", "Cassini"), ("Juno", "Juno"), ("Voyager 1", "Voyager 1"), ("Hubble", "Hubble") } },

            // 6 · Saturn orbit
            new() { World = 5, Correct = 0,
                Text = ("What is a gravity assist?", "¿Qué es una asistencia gravitatoria?"),
                Options = new[] {
                    ("Using a planet's gravity to gain speed without fuel", "Usar la gravedad de un planeta para ganar velocidad sin combustible"),
                    ("Refuelling at a planet", "Recargar combustible en un planeta"),
                    ("Landing on a moon", "Aterrizar en una luna"),
                    ("A type of engine", "Un tipo de motor") } },
            new() { World = 5, Correct = 2,
                Text = ("Saturn's rings are made of…", "Los anillos de Saturno están hechos de…"),
                Options = new[] { ("gas", "gas"), ("metal plates", "placas de metal"), ("ice and rock", "hielo y roca"), ("light", "luz") } },

            // 7 · Uranus flyby
            new() { World = 6, Correct = 1,
                Text = ("Why do messages from Uranus take hours to reach Earth?", "¿Por qué los mensajes desde Urano tardan horas en llegar a la Tierra?"),
                Options = new[] {
                    ("The antenna is slow", "La antena es lenta"),
                    ("Radio travels at the speed of light and it is billions of km away", "La radio viaja a la velocidad de la luz y está a miles de millones de km"),
                    ("Clouds block the signal", "Las nubes bloquean la señal"),
                    ("The Sun is in the way", "El Sol está en medio") } },
            new() { World = 6, Correct = 3,
                Text = ("What is unusual about Uranus?", "¿Qué tiene de raro Urano?"),
                Options = new[] {
                    ("It has no gravity", "No tiene gravedad"),
                    ("It is the hottest planet", "Es el planeta más caliente"),
                    ("It is made of solid rock", "Es de roca sólida"),
                    ("It spins tipped over on its side", "Gira acostado de lado") } },

            // 8 · Neptune flyby
            new() { World = 7, Correct = 0,
                Text = ("Which engine suits the long trip to Neptune best?", "¿Qué motor conviene más para el largo viaje a Neptuno?"),
                Options = new[] {
                    ("An efficient one (high Isp) that saves propellant", "Uno eficiente (alto Isp) que ahorra propelente"),
                    ("The heaviest one", "El más pesado"),
                    ("No engine at all", "Ningún motor"),
                    ("Just a bigger tank, the engine doesn't matter", "Solo un tanque más grande, el motor no importa") } },
            new() { World = 7, Correct = 2,
                Text = ("Which spacecraft has visited Neptune?", "¿Qué nave ha visitado Neptuno?"),
                Options = new[] { ("Juno", "Juno"), ("Curiosity", "Curiosity"), ("Voyager 2", "Voyager 2"), ("Cassini", "Cassini") } },
        };

        // Questions for one world, in order
        public static System.Collections.Generic.List<int> ForWorld(int world)
        {
            var list = new System.Collections.Generic.List<int>();
            for (int i = 0; i < Bank.Length; i++) if (Bank[i].World == world) list.Add(i);
            return list;
        }
    }
}
