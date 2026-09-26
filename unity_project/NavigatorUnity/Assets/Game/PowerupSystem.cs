// Navigator 2.0 — PowerupSystem.cs
// Pods flotantes en el espacio: combustible, boost, escudo, EMP, gravity assist
// Cada pod flota y rota — el jugador lo recoge al pasar encima (Trigger)
// Guacalita Inc. — SAL / Claude

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Navigator.Network;
using TMPro;

namespace Navigator.Game
{
    public enum PowerupType
    {
        Fuel,           // +15% combustible
        Boost,          // boost 8s (llena la barra Burnout instantáneamente)
        Shield,         // escudo 10s — ignora daño de colisión
        EMP,            // neutraliza el boost del rival por 5s
        GravityAssist,  // +500 m/s ΔV instantáneo en dirección actual
    }

    // ── Gestor que spawnea los pods en la escena ───────────────────────────────

    public class PowerupSystem : MonoBehaviour
    {
        [Header("Prefabs de pods (uno por tipo)")]
        public GameObject FuelPodPrefab;
        public GameObject BoostPodPrefab;
        public GameObject ShieldPodPrefab;
        public GameObject EMPPodPrefab;
        public GameObject GravityAssistPodPrefab;

        [Header("Configuración de spawn")]
        public int   MaxPodsActive  = 8;         // pods simultáneos en la escena
        public float RouteSpread    = 60f;       // desvío lateral máximo respecto a la ruta
        public float RespawnDelayS  = 12f;       // tiempo hasta respawn al recogerse uno

        [Header("Cámara AR (para calcular posiciones relativas)")]
        public Camera ARCamera;

        // ── Runtime ────────────────────────────────────────────────────────────
        private readonly List<GameObject> _active = new();
        private int _spawnIndex = 0;

        // Distribución de tipos (2 fuel, 2 boost, 1 shield, 1 EMP, 2 gravity)
        private static readonly PowerupType[] SpawnSequence =
        {
            PowerupType.Fuel, PowerupType.Boost, PowerupType.Fuel, PowerupType.Shield,
            PowerupType.Boost, PowerupType.GravityAssist, PowerupType.EMP, PowerupType.GravityAssist,
        };

        private Vector3 _routeFrom, _routeTo;
        private bool    _hasRoute;

        // Llamado al empezar el vuelo: reparte los pods entre la salida y el planeta
        public void SpawnAlongRoute(Vector3 from, Vector3 to)
        {
            ClearAll();
            _routeFrom = from;
            _routeTo   = to;
            _hasRoute  = true;
            _spawnIndex = 0;
            for (int i = 0; i < MaxPodsActive; i++)
                SpawnNext();
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            foreach (var p in _active) if (p != null) Destroy(p);
            _active.Clear();
            _hasRoute = false;
        }

        void SpawnNext()
        {
            var type = SpawnSequence[_spawnIndex % SpawnSequence.Length];
            _spawnIndex++;
            SpawnPod(type);
        }

        void SpawnPod(PowerupType type)
        {
            var prefab = PrefabFor(type);
            if (prefab == null) return;

            Vector3 pos = RandomSpawnPos();
            var pod = Instantiate(prefab, pos, Random.rotation);
            var powerup = pod.GetComponent<Powerup>();
            if (powerup != null) powerup.Type = type;
            _active.Add(pod);

            Debug.Log($"[Powerups] Spawn {type} en {pos:F1}");
        }

        Vector3 RandomSpawnPos()
        {
            if (!_hasRoute)
            {
                Vector3 origin = ARCamera != null ? ARCamera.transform.position : Vector3.zero;
                return origin + Random.onUnitSphere * Random.Range(15f, RouteSpread);
            }
            // Entre el 15% y el 85% del trayecto, desviado a los lados
            Vector3 onRoute = Vector3.Lerp(_routeFrom, _routeTo, Random.Range(0.15f, 0.85f));
            return onRoute + Random.insideUnitSphere * RouteSpread;
        }

        public void OnPodCollected()
        {
            // Llamado por Powerup.Apply — respawnea uno nuevo después del delay
            StartCoroutine(RespawnAfterDelay());
        }

        IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(RespawnDelayS);
            _active.RemoveAll(p => p == null);
            if (_hasRoute && _active.Count < MaxPodsActive)
                SpawnNext();
        }

        GameObject PrefabFor(PowerupType type) => type switch
        {
            PowerupType.Fuel          => FuelPodPrefab,
            PowerupType.Boost         => BoostPodPrefab,
            PowerupType.Shield        => ShieldPodPrefab,
            PowerupType.EMP           => EMPPodPrefab,
            PowerupType.GravityAssist => GravityAssistPodPrefab,
            _ => null,
        };
    }
}
