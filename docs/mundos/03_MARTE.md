# Mundo 3 — MARTE · "Los 7 minutos de terror"

> Aterrizas en el cráter Jezero como Perseverance, manejas el rover, taladras 3 rocas y dejas las muestras en el depósito para que una misión futura las traiga a la Tierra.

## 1. Ficha real

| Dato | Valor real |
|---|---|
| Gravedad | 3,71 m/s² (0,38 g): saltas **2,6 veces** más alto que en la Tierra |
| Atmósfera | 0,006 bar (menos del 1 % de la Tierra), 95 % CO₂ |
| Temperatura | −63 °C promedio (de −125 °C a +20 °C) |
| Día | 24 h 37 min ("sol") |
| Luz del Sol | 43 % de la de la Tierra |
| Lunas | Fobos y Deimos |
| Misión real | **Perseverance** (2021) + helicóptero Ingenuity (72 vuelos). Curiosity (2012) |

**Por qué es difícil aterrizar:** el aire es muy delgado. El escudo de calor y el paracaídas frenan, pero no lo suficiente, así que al final hacen falta cohetes. Perseverance entró a 5,4 km/s y en 7 minutos tuvo que quedar quieto en el suelo. La señal tarda más de 7 minutos en llegar a la Tierra, así que todo pasa **solo, sin ayuda**.

## 2. Cómo se ve

| Elemento | Detalle |
|---|---|
| Cielo de día | Caramelo polvoriento `#C9A27E`; atardecer **azul** `#6E8FB5` (al revés que en la Tierra) |
| Suelo | Óxido rojo `#B5532D`, polvo claro `#D08A5A`, rocas oscuras `#5A3A2A` |
| Terreno | Borde del cráter Jezero al fondo, **delta de un río antiguo** con capas, dunas con ondas, rocas sueltas, parches de arena blanda |
| Luz | Cálida y un poco débil; el Sol se ve más chico |
| Partículas | Polvo de las ruedas que tarda en caer (poca gravedad); remolinos de polvo; **tormenta de polvo** como evento |
| En el cielo | Fobos cruzando rápido (se reutiliza `MN-01_Phobos`) y el orbitador MRO (`OB-13_MRO`) como relé de comunicaciones |

## 3. Vehículos

| Fase | Vehículo | Modelo |
|---|---|---|
| Entrada | Cápsula con escudo de calor | `VH-03_EntryCapsule` (nuevo, sirve también para Venus, Júpiter y Titán) |
| Paracaídas | Paracaídas supersónico de 21,5 m | `VH-09_Parachute` (nuevo, sirve para todos) |
| Descenso con cohetes | Etapa de descenso, la "grúa aérea" con 8 motores | `VH-05_SkyCrane` (nuevo) |
| Misión | Rover Perseverance | `OB-18_Rover` (**ya existe**) |
| Depósito | Tubos de muestra en el suelo | `PR-05_SampleTube` (nuevo) |

## 4. Fases y controles

El tiempo va acelerado: los 7 minutos reales duran unos 80 segundos.

| Fase | Qué pasa | Stick izq. | Stick der. | A (grande) | B | C | Cómo se falla |
|---|---|---|---|---|---|---|---|
| **1. Entrada** (15 s) | La cápsula brilla por el calor | Mantener la nariz dentro del círculo verde | Mirar | — | — | — | Fuera del círculo el escudo se calienta; al 100 % se destruye |
| **2. Paracaídas** | El velocímetro muestra una **ventana verde** (Mach 1,2 a 2) | Igual | Mirar | — | **PARACAÍDAS** | — | Muy temprano se rompe; muy tarde no alcanza a frenar |
| **3. Soltar escudo** | Con el paracaídas abierto | — | Mirar | — | **SOLTAR ESCUDO** | — | Si no lo sueltas, el radar no ve el suelo (no hay altura) |
| **4. Descenso con cohetes** (desde 2 km) | Te separas del paracaídas | **Inclinar** para corregir la deriva | Mirar el suelo | **EMPUJE** (mantener; dosifica) | — | ESCANEAR zonas | Chocar a más de 3 m/s o con más de 15° de inclinación; quedarte sin combustible |
| **5. Elegir sitio** | Las zonas seguras se ven verdes y las rocas rojas (navegación por terreno, como Perseverance) | Inclinar hacia lo verde | Mirar | EMPUJE | — | — | Aterrizar sobre rocas o pendiente |
| **6. Grúa aérea** | A 20 m, flota quieto 3 s; los cables bajan el rover | Mantener quieto | Mirar | EMPUJE | — | **CORTAR CABLES** (cuando las ruedas tocan) | Cortar en el aire: el rover cae |
| **7. Rover** (misión) | **Cambia la vista**: cámara detrás del rover; la grúa se aleja volando | **Manejar**: arriba avanza, abajo retrocede, los lados giran | Mirar 360° | **TALADRAR** (mantener 3 s junto a la roca marcada) | **DEJAR MUESTRA** (en el depósito) | **ESCANEAR** roca | Batería en 0; quedar atascado en arena |

## 5. Física de este mundo

| Qué | Valor en el juego | Por qué |
|---|---|---|
| Gravedad | 3,71 m/s² | Real |
| Arrastre del aire | Muy bajo (densidad 0,02 kg/m³) | El paracaídas solo frena hasta ~100 m/s; lo demás lo hacen los cohetes |
| Rover | Hasta 5 m/s | El real va a 4 cm/s; aquí se acelera ×120 para que se pueda jugar (se dice en pantalla) |
| Saltos | Al pasar un montículo rápido, el rover se despega 2,6 veces más que en la Tierra | Poca gravedad |
| Pendientes | Por encima de 25° las ruedas resbalan | Poca gravedad = poco agarre |
| Arena blanda | Parches donde las ruedas giran sin avanzar; se sale retrocediendo despacio | Spirit quedó atrapado así en 2009 |

## 6. La misión y las estrellas

Hay 3 sitios de muestra marcados en el mapa, cada uno con una roca distinta:
1. **Delta del río antiguo**: roca en capas (sedimentos de un lago).
2. **Fondo del cráter**: roca volcánica.
3. **Orilla del lago antiguo**: carbonatos (donde el agua pudo guardar señales de vida).

Con el **espectrómetro UV**, que funciona como el instrumento SHERLOC de Perseverance, **ESCANEAR** muestra cuál roca tiene moléculas orgánicas.

Al final, llevas las 3 muestras al **depósito**. Es real: Perseverance dejó 10 tubos en "Three Forks" para la futura misión que traerá muestras a la Tierra.

| Estrellas | Condición |
|---|---|
| ★ | Aterrizar sano y salvo |
| ★★ | 3 muestras en el depósito |
| ★★★ | Además, la roca con orgánicos entre ellas y batería por encima del 20 % |

## 7. Lo que el diseño cambia aquí

| Decisión | Efecto |
|---|---|
| Combustible sobrante | Combustible del descenso con cohetes |
| **Solar** | La batería baja más rápido; en la tormenta de polvo carga 60 % menos (Opportunity murió así en 2018) |
| **RTG** | Batería constante, como Curiosity y Perseverance |
| Taladro | Sin él: ★ como máximo (no hay muestras) |
| Espectrómetro UV | Sin él no sabes cuál roca tiene orgánicos |
| Altímetro radar | Sin él, la altura se ve con ±20 % de error |
| Cámara teleobjetivo | Marca los sitios de muestra desde lejos |
| Antena | Qué tan rápido llega a la Tierra el informe de cada muestra |

## 8. Eventos

- **Tormenta de polvo** a mitad de la misión: se ve menos, la energía solar baja y el cielo se oscurece.
- **Remolino de polvo**: si pasa sobre el rover, limpia los paneles y la batería solar sube. Es real, les pasó a Spirit y Opportunity.

## 9. Multijugador

Carrera en Jezero: cada jugador aterriza su rover y gana quien deposita primero sus 3 muestras. Las rocas con orgánicos valen doble.

## 10. Informe "¿y si…?"

- "Con RTG en vez de paneles solares, la tormenta no te habría dejado sin batería."
- "Si hubieras abierto el paracaídas 2 segundos antes, habrías ahorrado 40 kg de combustible."
- "Perseverance tardó 2 años en juntar 10 muestras; tú lo hiciste en 4 minutos (con el tiempo acelerado)."

## 11. Fuentes

NASA Mars 2020 / Perseverance (mars.nasa.gov/mars2020): entrada, descenso y aterrizaje, navegación por terreno y depósito Three Forks. NASA Mars Fact Sheet (nssdc.gsfc.nasa.gov).
