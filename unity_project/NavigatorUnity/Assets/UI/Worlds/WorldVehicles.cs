// Navigator 2.0 — WorldVehicles.cs
// The vehicles of the worlds, with their own simple physics on top of WorldTerrain (no physics
// engine colliders, so they behave the same on phones and in the browser):
//   DescentCraft  capsule / lander / probe: gravity, air drag, parachute, wind, rocket thrust
//                 with tilt to steer, fuel, and the touchdown check (vertical, sideways, tilt)
//   GroundVehicle rover: drive, turn, slopes that slide, soft sand that traps, low-gravity hops
//   Hopper        aim, charge and jump; nitrogen jets to correct in the air
//   Drone         rotorcraft: lift, fly, land; battery drains flying and recharges landed
// Plus fallback shapes for every vehicle, used until its Meshy model is in the project.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Worlds
{
    // ═════════════════════════════════════════════════════════════════════════
    public class DescentCraft
    {
        public Transform T;
        public Vector3   Vel;
        public float Gravity;
        public float AirDensity;            // at the ground
        public float ScaleHeight = 600f;    // metres: the air thins with height (game scale)
        public float Wind;                  // m/s sideways at the top, fading to ~0 at the ground
        public Vector3 WindDir = Vector3.right;
        public float BodyCdA = 3f, ChuteCdA = 0f, Mass = 1000f;
        public float MaxThrustAcc = 9f;     // m/s² at full throttle
        public float Fuel = 40f;            // seconds of full thrust
        public float FuelMax = 40f;
        public float TiltMax = 25f;
        public bool  ThrustAllowed = true;
        public WorldTerrain Terrain;        // null = no ground (giants)
        public float Throttle { get; private set; }
        public Vector2 TiltDir;             // world XZ, magnitude 0..1
        public float   Yaw;                 // heading of the craft (degrees)

        public float GroundY => Terrain != null ? Terrain.SurfaceY(T.position) : float.NegativeInfinity;
        public float Altitude => Terrain != null ? T.position.y - GroundY : 99999f;
        public float VerticalSpeed => Vel.y;
        public float SideSpeed => new Vector2(Vel.x, Vel.z).magnitude;
        public float TiltDeg => Vector3.Angle(T.up, Vector3.up);

        // stick in camera space; yaw = camera yaw in degrees
        public void Control(Vector2 stick, float cameraYaw, bool thrust, float dt)
        {
            var dir = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(stick.x, 0f, stick.y);
            var target = new Vector2(dir.x, dir.z);
            TiltDir = Vector2.Lerp(TiltDir, target, 1f - Mathf.Exp(-3f * dt));
            Throttle = thrust && ThrustAllowed && Fuel > 0f ? 1f : 0f;
        }

        public float AirAt(float y)
        {
            if (AirDensity <= 0f) return 0f;
            float h = Terrain != null ? Mathf.Max(0f, y - GroundY) : Mathf.Max(0f, y);
            return AirDensity * Mathf.Exp(-h / ScaleHeight);
        }

        public void Step(float dt)
        {
            float tilt = TiltDir.magnitude * TiltMax * Mathf.Deg2Rad;
            var dirXZ = TiltDir.sqrMagnitude > 1e-4f ? new Vector3(TiltDir.x, 0f, TiltDir.y).normalized : Vector3.zero;
            Vector3 up = (Vector3.up * Mathf.Cos(tilt) + dirXZ * Mathf.Sin(tilt)).normalized;
            T.rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, Yaw, 0f);

            float rho = AirAt(T.position.y);
            float windK = Terrain != null ? Mathf.Clamp01(Altitude / 400f) : 1f;
            Vector3 air = WindDir.normalized * Wind * windK;
            Vector3 rel = Vel - air;
            Vector3 drag = rho > 0f ? -0.5f * rho * rel.magnitude * rel * (BodyCdA + ChuteCdA) / Mass : Vector3.zero;
            Vector3 acc = Vector3.down * Gravity + up * (Throttle * MaxThrustAcc) + drag;
            Vel += acc * dt;
            // Very dense air (Venus) would make explicit steps unstable: cap the drag change
            if (rho > 5f) Vel = Vector3.Lerp(Vel, air + Vector3.down * TerminalSpeed(rho), 1f - Mathf.Exp(-rho * 0.02f * dt));
            T.position += Vel * dt;
            // Wind or steering must never carry the craft off the edge of the map
            if (Terrain != null)
            {
                var c = Terrain.Clamp(T.position, 25f);
                if (c.x != T.position.x) Vel.x = 0f;
                if (c.z != T.position.z) Vel.z = 0f;
                T.position = c;
            }
            Fuel = Mathf.Max(0f, Fuel - Throttle * dt);
        }

        public float TerminalSpeed(float rho) => Mathf.Sqrt(2f * Mass * Gravity / Mathf.Max(0.01f, rho * (BodyCdA + ChuteCdA)));

        // Touchdown verdict: 0 = fine, else the reason (localization key)
        public string TouchdownProblem(float vsMax = 3f, float hsMax = 1.5f, float tiltMax = 15f, bool legsDown = true)
        {
            if (!legsDown) return "w.fail.nolegs";
            if (-Vel.y > vsMax * 1.6f) return "w.fail.crash";
            if (SideSpeed > hsMax * 2f) return "w.fail.sideways";
            if (TiltDeg > tiltMax * 1.6f) return "w.fail.tipped";
            if (Terrain != null)
            {
                var n = Terrain.Normal(T.position.x, T.position.z);
                if (Vector3.Angle(n, Vector3.up) > 22f) return "w.fail.slope";
                if (Terrain.HitsRock(T.position, 1.5f, out _)) return "w.fail.rocks";
                if (Terrain.InWater(T.position)) return "w.fail.lake";
            }
            return null;
        }

        public float TouchdownQuality(float vsMax = 3f)   // 1 = feather, 0 = barely ok
            => Mathf.Clamp01(1f - (-Vel.y) / (vsMax * 1.6f));
    }

    // ═════════════════════════════════════════════════════════════════════════
    public class GroundVehicle
    {
        public Transform T;
        public WorldTerrain Terrain;
        public float Gravity;
        public float MaxSpeed = 5f, Accel = 2.5f, TurnRate = 60f, Radius = 1.5f;
        public float Speed;
        public float Heading;
        public bool  Airborne { get; private set; }
        public bool  Stuck { get; private set; }
        public float Battery = 1f;
        public float DrainPerSecondMoving = 0.004f;
        public System.Func<Vector3, bool> IsSoftSand;
        public float SlideAngle = 25f;

        float _vy, _lastGroundY, _lastGroundVy, _unstuck;
        Vector3 _slide;
        Vector3 _up = Vector3.up;

        public void Place(Vector3 at, float heading)
        {
            Heading = heading;
            at.y = Terrain.Height(at);
            T.position = at;
            _lastGroundY = at.y;
            Speed = 0f; _vy = 0f; Airborne = false; Stuck = false; _slide = Vector3.zero;
            T.rotation = Quaternion.Euler(0f, heading, 0f);
        }

        public void Step(Vector2 move, float dt)
        {
            bool power = Battery > 0f;
            float target = power ? move.y * MaxSpeed : 0f;
            if (Stuck)
            {
                target = 0f;
                // Real trick (Spirit, Opportunity): back out slowly
                if (move.y < -0.1f && move.y > -0.7f) { _unstuck += dt; target = -0.4f; }
                if (_unstuck > 2f) { Stuck = false; _unstuck = 0f; }
            }
            bool soft = IsSoftSand != null && IsSoftSand(T.position);
            if (soft && !Stuck && Speed > MaxSpeed * 0.55f && !Airborne) { Stuck = true; Speed = 0f; _unstuck = 0f; }
            if (soft) target *= 0.35f;

            if (!Airborne)
            {
                Speed = Mathf.MoveTowards(Speed, target, Accel * dt);
                float turn = move.x * TurnRate * dt * (Speed < -0.1f ? -1f : 1f);
                if (power && !Stuck) Heading += turn;
            }

            var fwd = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
            var n = Terrain.Normal(T.position.x, T.position.z);
            float slope = Vector3.Angle(n, Vector3.up);
            // Low gravity = little grip: steep slopes make the rover slide downhill
            if (!Airborne && slope > SlideAngle)
            {
                var downhill = new Vector3(n.x, 0f, n.z).normalized;
                _slide += downhill * Gravity * Mathf.Sin(slope * Mathf.Deg2Rad) * 0.6f * dt;
            }
            else _slide = Vector3.MoveTowards(_slide, Vector3.zero, 4f * dt);

            Vector3 pos = T.position + (fwd * Speed + _slide) * dt;
            if (Terrain.HitsRock(pos, Radius, out var push)) { pos += push; Speed *= -0.25f; }
            pos = Terrain.Clamp(pos);

            float ground = Terrain.Height(pos);
            float groundVy = (ground - _lastGroundY) / Mathf.Max(1e-4f, dt);
            if (!Airborne)
            {
                // Leaving the top of a bump faster than gravity can pull you down = a hop
                if (groundVy < _lastGroundVy - Gravity * dt * 2.5f && Mathf.Abs(Speed) > 2f && _lastGroundVy > 0.5f)
                {
                    Airborne = true;
                    _vy = _lastGroundVy;
                    pos.y = T.position.y;
                }
                else pos.y = ground;
            }
            if (Airborne)
            {
                _vy -= Gravity * dt;
                pos.y = T.position.y + _vy * dt;
                if (pos.y <= ground) { pos.y = ground; Airborne = false; _vy = 0f; }
            }
            _lastGroundVy = Airborne ? 0f : groundVy;
            _lastGroundY = ground;
            T.position = pos;

            _up = Vector3.Slerp(_up, Airborne ? _up : n, 1f - Mathf.Exp(-8f * dt));
            T.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd, _up).normalized, _up);
            if (Mathf.Abs(Speed) > 0.1f) Battery = Mathf.Max(0f, Battery - DrainPerSecondMoving * dt);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    public class Hopper
    {
        public Transform T;
        public WorldTerrain Terrain;
        public float Gravity;
        public float Charge { get; private set; }
        public bool  Airborne { get; private set; }
        public float Nitrogen = 1f;             // 0..1 jets
        public Vector3 Vel;
        public System.Func<Vector3, bool> IsFrost;   // slippery: slides after landing
        Vector3 _slide;

        public Vector3 Aim(Vector2 stick, float cameraYaw)
        {
            var d = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(stick.x, 0f, stick.y);
            return d.sqrMagnitude > 0.01f ? d.normalized : Quaternion.Euler(0f, T.eulerAngles.y, 0f) * Vector3.forward;
        }

        public Vector3 LaunchVelocity(Vector3 dir, float charge) =>
            dir * (1.5f + 4f * charge) + Vector3.up * (1.2f + 2.2f * charge);

        public void Hold(float dt) { if (!Airborne) Charge = Mathf.Min(1f, Charge + dt / 1.2f); }

        public void Release(Vector3 dir)
        {
            if (Airborne || Charge <= 0.05f) { Charge = 0f; return; }
            Vel = LaunchVelocity(dir, Charge);
            Airborne = true;
            Charge = 0f;
            T.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        public void Jets(Vector3 dir, float dt)
        {
            if (!Airborne || Nitrogen <= 0f) return;
            Vel += (dir * 1.4f + Vector3.up * 0.9f) * dt;
            Nitrogen = Mathf.Max(0f, Nitrogen - dt * 0.06f);
        }

        public void Kick(Vector3 v) { Vel += v; Airborne = true; }

        // Checkpoint: back on the ground at this spot, at rest
        public void Place(Vector3 at)
        {
            at.y = Terrain.Height(at);
            T.position = at;
            Vel = Vector3.zero; _slide = Vector3.zero; Charge = 0f; Airborne = false;
        }

        public void Step(float dt)
        {
            if (Airborne)
            {
                Vel += Vector3.down * Gravity * dt;
                var pos = T.position + Vel * dt;
                pos = Terrain.Clamp(pos);
                float g = Terrain.Height(pos);
                if (pos.y <= g)
                {
                    pos.y = g;
                    Airborne = false;
                    _slide = IsFrost != null && IsFrost(pos) ? new Vector3(Vel.x, 0f, Vel.z) * 0.8f : Vector3.zero;
                    Vel = Vector3.zero;
                }
                T.position = pos;
            }
            else if (_slide.sqrMagnitude > 0.01f)
            {
                var pos = Terrain.Clamp(T.position + _slide * dt);
                pos.y = Terrain.Height(pos);
                T.position = pos;
                _slide = Vector3.MoveTowards(_slide, Vector3.zero, 0.6f * dt);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    public class Drone
    {
        public Transform T;
        public WorldTerrain Terrain;
        public float Gravity, AirDensity;
        public Vector3 Vel;
        public float Battery = 1f;
        public float FlyDrain = 0.012f, Recharge = 0.03f;
        public bool Landed { get; private set; } = true;

        public float Surface => Terrain.SurfaceY(T.position);
        public float Altitude => T.position.y - Surface;

        // Checkpoint: landed at this spot, rotors off
        public void Place(Vector3 at)
        {
            at.y = Terrain.SurfaceY(at);
            T.position = at;
            Vel = Vector3.zero; Landed = true;
        }

        public void Step(Vector2 stick, float cameraYaw, bool lift, float dt)
        {
            bool power = Battery > 0f;
            var wish = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(stick.x, 0f, stick.y) * 7f;
            float up = (lift && power ? Gravity * 2.2f : Gravity * (power && !Landed ? 0.75f : 0f));   // idle rotors almost hover
            if (Landed && !(lift && power)) { Vel = Vector3.zero; }
            else
            {
                Landed = false;
                var h = new Vector3(Vel.x, 0f, Vel.z);
                h = Vector3.MoveTowards(h, power ? wish : Vector3.zero, 5f * dt);
                Vel.x = h.x; Vel.z = h.z;
                Vel.y += (up - Gravity) * dt;
                Vel *= Mathf.Exp(-0.25f * AirDensity * 0.2f * dt);   // thick air damps everything
                Vel.y = Mathf.Clamp(Vel.y, -3f, 4f);
                var pos = Terrain.Clamp(T.position + Vel * dt);
                float s = Terrain.SurfaceY(pos);
                if (pos.y <= s + 0.05f && Vel.y <= 0f) { pos.y = s; Landed = true; Vel = Vector3.zero; }
                pos.y = Mathf.Min(pos.y, s + 60f);   // stay near the ground (it's a science flight)
                T.position = pos;
                if (h.sqrMagnitude > 0.2f) T.rotation = Quaternion.Slerp(T.rotation, Quaternion.LookRotation(h, Vector3.up), 1f - Mathf.Exp(-3f * dt));
            }
            Battery = Mathf.Clamp01(Battery + (Landed ? Recharge : -FlyDrain) * dt);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Shapes used until each Meshy model exists (right colours, right proportions)
    public static class VehicleShapes
    {
        static readonly Color White = new(0.92f, 0.92f, 0.9f), Gold = new(0.85f, 0.65f, 0.25f),
                              Dark = new(0.2f, 0.2f, 0.22f), Shield = new(0.45f, 0.28f, 0.16f);

        static GameObject P(WorldAssets a, PrimitiveType t, Transform parent, Color c, Vector3 pos, Vector3 scale, Vector3 euler = default)
        {
            var go = WorldAssets.Primitive(t, parent, a.NewMaterial(a.PropMaterial, c, 0.35f));
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        // Every shape: origin at the bottom centre, forward = +Z
        public static GameObject Build(WorldAssets a, string kind, Transform parent)
        {
            var root = new GameObject(kind);
            root.transform.SetParent(parent, false);
            var r = root.transform;
            switch (kind)
            {
                case "capsule":
                    P(a, PrimitiveType.Cylinder, r, Shield, new Vector3(0, 0.1f, 0), new Vector3(2.8f, 0.1f, 2.8f));
                    P(a, PrimitiveType.Sphere, r, White, new Vector3(0, 0.8f, 0), new Vector3(2.4f, 1.4f, 2.4f));
                    break;
                case "parachute":
                    P(a, PrimitiveType.Sphere, r, new Color(1f, 0.55f, 0.2f), new Vector3(0, 0, 0), new Vector3(9f, 2.5f, 9f));
                    P(a, PrimitiveType.Cylinder, r, White, new Vector3(0, 0.3f, 0), new Vector3(3f, 0.9f, 3f));
                    break;
                case "skycrane":
                    P(a, PrimitiveType.Cube, r, Gold, new Vector3(0, 0.5f, 0), new Vector3(3f, 0.6f, 3f));
                    for (int i = 0; i < 4; i++)
                    {
                        var d = Quaternion.Euler(0, 45 + i * 90, 0) * Vector3.forward * 1.7f;
                        P(a, PrimitiveType.Cylinder, r, Dark, d + Vector3.up * 0.2f, new Vector3(0.35f, 0.3f, 0.35f));
                    }
                    break;
                case "lander":
                    P(a, PrimitiveType.Cube, r, Gold, new Vector3(0, 1.3f, 0), new Vector3(1.8f, 1f, 1.8f));
                    P(a, PrimitiveType.Cylinder, r, White, new Vector3(0, 2.1f, 0), new Vector3(0.9f, 0.05f, 0.9f));
                    for (int i = 0; i < 4; i++)
                    {
                        var d = Quaternion.Euler(0, 45 + i * 90, 0) * Vector3.forward;
                        P(a, PrimitiveType.Cylinder, r, White, d * 1.3f + Vector3.up * 0.55f, new Vector3(0.12f, 0.55f, 0.12f));
                        P(a, PrimitiveType.Cylinder, r, Dark, d * 1.45f + Vector3.up * 0.03f, new Vector3(0.5f, 0.03f, 0.5f));
                    }
                    break;
                case "rover":
                case "thermalrover":
                    P(a, PrimitiveType.Cube, r, White, new Vector3(0, 0.95f, 0), new Vector3(1.7f, 0.55f, 2.6f));
                    for (int i = 0; i < 6; i++)
                    {
                        float z = (i % 3 - 1) * 1.05f, x = i < 3 ? -1.05f : 1.05f;
                        P(a, PrimitiveType.Cylinder, r, Dark, new Vector3(x, 0.38f, z), new Vector3(0.75f, 0.14f, 0.75f), new Vector3(0, 0, 90));
                    }
                    P(a, PrimitiveType.Cylinder, r, White, new Vector3(0.4f, 1.9f, 0.9f), new Vector3(0.12f, 0.55f, 0.12f));
                    P(a, PrimitiveType.Cube, r, Dark, new Vector3(0.4f, 2.5f, 0.95f), new Vector3(0.45f, 0.25f, 0.3f));
                    P(a, PrimitiveType.Cylinder, r, Gold, new Vector3(-0.55f, 1.3f, -1.1f), new Vector3(0.45f, 0.25f, 0.45f));
                    if (kind == "thermalrover")
                        P(a, PrimitiveType.Cylinder, r, White, new Vector3(0, 2.2f, 0), new Vector3(3.2f, 0.04f, 3.2f));
                    break;
                case "venuslander":
                    P(a, PrimitiveType.Cylinder, r, Dark, new Vector3(0, 0.25f, 0), new Vector3(2.6f, 0.25f, 2.6f));
                    P(a, PrimitiveType.Sphere, r, new Color(0.75f, 0.75f, 0.72f), new Vector3(0, 1.3f, 0), new Vector3(1.8f, 1.8f, 1.8f));
                    P(a, PrimitiveType.Cylinder, r, White, new Vector3(0, 2.5f, 0), new Vector3(2.8f, 0.05f, 2.8f));
                    P(a, PrimitiveType.Cube, r, Dark, new Vector3(0, 0.9f, 1.4f), new Vector3(0.15f, 0.15f, 1.2f));
                    break;
                case "probe":
                    P(a, PrimitiveType.Cylinder, r, Shield, new Vector3(0, 0.1f, 0), new Vector3(1.5f, 0.12f, 1.5f));
                    P(a, PrimitiveType.Sphere, r, new Color(0.8f, 0.78f, 0.7f), new Vector3(0, 0.6f, 0), new Vector3(1.3f, 1f, 1.3f));
                    break;
                case "dragonfly":
                    P(a, PrimitiveType.Cube, r, White, new Vector3(0, 0.9f, 0), new Vector3(1.3f, 0.6f, 2.3f));
                    P(a, PrimitiveType.Cylinder, r, Gold, new Vector3(0, 1f, -1.3f), new Vector3(0.45f, 0.3f, 0.45f), new Vector3(90, 0, 0));
                    for (int i = 0; i < 4; i++)
                    {
                        var d = Quaternion.Euler(0, 45 + i * 90, 0) * Vector3.forward * 1.6f;
                        P(a, PrimitiveType.Cylinder, r, Dark, d + Vector3.up * 1.25f, new Vector3(1.1f, 0.02f, 1.1f));
                        P(a, PrimitiveType.Cylinder, r, Dark, d + Vector3.up * 1.4f, new Vector3(1.1f, 0.02f, 1.1f));
                    }
                    P(a, PrimitiveType.Cube, r, Dark, new Vector3(-0.6f, 0.25f, 0), new Vector3(0.1f, 0.1f, 2.2f));
                    P(a, PrimitiveType.Cube, r, Dark, new Vector3(0.6f, 0.25f, 0), new Vector3(0.1f, 0.1f, 2.2f));
                    break;
                case "hopper":
                    P(a, PrimitiveType.Sphere, r, White, new Vector3(0, 1.1f, 0), new Vector3(1.3f, 1f, 1.3f));
                    P(a, PrimitiveType.Cylinder, r, Gold, new Vector3(0, 1.75f, 0), new Vector3(0.1f, 0.35f, 0.1f));
                    for (int i = 0; i < 3; i++)
                    {
                        var d = Quaternion.Euler(0, i * 120, 0) * Vector3.forward;
                        P(a, PrimitiveType.Cylinder, r, Dark, d * 0.7f + Vector3.up * 0.4f, new Vector3(0.1f, 0.45f, 0.1f));
                    }
                    break;
                case "orbiter":
                    P(a, PrimitiveType.Cube, r, Gold, Vector3.zero, new Vector3(2f, 2f, 2.4f));
                    P(a, PrimitiveType.Cylinder, r, White, new Vector3(0, 1.3f, 0), new Vector3(4f, 0.15f, 4f));
                    P(a, PrimitiveType.Cube, r, Dark, new Vector3(3.5f, 0, 0), new Vector3(5f, 0.08f, 0.08f));
                    break;
                default:
                    P(a, PrimitiveType.Cube, r, White, new Vector3(0, 0.5f, 0), Vector3.one);
                    break;
            }
            return root;
        }
    }
}
