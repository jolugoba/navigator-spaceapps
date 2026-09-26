// Navigator 2.0 — ShipTools.cs
// The ship's two tools, both aimed with the reticle (where the ship points):
//   SCAN  — "what is that?": identifies the asteroid, pod, planet, rival or nebula in your sights
//           and explains it. The first time you scan each kind of thing it's a discovery (+points).
//           Scanning a classmate's ship tells them you waved.
//   FIRE  — a laser bolt. It never destroys anything, but a hit on a rival works like a crash:
//           it costs them one hull point (a shield or Burnout blocks it). Three hits and they're out.
//           Every shot spends a little propellant: firing is a trade-off, not free.
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Navigator.Core;
using Navigator.Network;

namespace Navigator.Game
{
    public struct ScanInfo
    {
        public string Kind, Id, Title, Body;
        public bool   Discovery;   // first time this kind of thing was scanned
    }

    public class ShipTools : MonoBehaviour
    {
        [Header("Scanner")]
        public float ScanRange = 1500f;
        public float ConeDeg   = 14f;

        [Header("Laser")]
        public float FireRange     = 700f;
        public float FireCooldown  = 1f;
        public float FireFuelCost  = 0.015f;   // 1.5 % of the tank per shot
        public float BoltSpeed     = 900f;
        public Material   GlowMaterial;
        public GameObject ImpactFX;

        public const int DiscoveryPoints = 50;

        // A teacher can turn weapons off for the class
        public static bool WeaponsAllowed = true;

        public static event Action<ScanInfo> Scanned;
        public static event Action<string>   FiredAt;      // name of what was hit (null = missed)

        public Scannable Target { get; private set; }
        public string    TargetName { get; private set; }   // short name for the reticle
        public float Cooldown01 => FireCooldown <= 0f ? 0f : Mathf.Clamp01(_cooldown / FireCooldown);

        private float _cooldown;
        private ShipController _ship;
        private FuelSystem     _fuel;
        private SendaBridge    _senda;
        private RemoteShipManager _remotes;
        private static readonly HashSet<string> _discovered = new();

        void Awake()
        {
            _ship = GetComponent<ShipController>();
            _fuel = GetComponent<FuelSystem>();
        }

        // Called at the start of every flight
        public static void ResetDiscoveries() => _discovered.Clear();

        // Set by GameManager together with the flight controls
        [HideInInspector] public bool ControlsEnabled;
        bool Active => ControlsEnabled;

        void Update()
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (!Active) { Target = null; return; }
            var t = PickTarget();
            if (t != Target) TargetName = t != null ? Describe(t).Title : null;
            Target = t;

            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if ((kb != null && kb.cKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame)) Scan();
            if ((kb != null && kb.xKey.wasPressedThisFrame) || (pad != null && pad.rightStickButton.wasPressedThisFrame)) Fire();
        }

        // ── Aim ───────────────────────────────────────────────────────────────
        Scannable PickTarget()
        {
            Vector3 origin = transform.position, dir = transform.forward;
            Scannable best = null;
            float bestScore = float.MaxValue;
            foreach (var s in Scannable.All)
            {
                if (s == null || s.transform == transform) continue;
                Vector3 v = s.transform.position - origin;
                float dist = v.magnitude;
                if (dist < 0.01f) continue;
                if (!s.AtInfinity && dist > ScanRange) continue;

                // Allow a bit more angle for big or close things
                float angle = Vector3.Angle(dir, v);
                float slack = s.AtInfinity ? 0f : Mathf.Atan2(s.Radius, dist) * Mathf.Rad2Deg;
                if (angle > ConeDeg + slack) continue;

                // Prefer what's centred, then what's near; things in the sky only if nothing else
                float score = angle + (s.AtInfinity ? 60f : dist * 0.004f);
                if (score < bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        // ── Scan ──────────────────────────────────────────────────────────────
        public void Scan()
        {
            if (!Active) return;
            AudioFX.Play(Sfx.Scan);
            if (Target == null)
            {
                Scanned?.Invoke(new ScanInfo { Kind = "none", Title = Loc.T("scan.none.t"), Body = Loc.T("scan.none") });
                return;
            }

            var info = Describe(Target);
            string key = info.Kind + ":" + (info.Kind == "ship" ? "ship" : info.Id);
            info.Discovery = _discovered.Add(key) && info.Kind != "ship";
            Scanned?.Invoke(info);

            if (info.Kind == "ship") SendTo(Target.Id, "{\"scan\":1");
        }

        ScanInfo Describe(Scannable s)
        {
            var info = new ScanInfo { Kind = s.Kind, Id = s.Id };
            switch (s.Kind)
            {
                case "asteroid":
                    info.Title = Loc.T("scan.asteroid.t");
                    info.Body  = Loc.T("scan.asteroid");
                    break;
                case "pod":
                    info.Title = Loc.T("podname." + s.Id);
                    info.Body  = Loc.T("scan.pod." + s.Id);
                    break;
                case "planet":
                case "flyby":
                    info.Title = Loc.Planet(s.Id);
                    info.Body  = Loc.T("scan.planet." + s.Id) + (s.Kind == "flyby" ? "\n" + Loc.T("scan.flyby") : "");
                    break;
                case "probe":
                case "moon":
                    info.Title = Loc.T($"scan.{s.Kind}.{s.Id}.t");
                    info.Body  = Loc.T($"scan.{s.Kind}.{s.Id}");
                    break;
                case "ship":
                    _remotes ??= FindFirstObjectByType<RemoteShipManager>();
                    string name = _remotes != null ? _remotes.NameOf(s.Id) : "Rival";
                    int lives = 3; float fuel = 1f;
                    _remotes?.TryGetStatus(s.Id, out lives, out fuel);
                    info.Title = name;
                    info.Body  = Loc.F("scan.ship", Mathf.Max(0, lives), Mathf.RoundToInt(fuel * 100f));
                    break;
                case "sky":
                    string kind = s.Id.StartsWith("nebula") ? "nebula" : s.Id.StartsWith("galaxy") ? "galaxy" : "blackhole";
                    info.Title = Loc.Has("scan.sky." + s.Id + ".t") ? Loc.T("scan.sky." + s.Id + ".t") : Loc.T("scan.sky." + kind + ".t");
                    info.Body  = Loc.Has("scan.sky." + s.Id)        ? Loc.T("scan.sky." + s.Id)        : Loc.T("scan.sky." + kind);
                    break;
                default:
                    info.Title = s.Id;
                    info.Body  = "";
                    break;
            }
            return info;
        }

        // ── Fire ──────────────────────────────────────────────────────────────
        public void Fire()
        {
            if (!Active || !WeaponsAllowed || _cooldown > 0f) return;
            if (_fuel != null && _fuel.IsEmpty) return;

            _cooldown = FireCooldown;
            _fuel?.Leak(FireFuelCost);
            AudioFX.Play(Sfx.Laser, 0.7f);

            // Mild auto-aim: the bolt flies at whatever is in the reticle and within range
            Scannable target = Target != null && !Target.AtInfinity &&
                               Vector3.Distance(Target.transform.position, transform.position) <= FireRange ? Target : null;
            Vector3 from = transform.position + transform.forward * 3f;
            Vector3 to   = target != null ? target.transform.position : from + transform.forward * FireRange;
            StartCoroutine(Bolt(from, to, target));
        }

        IEnumerator Bolt(Vector3 from, Vector3 to, Scannable target)
        {
            var go = new GameObject("LaserBolt");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.widthMultiplier = 0.45f;
            lr.useWorldSpace = true;
            if (GlowMaterial != null) lr.sharedMaterial = GlowMaterial;
            lr.startColor = new Color(1f, 0.35f, 0.25f, 1f);
            lr.endColor   = new Color(1f, 0.85f, 0.4f, 0.2f);

            float total = Vector3.Distance(from, to), travelled = 0f;
            while (travelled < total)
            {
                // Follow a moving target so the hit lands where it is now
                if (target != null) to = target.transform.position;
                total = Vector3.Distance(from, to);
                travelled += BoltSpeed * Time.deltaTime;
                Vector3 dir = (to - from).normalized;
                Vector3 head = from + dir * Mathf.Min(travelled, total);
                lr.SetPosition(0, head);
                lr.SetPosition(1, head - dir * Mathf.Min(10f, travelled));
                yield return null;
            }
            Destroy(go);

            if (target == null) { FiredAt?.Invoke(null); yield break; }
            if (ImpactFX != null) Instantiate(ImpactFX, to, Quaternion.identity);
            AudioFX.Play(Sfx.Hit, 0.6f);

            if (target.Kind == "ship")
            {
                SendTo(target.Id, "{\"laser\":1");
                _remotes ??= FindFirstObjectByType<RemoteShipManager>();
                FiredAt?.Invoke(_remotes != null ? _remotes.NameOf(target.Id) : "Rival");
            }
            else
            {
                // Rocks and pods shrug it off: the laser doesn't destroy the environment
                FiredAt?.Invoke(Describe(target).Title);
            }
        }

        // ── Network ───────────────────────────────────────────────────────────
        // Events for one ship carry "to"; the receiver's GameManager applies them
        void SendTo(string nodeId, string jsonStart)
        {
            _senda ??= FindFirstObjectByType<SendaBridge>();
            if (_senda == null || !_senda.Initialized) return;
            string me = _ship != null && !string.IsNullOrEmpty(_ship.DisplayName) ? _ship.DisplayName : "Rival";
            _senda.Broadcast(NavMsg.Build(NavMsgType.shipEvent, _senda.NodeId,
                jsonStart + ",\"to\":\"" + nodeId + "\",\"name\":\"" + me.Replace("\"", "'") + "\"}"));
        }
    }
}
