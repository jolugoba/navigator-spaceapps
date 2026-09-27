// Navigator 2.0 — NavScreen.jspre (web build only)
// The game is played in landscape, always:
//   · on the first tap the page goes full screen and locks landscape (Android Chrome; the lock
//     only works in full screen). Installed as an app, the manifest already asks for landscape.
//   · where the browser can't lock (iPhone Safari), a "turn your phone" card covers the game
//     while the phone is upright.
// Guacalita Inc. — SAL / Claude

window['NavScreen'] = (function () {
  var touch = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);

  function angle() {
    var o = screen.orientation;
    if (o && typeof o.angle === 'number') return o.angle;
    if (typeof window.orientation === 'number') return (window.orientation + 360) % 360;
    return 0;
  }

  function lock() {
    var o = screen.orientation;
    if (!o || !o.lock) return;
    try { o.lock('landscape').catch(function () {}); } catch (e) {}
  }

  function goFullscreenAndLock() {
    var d = document, el = d.documentElement;
    var inFs = d.fullscreenElement || d.webkitFullscreenElement;
    if (inFs) { lock(); return; }
    var req = el.requestFullscreen || el.webkitRequestFullscreen;
    if (!req) { lock(); return; }
    try {
      var p = req.call(el, { navigationUI: 'hide' });
      if (p && p.then) p.then(lock).catch(lock); else lock();
    } catch (e) { lock(); }
  }

  var card = null;
  function makeCard() {
    card = document.createElement('div');
    card.style.cssText = 'position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;' +
      'flex-direction:column;gap:18px;background:#050a18;color:#7fe3ff;font:600 20px system-ui,sans-serif;text-align:center;padding:24px';
    var es = (navigator.language || '').toLowerCase().indexOf('es') === 0;
    card.innerHTML = '<div style="font-size:64px;animation:navrot 1.6s ease-in-out infinite">&#128241;</div>' +
      '<div>' + (es ? 'Gira tu celular: Navigator se juega en horizontal' : 'Turn your phone: Navigator is played in landscape') + '</div>' +
      '<style>@keyframes navrot{0%,20%{transform:rotate(0)}60%,100%{transform:rotate(-90deg)}}</style>';
    document.body.appendChild(card);
  }

  function update() {
    if (!touch) return;
    if (!card) makeCard();
    var portrait = window.innerHeight > window.innerWidth;
    card.style.display = portrait ? 'flex' : 'none';
  }

  if (touch) {
    var first = function () {
      goFullscreenAndLock();
      window.removeEventListener('touchend', first, true);
      window.removeEventListener('click', first, true);
    };
    window.addEventListener('touchend', first, true);
    window.addEventListener('click', first, true);
    // Leaving full screen (back gesture) unlocks: lock again on the next tap
    document.addEventListener('fullscreenchange', function () {
      if (!document.fullscreenElement) {
        window.addEventListener('touchend', first, true);
        window.addEventListener('click', first, true);
      }
    });
    window.addEventListener('resize', update);
    window.addEventListener('orientationchange', update);
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', update); else update();
  }

  return { 'angle': angle, 'lock': goFullscreenAndLock };
})();
