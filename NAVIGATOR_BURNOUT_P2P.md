# NAVIGATOR 2.0 — Sistema Burnout + Offline P2P + Chat
**Guacalita Inc. — Choques épicos sin internet**
**Fecha: 2026-09-23**

> **Nota:** documento de diseño histórico. Los valores y el comportamiento actuales del juego están en
> [`NAVIGATOR_GAME_DESIGN_COMPLETE.md`](NAVIGATOR_GAME_DESIGN_COMPLETE.md), validado contra el código.


---

## BURNOUT EN EL ESPACIO — La filosofía

> En Burnout el choque NO es el fin — es la herramienta.
> Volar peligroso te da energía. Destruir rivals te da boost.
> El riesgo ES la mecánica central.
>
> En Navigator aplica igual:
> Volar cerca de asteroides sin chocar = boost
> Destruir nave rival = boost enorme + rival fuera por segundos
> Tu propio wreckage puede seguir dañando a otros

---

## SISTEMA BURNOUT — BARRA DE BOOST

### Cómo se llena la barra
```
ACCIÓN PELIGROSA                    BOOST GANADO
────────────────────────────────────────────────
Near-miss asteroide  (<3u sin tocar) → +8%
Near-miss nave rival (<5u sin tocar) → +12%
Atravesar campo de escombros         → +15%
Takedown exitoso (destruir rival)    → +35% INSTANTÁNEO
Ir al límite de combustible (< 5%)   → +5% continuo
Volar en zona de radiación           → +10% por segundo
Gravity assist (slingshot)           → +20%
────────────────────────────────────────────────
Barra llena = BURNOUT MODE (10 segundos)
```

### BURNOUT MODE — cuando la barra llena
```
Activación: automática al llenar la barra
Duración: 10 segundos
Efectos:
  → Velocidad x2.5 (el más rápido del juego)
  → Invencibilidad total a colisiones
  → Exhaust cambia a BLANCO CEGADOR
  → Trail de luz deja marca en el espacio AR
  → Sonido de motor en overdrive
  → Pantalla con vignette de fuego en los bordes
  → Otros jugadores ven tu nave con aura de fuego

Estrategia: guardar el burnout para la recta final
```

---

## SISTEMA TAKEDOWN

### Tipos de takedown
```
TAKEDOWN FRONTAL
  Impactar de frente a rival a alta velocidad
  → Rival: spinning out 3 segundos, pierde velocidad 80%
  → Tú: rebote hacia atrás, +35% boost, pequeño daño
  → Visual: explosión enorme, escombros en todas direcciones

TAKEDOWN LATERAL (el más elegante)
  Raspar el costado del rival a alta velocidad
  → Rival: gira fuera de control 2 segundos
  → Tú: pequeño desvío, +25% boost, sin daño propio
  → Visual: chispas + panel arrancado volando

TAKEDOWN TRASERO
  Embestir por detrás (más fácil de ejecutar)
  → Rival: lanzado hacia adelante como catapulta
  → Tú: desaceleras un poco, +20% boost
  → Si el rival choca un asteroide después → CHAIN TAKEDOWN +50%

CHAIN TAKEDOWN (el más espectacular)
  Tu rival destruido choca con otro rival
  → Ambos rivales eliminados
  → Tú: +100% boost = BURNOUT INSTANTÁNEO
  → Anuncio en pantalla: "¡¡CHAIN TAKEDOWN!!"
  → Todo el chat se entera
```

### Aftertouch — control del wreckage
```
Inspirado en Burnout Revenge:
Cuando tu nave es destruida (0 vidas en modo carrera libre)
  → Todavía controlas los escombros por 3 segundos
  → Puedes dirigir los fragmentos con el giroscopio
  → Si tus escombros golpean a otro jugador → TAKEDOWN PÓSTUMO
  → Visual: fragmentos de tu nave volando en AR como meteoros

Esto hace que MORIR sea divertido en lugar de frustrante
```

---

## NEAR-MISS SYSTEM

```
Inspirado en Burnout Paradise:
El sistema detecta cada vez que CASI chocas

Near-miss con asteroide:
  Distancia < 3 unidades sin tocar
  → Flash amarillo en el borde de pantalla
  → "+NEAR MISS" aparece en HUD
  → Boost +8%

Near-miss con nave rival:
  Distancia < 5 unidades a alta velocidad
  → Flash cyan en el borde
  → "+NEAR MISS" con nombre del rival
  → Boost +12%

Combo de near-misses:
  3 seguidos sin choque = x2 boost por near-miss
  5 seguidos = x3 boost
  10 seguidos = "UNTOUCHABLE" + boost completo instantáneo

Estrategia: zigzaguear entre asteroides llenando la barra
antes de entrar en modo ataque contra rivales
```

---

## CRASH PHYSICS — Físicamente correcto

```
Basado en conservación de momento:
  m₁v₁ + m₂v₂ = total momento conservado

La nave MÁS PESADA (más combustible) gana el choque:
  Nave con 5,000 kg combustible choca con nave 1,000 kg
  → Nave pesada: pierde 10% velocidad
  → Nave liviana: sale disparada a 5x velocidad
  
  LECCIÓN NASA: por qué los satélites evitan debris —
  un tornillo de 10g a 7km/s destruye satélites de toneladas

Escombros físicos reales:
  Al destruirse una nave → 8-12 fragmentos independientes
  Cada fragmento tiene:
    → Masa propia
    → Velocidad inicial = velocidad nave × dirección aleatoria
    → Rotación propia aleatoria
    → Vida: 30 segundos antes de desaparecer
    → Colisión activa: pueden golpear a otros jugadores
```

---

## OFFLINE P2P — Sin internet, sin servidor

### Arquitectura completamente offline
```
ESCENARIO TÍPICO DE USO:
  Hackathon NASA → sala sin buena WiFi
  Colegio → sin permisos de internet en la red
  Demo en evento → spot propio sin conexión
  Dos amigos en casa → sin configurar nada

SOLUCIÓN: Navigator funciona con WiFi Direct (P2P local)
  Los dos celulares se conectan DIRECTAMENTE entre sí
  Sin pasar por internet
  Sin servidor
  Sin cuenta
  Solo: escanea QR → conectados → jugando
```

### Cómo funciona la conexión offline
```
PASO 1 — Host crea sala (sin internet):
  App genera QR code único con:
    IP local del dispositivo
    Puerto P2P (ej: 47832)
    Clave pública Kyber-1024 del host
    Session ID único

PASO 2 — Guest escanea QR:
  Lee IP + puerto + clave pública
  Establece conexión TCP/UDP directa a esa IP
  Realiza handshake Kyber-1024 (key exchange post-cuántico)
  Firma con Dilithium-5 (autenticación)
  CONECTADOS — sin tocar internet en ningún momento

PASO 3 — Juego:
  Todo el estado del juego va directo device-to-device
  ShipState: position, velocity, yaw, throttle, boost, damage
  Latencia: 1-5ms en WiFi local vs 50-200ms en internet
  → El juego se siente MÁS FLUIDO offline que online

PASO 4 — 3+ jugadores:
  Host hace de "relay" entre guests
  Topología: estrella (host central) para 2-4 jugadores
  O mesh completo para 5-8 jugadores
```

### Modos de descubrimiento
```
MODO A — QR Code (principal):
  Host muestra QR en pantalla
  Guest abre cámara dentro del juego y escanea
  Conexión en < 2 segundos
  Funciona en: WiFi, WiFi Direct, Hotspot personal

MODO B — Auto-descubrimiento mDNS:
  Si ambos están en la misma red WiFi
  La app se anuncia: "_navigator._tcp.local"
  Otros dispositivos aparecen automáticamente en lobby
  "Nave de [nombre] encontrada en tu red"

MODO C — Hotspot manual:
  Un jugador activa hotspot en su celular
  Otros se conectan al hotspot
  Luego QR o mDNS para conectar el juego
  Funciona sin WiFi externo completamente

MODO D — Bluetooth (fallback):
  Si no hay WiFi disponible
  Bluetooth para descubrimiento + inicio de conexión
  Luego WiFi Direct para los datos del juego
  Más lento pero funciona
```

### Código Unity — ConnectionManager
```csharp
public class OfflineConnectionManager : MonoBehaviour
{
    // Genera sala sin internet
    public async Task<string> CreateRoom()
    {
        var localIP   = GetLocalIPAddress();
        var port      = FindFreePort();
        var kyberKeys = Kyber1024.GenerateKeyPair();
        
        // QR contiene: ip:port:publicKey:sessionId
        var qrData = $"{localIP}:{port}:{kyberKeys.Public}:{SessionId}";
        DisplayQRCode(qrData); // muestra en pantalla
        
        await ListenForConnections(port, kyberKeys.Private);
        return sessionId;
    }

    // Conecta escaneando QR (sin internet)
    public async Task JoinFromQR(string qrData)
    {
        var parts    = qrData.Split(':');
        var hostIP   = parts[0];
        var port     = int.Parse(parts[1]);
        var hostKey  = parts[2];
        
        // Conexión TCP directa — sin pasar por servidor
        var socket = new TcpClient(hostIP, port);
        
        // Handshake post-cuántico
        var (sharedSecret, ciphertext) = Kyber1024.Encapsulate(hostKey);
        await socket.SendAsync(ciphertext);
        
        // Firma de identidad
        var signature = Dilithium5.Sign(myPublicKey, myPrivateKey);
        await socket.SendAsync(signature);
        
        // ¡Conectados! Ya hay canal cifrado post-cuántico
        StartGameSync(socket, sharedSecret);
    }
    
    string GetLocalIPAddress()
    {
        // Obtiene IP en la red local (192.168.x.x)
        // Sin tocar internet
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            ?.ToString() ?? "127.0.0.1";
    }
}
```

---

## CHAT OFFLINE P2P

### Características del chat
```
FUNCIONA:
  ✅ Sin internet
  ✅ Cifrado Kyber-1024 (nadie puede interceptar)
  ✅ Mensajes de texto
  ✅ Emojis / reacciones rápidas
  ✅ Mensajes de voz cortos (PTT — Push to Talk)
  ✅ Mensajes automáticos del sistema ("Takedown de [Nombre]!")
  ✅ Chat grupal (todos los jugadores conectados)
  ✅ Chat privado (solo dos jugadores)

NO REQUIERE:
  ❌ WhatsApp
  ❌ Cuenta de usuario
  ❌ Servidor de mensajes
  ❌ Internet
```

### Tipos de mensaje
```
TEXTO LIBRE:
  El jugador escribe desde teclado en pantalla
  Máximo 100 caracteres
  Aparece como burbuja flotante sobre la nave del emisor en AR

REACCIONES RÁPIDAS (sin teclado):
  4 botones siempre visibles durante el juego:
  [🔥] [👀] [🚀] [💥]
  Aparecen como emoji flotante grande sobre la nave

MENSAJES DEL SISTEMA (automáticos):
  "🔥 TAKEDOWN! [NombreA] destruyó a [NombreB]"
  "⚡ [Nombre] activó BURNOUT MODE"
  "🏆 [Nombre] llegó a Marte — PRIMERO"
  "💀 [Nombre] quedó sin combustible"
  "🛡️ [Nombre] recogió escudo"

VOZ PTT (Push to Talk):
  Mantén presionado el botón de micrófono
  Audio se comprime y envía por el canal P2P cifrado
  Otros jugadores escuchan inmediatamente
  Indicador visual: "🎙️ [Nombre] hablando..."
```

### UI del chat en AR
```
DURANTE EL VUELO:
  Chat oculto por defecto (no distrae)
  Ícono de chat en esquina superior izquierda
  Si hay mensaje nuevo → ícono parpadea + vibración corta
  
  Al tocar el ícono:
    Panel semitransparente se desliza desde arriba
    Historial de mensajes (últimos 20)
    Campo de texto + botones de reacción rápida
    El universo AR sigue visible detrás
  
  Los mensajes del sistema aparecen SIEMPRE
  flotando en el centro de pantalla por 2 segundos
  sin necesidad de abrir el chat

BURBUJA DE CHAT EN AR:
  Cuando alguien escribe en chat
  Aparece burbuja flotando sobre su nave en el espacio AR
  Como en juegos MMO — el nombre y el texto sobre el personaje
  Desaparece en 4 segundos
```

### Código Unity — ChatSystem
```csharp
public class P2PChatSystem : MonoBehaviour
{
    readonly Queue<ChatMessage> _history = new(20);
    
    // Enviar mensaje (cifrado automáticamente por SendaBridge)
    public void SendMessage(string text)
    {
        var msg = new ChatMessage {
            From      = LocalPlayer.Name,
            Text      = text,
            Timestamp = DateTime.UtcNow,
            Type      = MessageType.Text
        };
        // SendaBridge cifra con Kyber antes de enviar
        SendaBridge.Send(NavMsgType.Chat, JsonUtility.ToJson(msg));
        AddToHistory(msg);
    }

    // Recibir mensaje de otro jugador
    public void OnRemoteMessage(ChatMessage msg)
    {
        AddToHistory(msg);
        ShowARBubble(msg); // burbuja flotante sobre su nave en AR
        if (chatPanelClosed) ShowNotificationPulse();
    }
    
    // Mensaje automático del sistema
    public void BroadcastSystemEvent(string eventText)
    {
        var msg = new ChatMessage {
            From = "SISTEMA",
            Text = eventText,
            Type = MessageType.System
        };
        ShowCenterOverlay(msg, duration: 2f); // siempre visible 2s
        SendaBridge.Broadcast(NavMsgType.Chat, JsonUtility.ToJson(msg));
    }
}
```

---

## FLUJO COMPLETO OFFLINE — De 0 a jugando en 30 segundos

```
[Jugador A]                          [Jugador B]
    │                                     │
    │ Abre Navigator                      │ Abre Navigator
    │ Toca "CREAR SALA"                   │
    │ QR aparece en pantalla ─────────────┤ Toca "UNIRSE"
    │                                     │ Escanea QR con cámara
    │ ← Handshake Kyber (< 1s) ──────────→│
    │ ← Dilithium firma ─────────────────→│
    │                                     │
    │ CONECTADOS (sin internet) ──────────│
    │                                     │
    │ Lobby aparece en AR                 │ Lobby aparece en AR
    │ Ve nave de B en su espacio          │ Ve nave de A en su espacio
    │ Chat disponible                     │ Chat disponible
    │                                     │
    │ Toca "INICIAR CARRERA"              │
    │ ← Countdown sincronizado ──────────→│
    │                                     │
    │ ¡JUGANDO! — 0 segundos de setup ────│
    │ Todo cifrado, sin servidor, sin web │
```

---

## ASSETS NUEVOS PARA BURNOUT + CHAT

### Efectos visuales nuevos (Unity VFX — NO Meshy)
```
NO crear en Meshy — Unity VFX Graph:
  takedown_explosion    : explosión masiva con onda expansiva
  chain_takedown_flash  : destello dorado que conecta dos naves
  burnout_aura          : fuego blanco alrededor de nave en burnout
  near_miss_spark       : chispas al pasar rozando
  aftertouch_debris     : fragmentos controlables post-muerte
  wreckage_trail        : humo de escombros flotando
  burnout_trail         : línea de luz dejada por el burnout
```

### UI nuevo para Burnout (Unity Canvas — NO Meshy)
```
NO crear en Meshy — Unity UI:
  boost_bar             : barra de burnout (izquierda de pantalla)
  takedown_banner       : banner "TAKEDOWN!" animado
  chain_banner          : banner "CHAIN TAKEDOWN!!" más grande
  near_miss_counter     : contador de near-misses seguidos
  burnout_countdown     : cuenta regresiva durante burnout mode
  chat_bubble_ar        : burbuja de chat sobre nave en AR
  chat_notification_dot : punto pulsante de mensaje nuevo
  ptt_indicator         : indicador "🎙️ hablando..."
```

---

## RESUMEN — POR QUÉ ESTO GANA

```
NAVIGATOR = Burnout Takedown + NASA Physics + AR + Offline P2P

Lo que nadie más tiene:
  ✅ Choques con física de conservación de momento REAL
  ✅ Takedowns que llenan la barra de boost (como Burnout)
  ✅ Near-miss system (riesgo = recompensa)
  ✅ Aftertouch con tus escombros (morir sigue siendo divertido)
  ✅ Todo ocurre en AR en tu espacio real
  ✅ Funciona sin internet — QR code → jugando en 30 segundos
  ✅ Chat cifrado post-cuántico incluido
  ✅ Los mensajes del sistema aparecen en AR sobre las naves
  ✅ La física NASA (masa del combustible) afecta los choques
     → nave cargada de fuel = tanque en las colisiones
     → nave vacía = papel en las colisiones
```

---

*Diseño y concepto: SAL / Guacalita Inc.*
*Implementación Unity: Claude (asistente)*
