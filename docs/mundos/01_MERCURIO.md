# Mundo 1 — MERCURIO · "Hielo junto al Sol"

> El planeta más cercano al Sol guarda **hielo** en cráteres del polo norte donde el Sol nunca entra. Aterrizas en el borde iluminado y bajas con un rover a la sombra eterna para sacarlo. Nadie ha aterrizado nunca en Mercurio: serías el primero.

## 1. Ficha real

| Dato | Valor real |
|---|---|
| Gravedad | 3,70 m/s² (0,38 g): saltas **2,7 veces** más alto |
| Atmósfera | **Ninguna** (solo una exosfera muy tenue) |
| Temperatura | +430 °C al sol · −180 °C en la sombra |
| Día solar | 176 días terrestres |
| El Sol | Hasta 3 veces más grande en el cielo y hasta 10 veces más brillante |
| Descubrimiento | MESSENGER (2011-2015) confirmó **hielo de agua** en cráteres polares en sombra permanente |
| Hoy | BepiColombo (Europa y Japón) llega a su órbita a fines de 2026 |

**Lo que lo hace único:** sin aire no hay escudo de calor ni paracaídas que sirvan; se aterriza **solo con cohetes**, como el módulo lunar del Apolo. Y el peligro no es un reloj, son **dos extremos de temperatura** a pocos metros uno del otro.

## 2. Cómo se ve

| Elemento | Detalle |
|---|---|
| Cielo | **Negro con estrellas**, incluso de día (no hay aire que disperse la luz) |
| Sol | Enorme y blanco, **pegado al horizonte**: en el polo siempre está bajo, así que las sombras son larguísimas |
| Suelo | Gris `#7D7873`, gris oscuro `#4B4845`, rayos claros de cráteres jóvenes `#B8B3AC` |
| Terreno | Cráteres de todos los tamaños, **escarpes** (acantilados de cuando el planeta se encogió), y el cráter polar cuyo fondo es **negro total** |
| Hielo | Blanco azulado `#CFE6F5`, solo visible con luces o con el mapeador infrarrojo |
| Luz | Contraste extremo: lo iluminado encandila, lo que está en sombra es negro |
| Partículas | El polvo que levantan las ruedas vuela en **arcos perfectos** y cae enseguida: no hay aire que lo sostenga |

## 3. Vehículos

| Fase | Vehículo | Modelo |
|---|---|---|
| Descenso | Módulo de aterrizaje con cohetes y 4 patas | `VH-01_Lander` (nuevo; se reutiliza en Tritón) |
| Misión | **Rover térmico**: 6 ruedas, **parasol** blanco como el de MESSENGER, faros | `VH-02_ThermalRover` (nuevo) |
| Objetivo | Bloques de hielo en el fondo del cráter | `PR-01_IceChunk` (nuevo; se reutiliza en Tritón) |

## 4. Fases y controles

| Fase | Qué pasa | Stick izq. | Stick der. | A (grande) | B | C | Cómo se falla |
|---|---|---|---|---|---|---|---|
| **1. Frenado orbital** | Vas de lado a 80 m/s y a 3 km de altura; hay que frenar con los motores | **Inclinar** el módulo (apunta el chorro) | Mirar | **EMPUJE** (mantener) | — | — | Gastar todo el combustible antes de bajar |
| **2. Descenso** | Bajas hacia el **borde iluminado** del cráter (ahí hay luz para cargar y señal a la Tierra) | Inclinar | Mirar | EMPUJE | **PATAS** (sacar las patas) | ESCANEAR zona | Tocar suelo sin las patas; más de 3 m/s; pendiente |
| **3. Despliegue** | Baja una rampa y **cambia la vista** al rover | — | — | — | — | — | — |
| **4. Rover** | Entras a la sombra por el hielo y sales al sol a cargar | **Manejar** | Mirar | **TALADRAR** hielo (mantener 3 s) | **LUCES** (en la sombra no ves nada sin ellas) | **ESCANEAR** (infrarrojo: el hielo brilla) | **Temperatura** fuera de rango; batería en 0 |

### El medidor de temperatura (la mecánica central)

```
 −180 °C  ████ CONGELADO ████ ── seguro ── ████ QUEMADO ████  +430 °C
           (en la sombra baja)                (al sol sube)
```

- Al **sol**, el rover se calienta. El parasol ayuda si el Sol le da de frente, así que conviene manejar mirando al Sol.
- En la **sombra** se enfría. Las luces y el taladro gastan batería, pero dan un poco de calor.
- Hay que ir y volver entre el sol y la sombra, como en un juego de sigilo.

## 5. Física de este mundo

| Qué | Valor en el juego |
|---|---|
| Gravedad | 3,70 m/s² |
| Arrastre | **Cero**: el paracaídas no existe (si el jugador lo busca, el juego le explica por qué) |
| Descenso | Solo empuje contra la gravedad: el combustible del diseño decide |
| Rover | Hasta 5 m/s (acelerado para jugar); saltos 2,7× |
| Térmico | Al sol +8 °C/s; en la sombra −6 °C/s (acelerado) |

## 6. La misión y las estrellas

1. Aterrizar en el borde iluminado.
2. Bajar al cráter en sombra y encontrar el hielo.
3. Taladrar **3 núcleos de hielo**.
4. Volver al módulo, al sol, para enviar los datos a la Tierra.

| Estrellas | Condición |
|---|---|
| ★ | Aterrizar sano y salvo |
| ★★ | 3 núcleos de hielo y regreso al módulo |
| ★★★ | Además, nunca pasar a zona roja de temperatura |

## 7. Lo que el diseño cambia aquí

| Decisión | Efecto |
|---|---|
| **Solar** | Carga **muchísimo** al sol (luz hasta 10 veces más fuerte) y **nada** en la sombra: hay que salir a cargar |
| **RTG** | Carga siempre y **da calor**: ayuda en la sombra, pero al sol te calienta más rápido. Una decisión con dos caras |
| Mapeador infrarrojo | Sin él no ves dónde está el hielo: taladras a ciegas y gastas batería |
| Taladro | Sin él no hay núcleos (★ como máximo) |
| Altímetro radar | Sin él, la altura se ve con ±20 % de error |
| Combustible sobrante | Todo el aterrizaje depende de él: sin aire no hay otra forma de frenar |

## 8. Eventos

- **Rayos del Sol que avanzan:** la línea entre luz y sombra se mueve despacio, así que un lugar seguro deja de serlo.
- **Escarpe:** un acantilado que obliga a dar la vuelta.

## 9. Multijugador

Carrera por el hielo: todos bajan al mismo cráter y gana quien vuelve primero al sol con 3 núcleos.

## 10. Informe "¿y si…?"

- "Con RTG no habrías tenido que salir a cargar, pero al sol te habrías quemado 30 % más rápido."
- "Sin aire, cada m/s que frenaste costó combustible: con 50 kg más habrías aterrizado sin la alarma."

## 11. Fuentes

NASA MESSENGER (hielo polar, 2012). ESA/JAXA BepiColombo. NASA Mercury Fact Sheet.
