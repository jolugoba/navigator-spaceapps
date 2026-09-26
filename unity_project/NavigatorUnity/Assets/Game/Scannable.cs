// Navigator 2.0 — Scannable.cs
// Anything the ship's scanner can identify: asteroids, pods, planets, flyby worlds, rival ships,
// and the nebulae / galaxy / black hole painted in the sky.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Game
{
    public class Scannable : MonoBehaviour
    {
        // asteroid · pod · planet · flyby · ship · sky
        public string Kind;
        // what it is inside its kind: "Fuel", "mars", a rival's NodeId, "nebula_01"…
        public string Id;
        // Sky objects are "at infinity": only the direction matters, never the distance
        public bool   AtInfinity;
        // How big it is, so a near miss with the reticle still counts
        public float  Radius = 3f;

        public static readonly HashSet<Scannable> All = new();

        void OnEnable()  => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Scannable Tag(GameObject go, string kind, string id, float radius, bool atInfinity = false)
        {
            var s = go.GetComponent<Scannable>() ?? go.AddComponent<Scannable>();
            s.Kind = kind; s.Id = id; s.Radius = radius; s.AtInfinity = atInfinity;
            return s;
        }
    }
}
