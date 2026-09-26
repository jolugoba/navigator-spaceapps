// Navigator 2.0 — NavP2P.jspre (web build only)
// Serverless peer-to-peer for the browser, replacing the native Iroh library:
//   · WebRTC data channels, no signalling server. Two phones connect by swapping two QR codes:
//     the host shows an OFFER code, the guest scans it and shows an ANSWER code, the host scans
//     that. It works on the same Wi-Fi (or a phone hotspot) with no internet at all; when online,
//     a public STUN server is added so phones on different networks can often connect too.
//   · Star with relay: every message carries its origin ("f") and an id ("m"); each node delivers
//     it and forwards it to its other links, so a guest reaches every player through the host.
//   · Codes are deflate-compressed and base64url-encoded so they fit a phone-scannable QR.
// Unity talks to it through NavP2P.jslib; this file is plain JS so it can be tested on its own.
// Guacalita Inc. — SAL / Claude

var NavP2P = (function () {
  'use strict';
  var me = null, emit = function () {};
  var links = {};        // peerId -> RTCDataChannel (direct neighbours)
  var pending = {};      // offerId -> RTCPeerConnection waiting for its answer
  var seen = {}, seenOrder = [];
  var counter = 0;

  function rid(n) {
    var a = new Uint8Array(n); crypto.getRandomValues(a);
    return Array.prototype.map.call(a, function (b) { return ('0' + b.toString(16)).slice(-2); }).join('');
  }

  function config() {
    // Offline: host candidates only (same Wi-Fi / hotspot). Online: STUN helps across networks.
    return { iceServers: navigator.onLine ? [{ urls: 'stun:stun.l.google.com:19302' }] : [] };
  }

  // ── Code encoding: JSON → deflate-raw → base64url ─────────────────────────
  function b64url(bytes) {
    var s = ''; for (var i = 0; i < bytes.length; i++) s += String.fromCharCode(bytes[i]);
    return btoa(s).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  }
  function unb64url(t) {
    t = t.replace(/-/g, '+').replace(/_/g, '/'); while (t.length % 4) t += '=';
    var s = atob(t), a = new Uint8Array(s.length);
    for (var i = 0; i < s.length; i++) a[i] = s.charCodeAt(i);
    return a;
  }
  function pipe(bytes, stream) {
    return new Response(new Blob([bytes]).stream().pipeThrough(stream)).arrayBuffer()
      .then(function (b) { return new Uint8Array(b); });
  }
  var canZip = typeof CompressionStream !== 'undefined' && typeof DecompressionStream !== 'undefined';
  function encode(kind, obj) {
    var raw = new TextEncoder().encode(JSON.stringify(obj));
    if (!canZip) return Promise.resolve('NV' + kind + '0.' + b64url(raw));
    return pipe(raw, new CompressionStream('deflate-raw')).then(function (z) { return 'NV' + kind + '1.' + b64url(z); });
  }
  function decode(code) {
    var m = /^NV([OA])([01])\.(.+)$/.exec((code || '').trim());
    if (!m) return Promise.reject(new Error('not a Navigator code'));
    var bytes = unb64url(m[3]);
    var p = m[2] === '1' ? pipe(bytes, new DecompressionStream('deflate-raw')) : Promise.resolve(bytes);
    return p.then(function (b) { var o = JSON.parse(new TextDecoder().decode(b)); o.kind = m[1]; return o; });
  }

  // Non-trickle ICE: wait until candidates are gathered (or a short timeout) so one code is enough
  function gathered(pc) {
    return new Promise(function (resolve) {
      if (pc.iceGatheringState === 'complete') return resolve();
      var done = false, finish = function () { if (!done) { done = true; resolve(); } };
      pc.addEventListener('icegatheringstatechange', function () { if (pc.iceGatheringState === 'complete') finish(); });
      setTimeout(finish, 2500);
    });
  }
  // Keep only what the connection needs: shorter code, denser-free QR
  function slim(sdp) {
    // TCP candidates are useless for a local data channel and make the QR much denser
    return sdp.split('\r\n').filter(function (l) {
      return l && !/^a=(extmap-allow-mixed|msid-semantic)/.test(l) && !/^a=candidate:\S+ \d+ tcp /i.test(l);
    }).join('\r\n') + '\r\n';
  }

  // ── Links and relay ───────────────────────────────────────────────────────
  function wire(ch, remoteHint) {
    var peer = remoteHint || null;
    ch.onopen = function () { ch.send(JSON.stringify({ h: 1, f: me })); };
    ch.onmessage = function (e) {
      var env; try { env = JSON.parse(e.data); } catch (x) { return; }
      if (env.h) {                       // hello: who is on the other end of this link
        peer = env.f; links[peer] = ch; emit('peer', peer, '');
        return;
      }
      route(env, ch);
    };
    ch.onclose = function () {
      if (peer && links[peer] === ch) { delete links[peer]; emit('gone', peer, ''); }
    };
  }

  function remember(id) {
    if (seen[id]) return false;
    seen[id] = 1; seenOrder.push(id);
    if (seenOrder.length > 2000) delete seen[seenOrder.shift()];
    return true;
  }

  function route(env, fromCh) {
    if (!env || !env.m || !remember(env.m)) return;           // duplicate or malformed
    if (!env.t || env.t === me) {
      if (env.f !== me) emit('msg', env.f, env.d);
      if (env.t === me) return;
    }
    var raw = JSON.stringify(env);
    if (env.t && links[env.t]) { if (links[env.t] !== fromCh) safeSend(links[env.t], raw); return; }
    for (var id in links) if (links[id] !== fromCh) safeSend(links[id], raw);
  }

  function safeSend(ch, raw) { try { if (ch.readyState === 'open') ch.send(raw); } catch (x) {} }

  function newPc() {
    var pc = new RTCPeerConnection(config());
    pc.addEventListener('connectionstatechange', function () {
      if (pc.connectionState === 'failed') emit('error', 'connection failed', '');
    });
    return pc;
  }

  return {
    'init': function (onEvent) {
      if (!me) me = rid(8);
      emit = onEvent || emit;
      return me;
    },
    'id': function () { return me; },
    'peers': function () { return Object.keys(links); },

    // Host: a fresh offer code for the next guest
    'makeOffer': function () {
      var pc = newPc(), oid = rid(4);
      var ch = pc.createDataChannel('nav', { ordered: true });
      wire(ch);
      pending[oid] = pc;
      return pc.createOffer()
        .then(function (o) { return pc.setLocalDescription(o); })
        .then(function () { return gathered(pc); })
        .then(function () { return encode('O', { i: oid, f: me, s: slim(pc.localDescription.sdp) }); })
        .then(function (code) { emit('code', code, 'offer'); return code; });
    },

    // Scanned code: an offer (we answer it) or an answer to one of our offers (we connect)
    'accept': function (code) {
      return decode(code).then(function (o) {
        if (o.f === me) throw new Error('that is my own code');
        if (o.kind === 'O') {
          var pc = newPc();
          pc.ondatachannel = function (e) { wire(e.channel, o.f); };
          return pc.setRemoteDescription({ type: 'offer', sdp: o.s })
            .then(function () { return pc.createAnswer(); })
            .then(function (a) { return pc.setLocalDescription(a); })
            .then(function () { return gathered(pc); })
            .then(function () { return encode('A', { i: o.i, f: me, s: slim(pc.localDescription.sdp) }); })
            .then(function (ans) { emit('code', ans, 'answer'); return ans; });
        }
        var host = pending[o.i];
        if (!host) throw new Error('this answer is for another offer');
        delete pending[o.i];
        return host.setRemoteDescription({ type: 'answer', sdp: o.s }).then(function () { return ''; });
      }).catch(function (e) { emit('error', String(e && e.message || e), ''); return ''; });
    },

    'broadcast': function (data) { route({ f: me, m: me + ':' + (++counter), d: data }, null); },
    'send': function (to, data)  { route({ f: me, t: to, m: me + ':' + (++counter), d: data }, null); }
  };
})();
if (typeof window !== 'undefined') window['NavP2P'] = NavP2P;
