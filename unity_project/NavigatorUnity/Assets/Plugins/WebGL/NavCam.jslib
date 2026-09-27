// Navigator 2.0 — NavCam.jslib (web build only)
// C# ⇄ JavaScript bridge for NavCam.jspre: camera frames, QR scanner, GPS and compass.
// Guacalita Inc. — SAL / Claude

mergeInto(LibraryManager.library, {

  // ── Camera ────────────────────────────────────────────────────────────────
  NavCam_Start: function () { window['NavCam']['start'](); },
  NavCam_Stop:  function () { window['NavCam']['stop'](); },
  // 0 idle · 1 opening / waiting for permission · 2 live · 3 denied or no camera
  NavCam_State: function () { return window['NavCam']['state'](); },

  // Size of the frames for this longest side, packed as width * 65536 + height (0 = not live)
  NavCam_Size: function (maxSide) {
    var s = window['NavCam']['size'](maxSide);
    return s ? s.w * 65536 + s.h : 0;
  },

  // Copies the newest frame (RGBA, top row first) into buf; 1 = new frame, 0 = nothing new
  NavCam_Frame: function (buf, len, maxSide) {
    var img = window['NavCam']['frame'](maxSide);
    if (!img || img.data.length !== len) return 0;
    HEAPU8.set(img.data, buf);
    return 1;
  },

  // ── QR scanner: the text goes back with SendMessage(go, "OnWebScan", text) ─
  NavCam_ScanStart: function (goPtr) {
    var go = UTF8ToString(goPtr);
    window['NavCam']['scanStart'](function (text) {
      if (typeof SendMessage === 'function') SendMessage(go, 'OnWebScan', text);
      else if (typeof Module !== 'undefined' && Module.SendMessage) Module.SendMessage(go, 'OnWebScan', text);
    });
  },
  NavCam_ScanStop: function () { window['NavCam']['scanStop'](); },

  // ── GPS ───────────────────────────────────────────────────────────────────
  NavGeo_Start: function () { window['NavCam']['geoStart'](); },
  // 0 idle · 1 asking · 2 ok · 3 denied · 4 unavailable
  NavGeo_State: function () { return window['NavCam']['geo'].state; },
  NavGeo_Get: function (axis) { var g = window['NavCam']['geo']; return axis === 0 ? g.lat : g.lon; },

  // ── Compass ───────────────────────────────────────────────────────────────
  NavCompass_Start: function () { window['NavCam']['compassStart'](); },
  // 0 none yet · 1 absolute orientation (Android) · 2 heading only (iPhone)
  NavCompass_Kind: function () { return window['NavCam']['compass'].kind; },
  // axis 0..2 = alpha, beta, gamma of the absolute orientation; 3 = iPhone heading
  NavCompass_Get: function (axis) {
    var c = window['NavCam']['compass'];
    return axis === 0 ? c.alpha : axis === 1 ? c.beta : axis === 2 ? c.gamma : c.heading;
  }
});
