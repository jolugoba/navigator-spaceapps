// Navigator 2.0 — NavP2P.jspre (web build only)
// Serverless peer-to-peer for the browser, replacing the native Iroh library:
//   · WebRTC data channels, no signalling server. Two phones connect by swapping two QR codes:
//     the host shows an OFFER code, the guest scans it and shows an ANSWER code, the host scans
//     that. It works on the same Wi-Fi (or a phone hotspot) with no internet at all; when online,
//     a public STUN server is added so phones on different networks can often connect too.
//   · Star with relay: every message carries its origin ("f") and an id ("m"); each node delivers
//     it and forwards it to its other links, so a guest reaches every player through the host.
//   · Codes are packed in binary (v2, ~150 chars → a small, easy QR); the full deflated SDP
//     (v1) is the fallback for anything unusual.
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

  // ── Compact codes (v2): only what WebRTC needs, packed in binary → ~150 chars, a small QR ──
  // ids, ICE user/password, the DTLS fingerprint, setup role, mid and the UDP candidates
  // (mDNS name, IPv4 or IPv6 + port). The other side rebuilds a standard data-channel SDP.
  // Anything unusual (another hash, TCP-only…) falls back to the full v1 code.
  function hexBytes(h) { var a = []; for (var i = 0; i < h.length; i += 2) a.push(parseInt(h.substr(i, 2), 16)); return a; }
  function bytesHex(a) { return a.map(function (b) { return ('0' + b.toString(16)).slice(-2); }).join(''); }
  function packStr(out, str) { if (str.length > 255) throw 0; out.push(str.length); for (var i = 0; i < str.length; i++) out.push(str.charCodeAt(i) & 255); }

  function parseSdp(sdp) {
    var g = function (re) { var m = re.exec(sdp); return m ? m[1] : null; };
    var p = { u: g(/a=ice-ufrag:(\S+)/), pw: g(/a=ice-pwd:(\S+)/), mid: g(/a=mid:(\S+)/) || '0',
              setup: g(/a=setup:(\S+)/), fp: g(/a=fingerprint:sha-256 ([0-9A-Fa-f:]+)/),
              port: parseInt(g(/a=sctp-port:(\d+)/) || '5000', 10), c: [] };
    if (!p.u || !p.pw || !p.fp || !p.setup) return null;
    var re = /a=candidate:\S+ \d+ udp \d+ (\S+) (\d+) typ (host|srflx)/gi, m;
    while ((m = re.exec(sdp))) p.c.push({ a: m[1], port: parseInt(m[2], 10), t: m[3].toLowerCase() });
    return p;
  }

  function compact(kind, obj) {
    try {
      var p = parseSdp(obj.s);
      if (!p || !/^[0-9a-f]{8}$/.test(obj.i) || !/^[0-9a-f]{16}$/.test(obj.f)) return null;
      var fp = hexBytes(p.fp.replace(/:/g, ''));
      if (fp.length !== 32) return null;
      var setups = ['actpass', 'active', 'passive'], si = setups.indexOf(p.setup);
      if (si < 0) return null;
      var out = [2].concat(hexBytes(obj.i), hexBytes(obj.f));
      packStr(out, p.u); packStr(out, p.pw); packStr(out, p.mid);
      out.push(si, (p.port >> 8) & 255, p.port & 255);
      out = out.concat(fp);
      var cands = [];
      p.c.forEach(function (c) {
        var m = /^([0-9a-f]{8})-([0-9a-f]{4})-([0-9a-f]{4})-([0-9a-f]{4})-([0-9a-f]{12})\.local$/i.exec(c.a), b;
        if (m && c.t === 'host') b = [0].concat(hexBytes((m[1] + m[2] + m[3] + m[4] + m[5]).toLowerCase()));
        else if (/^\d+\.\d+\.\d+\.\d+$/.test(c.a)) b = [c.t === 'srflx' ? 3 : 1].concat(c.a.split('.').map(Number));
        else return;                                   // IPv6 and others: keep the code short, skip
        cands.push(b.concat([(c.port >> 8) & 255, c.port & 255]));
      });
      if (!cands.length) return null;
      out.push(Math.min(cands.length, 6));
      cands.slice(0, 6).forEach(function (b) { out = out.concat(b); });
      return 'NV' + kind + '2.' + b64url(new Uint8Array(out));
    } catch (x) { return null; }
  }

  function expand(bytes) {
    var k = 0, rd = function (n) { var r = Array.prototype.slice.call(bytes, k, k + n); k += n; if (r.length < n) throw new Error('short code'); return r; };
    var str = function () { var n = rd(1)[0]; return String.fromCharCode.apply(null, rd(n)); };
    if (rd(1)[0] !== 2) throw new Error('unknown code version');
    var o = { i: bytesHex(rd(4)), f: bytesHex(rd(8)) };
    var u = str(), pw = str(), mid = str();
    var h = rd(3), setup = ['actpass', 'active', 'passive'][h[0]], port = (h[1] << 8) | h[2];
    var fp = rd(32).map(function (b) { return ('0' + b.toString(16).toUpperCase()).slice(-2); }).join(':');
    var n = rd(1)[0], lines = [];
    for (var j = 0; j < n; j++) {
      var t = rd(1)[0], addr;
      if (t === 0) { var x = bytesHex(rd(16)); addr = x.substr(0, 8) + '-' + x.substr(8, 4) + '-' + x.substr(12, 4) + '-' + x.substr(16, 4) + '-' + x.substr(20) + '.local'; }
      else addr = rd(4).join('.');
      var pb = rd(2), cport = (pb[0] << 8) | pb[1];
      lines.push('a=candidate:' + (j + 1) + ' 1 udp ' + (t === 3 ? 1686052607 - j : 2122260223 - j) + ' ' + addr + ' ' + cport +
                 ' typ ' + (t === 3 ? 'srflx raddr 0.0.0.0 rport 0' : 'host') + ' generation 0');
    }
    o.s = ['v=0', 'o=- ' + Date.now() + ' 2 IN IP4 127.0.0.1', 's=-', 't=0 0', 'a=group:BUNDLE ' + mid,
           'm=application 9 UDP/DTLS/SCTP webrtc-datachannel', 'c=IN IP4 0.0.0.0']
          .concat(lines, ['a=ice-ufrag:' + u, 'a=ice-pwd:' + pw, 'a=ice-options:trickle', 'a=fingerprint:sha-256 ' + fp,
                          'a=setup:' + setup, 'a=mid:' + mid, 'a=sctp-port:' + port, 'a=max-message-size:262144'])
          .join('\r\n') + '\r\n';
    return o;
  }

  function encode(kind, obj) {
    var small = compact(kind, obj);
    if (small) return Promise.resolve(small);
    var raw = new TextEncoder().encode(JSON.stringify(obj));
    if (!canZip) return Promise.resolve('NV' + kind + '0.' + b64url(raw));
    return pipe(raw, new CompressionStream('deflate-raw')).then(function (z) { return 'NV' + kind + '1.' + b64url(z); });
  }
  function decode(code) {
    var m = /^NV([OA])([012])\.([A-Za-z0-9_-]+)/.exec((code || '').trim());
    if (!m) return Promise.reject(new Error('not a Navigator code'));
    var bytes = unb64url(m[3]);
    if (m[2] === '2') {
      try { var o2 = expand(bytes); o2.kind = m[1]; return Promise.resolve(o2); }
      catch (e) { return Promise.reject(e); }
    }
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
