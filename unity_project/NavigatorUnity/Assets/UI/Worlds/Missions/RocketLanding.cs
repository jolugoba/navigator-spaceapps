// Navigator 2.0 — RocketLanding.cs
// Landing with rockets only, for worlds with no air to brake (Mercury, Triton): kill the
// sideways speed from orbit with the engine, put the legs down, touch down gently.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public abstract class RocketLandingMission : WorldMission
    {
        protected GameObject Lander;
        protected DescentCraft Craft;
        protected bool Legs;
        protected Vector2 PadLocal;
        protected float PadRadius = 14f;
        ParticleSystem _flame;
        GameObject _padMark;

        protected GameObject SpawnVehicle(GameObject model, string shape, float size, Vector3 at, bool alignLong = false)
        {
            GameObject go = model != null
                ? A.Spawn(model, PrimitiveType.Cube, Vector3.one, Color.white, size, Root, alignLong)
                : VehicleShapes.Build(A, shape, Root);
            go.transform.position = at;
            return go;
        }

        protected void StartRocketLanding(Vector3 start, Vector3 velocity, float thrustAcc, float fuelSeconds)
        {
            Lander = SpawnVehicle(A.Lander, "lander", 3.2f, start);
            _flame = Flame(Lander.transform, Vector3.zero);
            Craft = new DescentCraft
            {
                T = Lander.transform, Terrain = Ter, Gravity = P.Gravity, AirDensity = P.AirDensity,
                MaxThrustAcc = thrustAcc, TiltMax = 30f, Vel = velocity,
            };
            Craft.FuelMax = Craft.Fuel = DescentFuel(fuelSeconds);
            _padMark = WorldAssets.Primitive(PrimitiveType.Cylinder, Root, A.NewMaterial(A.PropMaterial, new Color(0.3f, 1f, 0.5f), 0.2f));
            _padMark.transform.position = Ter.Surface(PadLocal) + Vector3.up * 0.15f;
            _padMark.transform.localScale = new Vector3(PadRadius * 2f, 0.05f, PadRadius * 2f);
            D.Focus = Lander.transform;
            Cam.Target = Lander.transform; Cam.Distance = 16f; Cam.Height = 7f; Cam.Pitch = 26f; Cam.AutoFollow = false;
            Cam.Yaw = Quaternion.LookRotation(new Vector3(velocity.x, 0f, velocity.z) + Vector3.forward * 0.01f).eulerAngles.y;
            Cam.SnapBehind();
            Legs = false;
            H.SetPrompt(T("w.rocket.hint"));
            H.SetButton(H.BtnA, T("w.btn.thrust"), BtnState.On);
            H.SetButton(H.BtnB, T("w.btn.legs"), BtnState.Pulse);
            H.SetButton(H.BtnC, null, BtnState.Hidden);
            Go("landing");
        }

        // True once safely down; fails the mission on a bad touchdown
        protected bool TickRocketLanding(float dt)
        {
            Craft.Control(In.MoveV, Cam.Yaw, AHeld, dt);
            Craft.Step(dt);
            SetFlame(_flame, Craft.Throttle);
            DescentMeters(Craft);
            H.SetFill(H.BtnA, Craft.Fuel / Craft.FuelMax);
            H.SetArrow(Ter.Surface(PadLocal), T("w.pad"));
            if (BPressed && !Legs)
            {
                Legs = true;
                AudioFX.Play(Sfx.Beep);
                H.SetButton(H.BtnB, T("w.btn.legs"), BtnState.Hidden);
                H.ShowBanner(T("w.legs.down"), UIStyle.Good, 1.5f);
            }
            if (Craft.Altitude < 150f && !Legs) H.SetPrompt(T("w.legs.hint"), 0.3f);
            if (Craft.Fuel <= 0f && Craft.Altitude > 3f) H.SetPrompt(T("w.nofuel"), 0.3f);

            if (Craft.Altitude > 0.3f) return false;
            string problem = Craft.TouchdownProblem(legsDown: Legs);
            if (problem != null) { Fail(problem); return false; }
            var p = Lander.transform.position;
            p.y = Ter.Height(p);
            Lander.transform.position = p;
            Lander.transform.rotation = Quaternion.Euler(0f, Craft.Yaw, 0f);
            SetFlame(_flame, 0f);
            Result.Landed = true;
            Score(1000, "w.log.landed", Craft.TouchdownQuality() * 100f);
            float d = new Vector2(p.x - Ter.Center.x - PadLocal.x, p.z - Ter.Center.z - PadLocal.y).magnitude;
            if (d < PadRadius) Score(300, "w.log.pad");
            Score(Mathf.RoundToInt(Craft.Fuel / Craft.FuelMax * 300f), "w.log.fuelleft", Craft.Fuel / Craft.FuelMax * 100f);
            if (_padMark != null) Object.Destroy(_padMark);
            H.SetLanding(false, 0, 0, 0);
            Haptics.Buzz();
            AudioFX.Play(Sfx.Hit, 0.6f);
            return true;
        }
    }
}
