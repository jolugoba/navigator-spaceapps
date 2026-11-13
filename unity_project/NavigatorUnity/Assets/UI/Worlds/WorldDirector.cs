// Navigator 2.0 — WorldDirector.cs
// Opens a world when a landing mission arrives, runs its mission and closes it again.
// While a world is open, the flight camera rig, the AR camera feed and the space dust are off;
// the camera is driven by WorldCamera and everything is built far below the flight scene.
// Guacalita Inc. — SAL / Claude

using System;
using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public class WorldDirector : MonoBehaviour
    {
        public static readonly Vector3 WorldCenter = new(0f, -30000f, 0f);

        [Header("Scene refs (Setup Scene)")]
        public WorldHUD    Hud;
        public Camera      Cam;
        public Light       Sun;
        public WorldAssets Assets = new();
        public MonoBehaviour[] DisableWhileOpen;   // flight camera rig, space dust…

        public WorldInput       Input { get; private set; }
        public WorldEnvironment Env { get; private set; }
        public WorldCamera      WorldCam { get; private set; }
        public Transform        Focus;            // the vehicle being driven (for distances)

        public bool IsOpen { get; private set; }

        WorldMission _mission;
        WorldProfile _profile;
        MissionDesign _design;
        MissionOutcome _flight;
        Action<WorldResult> _done;
        bool _resultShown;
        int _attempts;

        // Only landing missions open a world (every planet has one: the giants via probe/moon)
        public static bool HasWorld(MissionDesign d) => d != null && d.IsLanding && WorldProfile.Get(d.TargetPlanetId) != null;

        void Awake()
        {
            if (Hud != null)
            {
                Input = new WorldInput { Move = Hud.MoveStick, Look = Hud.LookStick, A = Hud.BtnA?.Hold, B = Hud.BtnB?.Hold, C = Hud.BtnC?.Hold };
                Hud.ExitButton?.onClick.AddListener(OnExit);
                Hud.CheckpointButton?.onClick.AddListener(OnCheckpoint);
                Hud.RetryButton?.onClick.AddListener(Retry);
                Hud.ContinueButton?.onClick.AddListener(Close);
                Hud.Show(false);
            }
            else Input = new WorldInput();
        }

        public void Open(MissionDesign design, MissionOutcome flight, Func<string, GameObject> skyBodies, Action<WorldResult> done)
        {
            _design = design; _flight = flight; _done = done;
            _profile = WorldProfile.Get(design.TargetPlanetId);
            Assets.SkyBodyPrefab = skyBodies;
            _attempts = 0;
            WorldFX.Glow = Assets.GlowMaterial;
            WorldFX.Dust = Assets.DustMaterial;
            if (Cam == null) Cam = Camera.main;
            WorldCam = Cam.GetComponent<WorldCamera>();
            if (WorldCam == null) WorldCam = Cam.gameObject.AddComponent<WorldCamera>();
            foreach (var b in DisableWhileOpen) if (b != null) b.enabled = false;
            IsOpen = true;
            if (Hud != null) { Hud.Cam = Cam; Hud.Show(true); }
            StartAttempt();
            Hud?.Flash(Color.white, 1f);   // the jump from space into the world
            Navigator.Game.AudioFX.Play(Navigator.Game.Sfx.Go, 0.7f);
        }

        void StartAttempt()
        {
            _attempts++;
            _resultShown = false;
            WorldFX.ResetTime();
            Hud?.ResetAll();
            _mission = Create(_profile.Planet);
            Env = new WorldEnvironment(Cam, Sun, Assets);
            _mission.Init(this, _profile, _design, _flight);
            Env.Build(_profile, WorldCenter, _mission.Shape);
            WorldCam.Terrain = Env.Terrain;
            WorldCam.enabled = true;
            WorldCam.MinPitch = -20f; WorldCam.MaxPitch = 75f; WorldCam.LookAtHeight = 1f; WorldCam.AutoFollow = true;
            Cam.nearClipPlane = 0.2f;
            _mission.Begin();
        }

        static WorldMission Create(string planet) => planet switch
        {
            "mercury" => new MercuryMission(),
            "venus"   => new VenusMission(),
            "mars"    => new MarsMission(),
            "jupiter" => new JupiterMission(),
            "saturn"  => new TitanMission(),
            "uranus"  => new UranusMission(),
            _         => new TritonMission(),
        };

        void Update()
        {
            if (!IsOpen || _mission == null) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);   // big hitches must not tunnel through the ground
            _mission.Update(dt);
            if (WorldCam != null && !_resultShown) WorldCam.Look(Input.LookV, Time.deltaTime);
            Env?.Update();
            if (_mission.Finished && !_resultShown) ShowResult();

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) OnExit();
        }

        void ShowResult()
        {
            _resultShown = true;
            var r = _mission.Result;
            var sb = new System.Text.StringBuilder();
            int from = Mathf.Max(0, r.Lines.Count - 7);   // the last lines fit on the card
            for (int i = from; i < r.Lines.Count; i++) sb.Append("• ").Append(r.Lines[i]).Append('\n');
            sb.Append('\n').Append(Loc.F("w.result.points", r.Points));
            string title = r.Success ? Loc.T("w.result.ok." + _profile.Body) : Loc.T("w.result.fail");
            Hud?.SetButton(Hud.BtnA, null, BtnState.Hidden);
            Hud?.SetButton(Hud.BtnB, null, BtnState.Hidden);
            Hud?.SetButton(Hud.BtnC, null, BtnState.Hidden);
            Hud?.SetArrow(null, null);
            Hud?.ShowResult(r.Success, title, sb.ToString(), r.Stars, true);
            Hud?.SetCinematic(true);

            // Celebrate, or show what went wrong
            var at = Focus != null ? Focus.position : WorldCenter;
            if (r.Success)
            {
                StartCoroutine(WorldFX.Fireworks(Env.Root.transform, at));
                Hud?.Flash(new Color(1f, 0.9f, 0.5f), 0.5f);
            }
            else if (r.FailKey != null && CrashFail(r.FailKey))
            {
                WorldFX.Explosion(Env.Root.transform, at + Vector3.up);
                WorldCam?.Shake(1.2f);
                WorldCam?.Punch(12f);
                Hud?.Flash(new Color(1f, 0.3f, 0.2f), 1f);
                WorldFX.SlowMo(this, 0.3f, 0.8f);
            }
            else Hud?.Flash(new Color(1f, 0.3f, 0.2f), 0.6f);
        }

        static bool CrashFail(string key) => key switch
        {
            "w.fail.frozen" or "w.fail.acid" or "w.fail.overheat" or "w.fail.lake" => false,
            _ => true,
        };

        void OnExit()
        {
            if (!IsOpen) return;
            if (_resultShown) { Close(); return; }
            _mission?.Abort(_mission.StarsSoFar);
        }

        // Safety net for a vehicle that ended up somewhere it can't leave: on the surface it goes
        // back to where it was deployed (samples and points are kept); during the descent there
        // is nothing to go back to, so the descent starts again
        void OnCheckpoint()
        {
            if (!IsOpen || _resultShown || _mission == null || _mission.Finished) return;
            Navigator.Game.AudioFX.Play(Navigator.Game.Sfx.Beep, 0.6f);
            if (_mission.Recover())
            {
                Hud?.Flash(Color.white, 0.5f);
                Hud?.ShowBanner(Loc.T("w.checkpoint.back"), UIStyle.Accent, 3f);
                return;
            }
            StopAllCoroutines();
            Env?.Teardown();
            StartAttempt();
            Hud?.ShowBanner(Loc.T("w.checkpoint.restart"), UIStyle.Accent, 3f);
        }

        void Retry()
        {
            if (!IsOpen) return;
            StopAllCoroutines();
            Env?.Teardown();
            StartAttempt();
        }

        void Close()
        {
            if (!IsOpen) return;
            var result = _mission != null ? _mission.Result : new WorldResult();
            StopAllCoroutines();
            WorldFX.ResetTime();
            Env?.Teardown();
            Env = null;
            _mission = null;
            IsOpen = false;
            if (WorldCam != null) WorldCam.enabled = false;
            if (Cam != null) Cam.nearClipPlane = 0.3f;
            foreach (var b in DisableWhileOpen) if (b != null) b.enabled = true;
            Hud?.Show(false);
            if (_attempts > 1) result.Lines.Add(Loc.F("w.log.attempts", _attempts));
            _done?.Invoke(result);
        }
    }
}
