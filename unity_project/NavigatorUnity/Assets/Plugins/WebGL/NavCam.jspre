// Navigator 2.0 — NavCam.jspre (web build only)
// The phone's camera, GPS and compass in the browser, without Unity's WebCamTexture / LocationService
// (in WebGL the first floods "glCopySubTextureCHROMIUM: Offset overflows texture dimensions" and shows
// nothing, the second never asks for permission).
//   Camera  — one getUserMedia stream (back camera) shared by the AR background and the QR scanner.
//             Frames are handed to Unity as small RGBA images (C# uploads them into a Texture2D).
//   Scanner — BarcodeDetector when the browser has it (Chrome Android), else jsQR (jsQR.jspre).
//   GPS     — navigator.geolocation (the browser asks "allow location?").
//   Compass — deviceorientationabsolute (Android) or webkitCompassHeading (iPhone).
// Guacalita Inc. — SAL / Claude

window['NavCam'] = (function () {
  var video = null, stream = null, users = 0, starting = false, error = '';
  var frameCanvas = null, frameCtx = null, lastTime = -1, lastGrab = 0;

  function camState() {
    if (stream && video && video.videoWidth > 0) return 2;   // live
    if (starting || stream) return 1;                         // asking / opening
    return error ? 3 : 0;                                     // denied or no camera / idle
  }

  function camStart() {
    users++;
    if (stream || starting) return;
    error = '';
    var md = navigator.mediaDevices;
    if (!md || !md.getUserMedia) { error = 'unsupported'; return; }   // http:// or an old browser
    starting = true;
    var open = function (constraints) { return md.getUserMedia({ audio: false, video: constraints }); };
    open({ facingMode: { ideal: 'environment' }, width: { ideal: 1280 }, height: { ideal: 720 } })
      .catch(function (e) {
        if (e && (e.name === 'NotAllowedError' || e.name === 'SecurityError')) throw e;
        return open(true);   // e.g. OverconstrainedError on odd webcams
      })
      .then(function (s) {
        starting = false;
        if (users <= 0) { s.getTracks().forEach(function (t) { t.stop(); }); return; }
        stream = s;
        video = document.createElement('video');
        video.setAttribute('playsinline', '');
        video.setAttribute('muted', '');
        video.muted = true;
        video.autoplay = true;
        // iOS only updates frames of a video that is in the page: keep it there, invisible
        video.style.cssText = 'position:fixed;left:0;top:0;width:2px;height:2px;opacity:0.01;pointer-events:none;z-index:-1';
        document.body.appendChild(video);
        video.srcObject = s;
        var p = video.play();
        if (p && p.catch) p.catch(function () {});
      })
      .catch(function (e) {
        starting = false;
        error = (e && e.name) || 'error';
        console.warn('[NavCam] camera:', error);
      });
  }

  function camStop() {
    users = Math.max(0, users - 1);
    if (users > 0) return;
    scanStop();
    if (stream) stream.getTracks().forEach(function (t) { t.stop(); });
    stream = null;
    if (video) { video.srcObject = null; if (video.parentNode) video.parentNode.removeChild(video); }
    video = null;
    lastTime = -1;
  }

  // Frame size for a given longest side, keeping the camera's aspect
  function frameSize(maxSide) {
    if (camState() !== 2) return null;
    var vw = video.videoWidth, vh = video.videoHeight;
    var k = Math.min(1, maxSide / Math.max(vw, vh));
    return { w: Math.max(2, Math.round(vw * k)), h: Math.max(2, Math.round(vh * k)) };
  }

  function grab(canvasHolder, w, h) {
    if (!canvasHolder.canvas) {
      canvasHolder.canvas = document.createElement('canvas');
      canvasHolder.ctx = canvasHolder.canvas.getContext('2d', { willReadFrequently: true });
    }
    var c = canvasHolder.canvas;
    if (c.width !== w || c.height !== h) { c.width = w; c.height = h; }
    canvasHolder.ctx.drawImage(video, 0, 0, w, h);
    return canvasHolder.ctx.getImageData(0, 0, w, h);
  }

  var frameHolder = {};
  // New RGBA frame (top row first) or null when there is nothing new; capped at ~30 fps
  function camFrame(maxSide) {
    var s = frameSize(maxSide);
    if (!s) return null;
    var now = performance.now();
    if (video.currentTime === lastTime || now - lastGrab < 30) return null;
    lastTime = video.currentTime; lastGrab = now;
    return grab(frameHolder, s.w, s.h);
  }

  // ── QR scanner ───────────────────────────────────────────────────────────
  var scanTimer = 0, scanBusy = false, onCode = null, detector = null, detectorChecked = false;
  var scanHolder = {};

  function checkDetector(done) {
    if (detectorChecked) { done(); return; }
    detectorChecked = true;
    var BD = window['BarcodeDetector'];
    if (!BD || !BD.getSupportedFormats) { done(); return; }
    BD.getSupportedFormats().then(function (f) {
      if (f.indexOf('qr_code') >= 0) detector = new BD({ formats: ['qr_code'] });
      done();
    }).catch(function () { done(); });
  }

  function scanOnce() {
    if (scanBusy || !onCode || camState() !== 2) return;
    scanBusy = true;
    var finish = function (text) {
      scanBusy = false;
      if (text && onCode) { var cb = onCode; scanStop(); cb(text); }
    };
    if (detector) {
      detector.detect(video).then(function (codes) {
        finish(codes && codes.length ? codes[0].rawValue : null);
      }).catch(function () { detector = null; finish(null); });   // fall back to jsQR next time
      return;
    }
    var jsQR = window['NavJsQR'];
    if (!jsQR) { finish(null); return; }
    var s = frameSize(800);
    var img = grab(scanHolder, s.w, s.h);
    var r = null;
    try { r = jsQR(img.data, img.width, img.height); } catch (e) {}
    finish(r ? r.data : null);
  }

  function scanStart(cb) {
    scanStop();
    onCode = cb;
    checkDetector(function () {
      if (onCode === cb && !scanTimer) scanTimer = setInterval(scanOnce, 180);
    });
  }

  function scanStop() {
    onCode = null;
    if (scanTimer) clearInterval(scanTimer);
    scanTimer = 0;
  }

  // ── GPS ──────────────────────────────────────────────────────────────────
  var geo = { state: 0, lat: 0, lon: 0, watch: null };   // 0 idle, 1 asking, 2 ok, 3 denied, 4 unavailable

  function geoStart() {
    if (geo.state === 1 || geo.state === 2) return;
    if (!navigator.geolocation) { geo.state = 4; return; }
    geo.state = 1;
    var ok = function (p) { geo.lat = p.coords.latitude; geo.lon = p.coords.longitude; geo.state = 2; };
    var fail = function (e) {
      if (geo.state === 2) return;   // keep the last good fix
      geo.state = e && e.code === 1 ? 3 : 4;
      console.warn('[NavCam] location:', e && e.message);
    };
    var opts = { enableHighAccuracy: true, timeout: 20000, maximumAge: 120000 };
    navigator.geolocation.getCurrentPosition(ok, fail, opts);
    try { geo.watch = navigator.geolocation.watchPosition(ok, function () {}, opts); } catch (e) {}
  }

  // ── Compass ──────────────────────────────────────────────────────────────
  // kind 1: absolute orientation (alpha measured from north); kind 2: iPhone heading in degrees
  var compass = { kind: 0, alpha: 0, beta: 0, gamma: 0, heading: 0, started: false };

  function compassStart() {
    if (compass.started) return;
    compass.started = true;
    var abs = function (e) {
      if (e.alpha === null || e.alpha === undefined) return;
      compass.alpha = e.alpha; compass.beta = e.beta || 0; compass.gamma = e.gamma || 0; compass.kind = 1;
    };
    var ios = function (e) {
      if (typeof e.webkitCompassHeading === 'number' && e.webkitCompassHeading >= 0) {
        compass.heading = e.webkitCompassHeading; if (compass.kind !== 1) compass.kind = 2;
      }
    };
    var listen = function () {
      if ('ondeviceorientationabsolute' in window) window.addEventListener('deviceorientationabsolute', abs);
      window.addEventListener('deviceorientation', function (e) { if (e.absolute) abs(e); else ios(e); });
    };
    var DOE = window['DeviceOrientationEvent'];
    if (DOE && typeof DOE.requestPermission === 'function') {
      // iPhone: only a tap may ask. Try now (works if a tap is in progress), else on the next tap
      var ask = function () {
        window.removeEventListener('touchend', ask); window.removeEventListener('click', ask);
        DOE.requestPermission().then(function (r) { if (r === 'granted') listen(); }).catch(function () {});
      };
      DOE.requestPermission().then(function (r) { if (r === 'granted') listen(); else throw 0; })
        .catch(function () { window.addEventListener('touchend', ask); window.addEventListener('click', ask); });
    } else listen();
  }

  return {
    'start': camStart, 'stop': camStop, 'state': camState, 'size': frameSize, 'frame': camFrame,
    'scanStart': scanStart, 'scanStop': scanStop,
    'geo': geo, 'geoStart': geoStart,
    'compass': compass, 'compassStart': compassStart
  };
})();
