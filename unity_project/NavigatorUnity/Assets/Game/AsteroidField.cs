// Navigator 2.0 — AsteroidField.cs
// Cinturón de asteroides en la ruta al planeta
// Pasar cerca sin tocar = near-miss (carga Burnout). Chocar = daño y fuga de combustible.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Game
{
    public class AsteroidField : MonoBehaviour
    {
        [Header("Modelo (OB-01_Asteroid_FINAL)")]
        public GameObject AsteroidPrefab;
        public GameObject ExplosionPrefab;

        [Header("Distribución")]
        public int   Count        = 40;
        public float RouteSpread  = 45f;    // desvío lateral respecto a la ruta
        public float MinRadius    = 3f;
        public float MaxRadius    = 9f;

        [Header("Detección (unidades de juego)")]
        public float HitMargin      = 1.5f;  // radio asteroide + esto = choque
        public float NearMissMargin = 7f;    // radio asteroide + esto = near-miss

        private class Rock
        {
            public Transform T;
            public float     Radius;
            public Vector3   Spin;
            public bool      NearMissCounted;
            public bool      Hit;
        }

        private readonly List<Rock> _rocks = new();
        private Transform       _ship;
        private BurnoutSystem   _burnout;
        private CollisionSystem _collision;

        public void SpawnAlongRoute(Vector3 from, Vector3 to, Transform ship)
        {
            Clear();
            _ship      = ship;
            _burnout   = ship != null ? ship.GetComponent<BurnoutSystem>()   : null;
            _collision = ship != null ? ship.GetComponent<CollisionSystem>() : null;

            for (int i = 0; i < Count; i++)
            {
                // Keep the launch area and the planet's surroundings clear
                Vector3 pos = Vector3.Lerp(from, to, Random.Range(0.12f, 0.88f))
                            + Random.insideUnitSphere * RouteSpread;
                float radius = Random.Range(MinRadius, MaxRadius);

                GameObject go;
                if (AsteroidPrefab != null)
                {
                    go = new GameObject();
                    go.transform.SetParent(transform);
                    go.transform.SetPositionAndRotation(pos, Random.rotation);
                    var model = Instantiate(AsteroidPrefab, go.transform);
                    model.transform.localPosition = Vector3.zero;
                    Navigator.Core.ModelFit.FitToSize(model, radius * 2f);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(transform);
                    go.transform.SetPositionAndRotation(pos, Random.rotation);
                    var r = go.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(0.45f, 0.4f, 0.35f);
                    go.transform.localScale = Vector3.one * radius * 2f;
                }
                go.name = $"Asteroid_{i}";
                Scannable.Tag(go, "asteroid", "asteroid", radius);

                _rocks.Add(new Rock
                {
                    T      = go.transform,
                    Radius = radius,
                    Spin   = Random.insideUnitSphere * 25f,
                });
            }
        }

        public void Clear()
        {
            foreach (var r in _rocks) if (r.T != null) Destroy(r.T.gameObject);
            _rocks.Clear();
        }

        void Update()
        {
            if (_ship == null) return;
            Vector3 shipPos = _ship.position;
            float dt = Time.deltaTime;

            foreach (var rock in _rocks)
            {
                if (rock.T == null) continue;
                rock.T.Rotate(rock.Spin * dt, Space.Self);
                if (rock.Hit) continue;

                float dist = Vector3.Distance(shipPos, rock.T.position);

                if (dist < rock.Radius + HitMargin)
                {
                    rock.Hit = true;
                    OnHit(rock);
                }
                else if (dist < rock.Radius + NearMissMargin)
                {
                    rock.NearMissCounted = true;   // armed; counted once we leave cleanly
                }
                else if (rock.NearMissCounted)
                {
                    rock.NearMissCounted = false;
                    _burnout?.RegisterNearMissAsteroid();
                }
            }
        }

        void OnHit(Rock rock)
        {
            ExplosionVFX.Spawn(ExplosionPrefab, rock.T.position, false);
            rock.T.gameObject.SetActive(false);

            if (_collision == null || _collision.IsInvincible) return;
            if (_burnout != null && _burnout.BurnoutActive) return;

            _collision.ApplyDamage(CollisionSeverity.Medium);
            _collision.StartInvincibility();
        }
    }
}
