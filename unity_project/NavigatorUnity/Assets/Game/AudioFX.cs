// Navigator 2.0 — AudioFX.cs
// All game sound, synthesized at startup (no audio files needed):
// engine hum that follows thrust, explosions, hits, pickups, alerts, near-misses, Burnout, countdown.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Game
{
    public enum Sfx { Explosion, Hit, Pickup, Alert, NearMiss, Burnout, Beep, Go, Laser, Scan }

    public class AudioFX : MonoBehaviour
    {
        [Range(0f, 1f)] public float Volume       = 0.8f;
        [Range(0f, 1f)] public float EngineVolume = 0.45f;

        const int Rate = 44100;
        static AudioFX _instance;

        private readonly Dictionary<Sfx, AudioClip> _clips = new();
        private AudioSource _oneShots, _engine;
        private ShipController _ship;
        private BurnoutSystem  _burnout;
        private System.Random  _rng = new(7);

        public static void Play(Sfx sfx, float volume = 1f)
        {
            if (_instance == null || !_instance._clips.TryGetValue(sfx, out var clip)) return;
            _instance._oneShots.PlayOneShot(clip, volume * _instance.Volume);
        }

        void Awake()
        {
            _instance = this;
            _oneShots = gameObject.AddComponent<AudioSource>();
            _oneShots.playOnAwake = false;

            _engine = gameObject.AddComponent<AudioSource>();
            _engine.clip   = EngineLoop();
            _engine.loop   = true;
            _engine.volume = 0f;
            _engine.Play();

            _clips[Sfx.Explosion] = Explosion(1.3f, 45f);
            _clips[Sfx.Hit]       = Explosion(0.35f, 120f);
            _clips[Sfx.Pickup]    = Chirp(new[] { 660f, 880f, 1320f }, 0.09f);
            _clips[Sfx.Alert]     = Chirp(new[] { 880f, 660f, 880f, 660f }, 0.13f);
            _clips[Sfx.NearMiss]  = Whoosh(0.55f);
            _clips[Sfx.Burnout]   = Sweep(0.9f, 180f, 1100f);
            _clips[Sfx.Beep]      = Chirp(new[] { 880f }, 0.14f);
            _clips[Sfx.Go]        = Chirp(new[] { 1320f }, 0.45f);
            _clips[Sfx.Laser]     = Sweep(0.18f, 1800f, 300f);
            _clips[Sfx.Scan]      = Chirp(new[] { 1200f, 1500f, 1800f, 2400f }, 0.05f);
        }

        void Update()
        {
            if (_ship == null)
            {
                var go = GameObject.FindWithTag("LocalShip");
                if (go == null) return;
                _ship    = go.GetComponent<ShipController>();
                _burnout = go.GetComponent<BurnoutSystem>();
                if (_ship == null) return;
            }
            bool burn   = _burnout != null && _burnout.BurnoutActive;
            float thrust = _ship.ThrustMain;
            float targetVol   = thrust > 0.01f ? EngineVolume * (0.6f + 0.4f * thrust) + (_ship.Boosting ? 0.15f : 0f) : 0f;
            float targetPitch = 0.75f + 0.35f * thrust + (_ship.Boosting ? 0.3f : 0f) + (burn ? 0.25f : 0f);
            _engine.volume = Mathf.MoveTowards(_engine.volume, targetVol * Volume, 2.5f * Time.deltaTime);
            _engine.pitch  = Mathf.MoveTowards(_engine.pitch, targetPitch, 1.5f * Time.deltaTime);
        }

        // ── Synthesis ─────────────────────────────────────────────────────────

        float Noise() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Rumble: low-passed brown noise plus two sines that loop seamlessly over one second
        AudioClip EngineLoop()
        {
            int n = Rate;
            var d = new float[n];
            float brown = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                brown = Mathf.Clamp(brown + Noise() * 0.02f, -1f, 1f);
                lp += (brown - lp) * 0.08f;
                float t = (float)i / Rate;
                d[i] = lp * 0.6f + 0.25f * Mathf.Sin(2f * Mathf.PI * 55f * t) + 0.12f * Mathf.Sin(2f * Mathf.PI * 110f * t);
            }
            // Cross-fade the tail into the head so the loop has no click
            int fade = Rate / 20;
            for (int i = 0; i < fade; i++)
            {
                float a = (float)i / fade;
                d[i] = d[i] * a + d[n - fade + i] * (1f - a);
            }
            System.Array.Resize(ref d, n - fade);
            return Make("engine", d);
        }

        AudioClip Explosion(float seconds, float thumpHz)
        {
            int n = (int)(Rate * seconds);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Exp(-5f * t);
                lp += (Noise() - lp) * Mathf.Lerp(0.5f, 0.03f, t);   // darker as it fades
                d[i] = (lp * 0.9f + 0.6f * Mathf.Sin(2f * Mathf.PI * thumpHz * i / Rate) * Mathf.Exp(-9f * t)) * env;
            }
            return Make("explosion", d);
        }

        AudioClip Chirp(float[] notes, float noteSeconds)
        {
            int per = (int)(Rate * noteSeconds);
            var d = new float[per * notes.Length];
            for (int k = 0; k < notes.Length; k++)
            for (int i = 0; i < per; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, i / 200f) * Mathf.Exp(-4f * i / per);
                float w = Mathf.Sin(2f * Mathf.PI * notes[k] * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[k] * t);
                d[k * per + i] = 0.5f * w * env;
            }
            return Make("chirp", d);
        }

        AudioClip Whoosh(float seconds)
        {
            int n = (int)(Rate * seconds);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Sin(Mathf.PI * t);                 // swell in, swell out
                lp += (Noise() - lp) * Mathf.Lerp(0.05f, 0.4f, t);   // pitch rises as it passes
                d[i] = lp * env * 0.9f;
            }
            return Make("whoosh", d);
        }

        AudioClip Sweep(float seconds, float from, float to)
        {
            int n = (int)(Rate * seconds);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(from, to, t * t);
                phase += 2f * Mathf.PI * f / Rate;
                float saw = 2f * (phase / (2f * Mathf.PI) % 1f) - 1f;
                d[i] = (0.35f * saw + 0.2f * Noise()) * Mathf.Sin(Mathf.PI * t);
            }
            return Make("sweep", d);
        }
    }
}
