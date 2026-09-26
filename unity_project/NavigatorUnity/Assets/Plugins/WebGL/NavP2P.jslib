// Navigator 2.0 — NavP2P.jslib (web build only)
// C# ⇄ JavaScript bridge for NavP2P.jspre (peer-to-peer) and the phone's orientation sensor.
// Events go back to Unity with SendMessage(gameObject, "OnWebP2P", "kind\na\nb").
// Guacalita Inc. — SAL / Claude

mergeInto(LibraryManager.library, {

  $NavWeb: {
    str: function (s) {
      s = s || '';
      var n = lengthBytesUTF8(s) + 1, buf = _malloc(n);
      stringToUTF8(s, buf, n);
      return buf;
    },
    send: function (go, kind, a, b) {
      var msg = kind + '\n' + (a || '') + '\n' + (b || '');
      if (typeof SendMessage === 'function') SendMessage(go, 'OnWebP2P', msg);
      else if (typeof Module !== 'undefined' && Module.SendMessage) Module.SendMessage(go, 'OnWebP2P', msg);
    },
    // Latest device orientation (degrees); alpha is null until the first real reading
    ori: { alpha: null, beta: 0, gamma: 0, started: false }
  },

  NavP2P_Init__deps: ['$NavWeb'],
  NavP2P_Init: function (goPtr) {
    var go = UTF8ToString(goPtr);
    var id = window['NavP2P']['init'](function (kind, a, b) { NavWeb.send(go, kind, a, b); });
    return NavWeb.str(id);
  },

  NavP2P_MakeOffer: function () { window['NavP2P']['makeOffer'](); },

  NavP2P_Accept: function (codePtr) { window['NavP2P']['accept'](UTF8ToString(codePtr)); },

  NavP2P_Broadcast: function (dataPtr) { window['NavP2P']['broadcast'](UTF8ToString(dataPtr)); },

  NavP2P_Send: function (toPtr, dataPtr) { window['NavP2P']['send'](UTF8ToString(toPtr), UTF8ToString(dataPtr)); },

  // ── Orientation sensor (gyroscope) ────────────────────────────────────────
  NavGyro_Supported: function () {
    var touch = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    return (typeof DeviceOrientationEvent !== 'undefined' && touch) ? 1 : 0;
  },

  NavGyro_Start__deps: ['$NavWeb'],
  NavGyro_Start: function () {
    var o = NavWeb.ori;
    if (o.started) return;
    o.started = true;
    var listen = function () {
      window.addEventListener('deviceorientation', function (e) {
        if (e.alpha === null && e.beta === null) return;
        o.alpha = e.alpha || 0; o.beta = e.beta || 0; o.gamma = e.gamma || 0;
      });
    };
    // iOS asks for permission, and only from a tap: request it on the first touch
    if (typeof DeviceOrientationEvent !== 'undefined' && typeof DeviceOrientationEvent.requestPermission === 'function') {
      var ask = function () {
        DeviceOrientationEvent.requestPermission().then(function (r) { if (r === 'granted') listen(); }).catch(function () {});
        window.removeEventListener('touchend', ask); window.removeEventListener('click', ask);
      };
      window.addEventListener('touchend', ask); window.addEventListener('click', ask);
    } else listen();
  },

  NavGyro_HasData__deps: ['$NavWeb'],
  NavGyro_HasData: function () { return NavWeb.ori.alpha === null ? 0 : 1; },

  // ── Clipboard and vibration ───────────────────────────────────────────────
  NavWeb_Copy: function (textPtr) {
    var t = UTF8ToString(textPtr);
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) { navigator.clipboard.writeText(t); return; }
    } catch (e) {}
    var ta = document.createElement('textarea'); ta.value = t; document.body.appendChild(ta);
    ta.select(); try { document.execCommand('copy'); } catch (e) {} document.body.removeChild(ta);
  },

  NavWeb_Paste__deps: ['$NavWeb'],
  NavWeb_Paste: function (goPtr) {
    var go = UTF8ToString(goPtr);
    var reply = function (t) {
      if (typeof SendMessage === 'function') SendMessage(go, 'OnWebPaste', t || '');
      else if (typeof Module !== 'undefined' && Module.SendMessage) Module.SendMessage(go, 'OnWebPaste', t || '');
    };
    if (navigator.clipboard && navigator.clipboard.readText)
      navigator.clipboard.readText().then(reply).catch(function () { reply(window.prompt('Paste the challenge code / Pega el código del reto') || ''); });
    else reply(window.prompt('Paste the challenge code / Pega el código del reto') || '');
  },

  NavWeb_Vibrate: function (ms) { try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) {} },

  NavGyro_Get__deps: ['$NavWeb'],
  NavGyro_Get: function (axis) {
    var o = NavWeb.ori;
    return axis === 0 ? (o.alpha || 0) : axis === 1 ? o.beta : o.gamma;
  }
});
