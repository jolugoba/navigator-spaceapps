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

    // ── El pod individual ──────────────────────────────────────────────────────

    [RequireComponent(typeof(Collider))]
    public class Powerup : MonoBehaviour
    {
        public PowerupType Type;

        [Header("Animación")]
        public float BobAmplitude = 0.15f;  // metros de subida/bajada
        public float BobSpeed     = 1.2f;   // ciclos por segundo
        public float RotateSpeed  = 45f;    // grados/s eje Y

        public const float GravityAssistImpulse = 60f;
        public const int   EmpSeconds           = 5;

        [Header("Visual")]
        public Renderer PodRenderer;        // índice 0 = color del tipo
        public GameObject CollectFX;        // partículas al recoger
        public TMP_Text   LabelWorld;       // etiqueta en world space (opcional)

        private Vector3 _startPos;
        private float   _bobPhase;
        private static readonly MaterialPropertyBlock _mpb = new();
        private static readonly int _colorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _emitId  = Shader.PropertyToID("_EmissionColor");

        void Start()
        {
            _startPos = transform.position;
            // Offset de fase aleatorio para que los pods no bobeen en sincronía
            _bobPhase = Random.value * Mathf.PI * 2f;

            ApplyColor();
            if (LabelWorld != null) LabelWorld.text = TypeLabel();

            // El collider debe ser trigger
            GetComponent<Collider>().isTrigger = true;
        }

        void Update()
        {
            // Movimiento flotante
            float y = Mathf.Sin(Time.time * BobSpeed * Mathf.PI * 2f + _bobPhase) * BobAmplitude;
            transform.position = _startPos + Vector3.up * y;

            // Rotación
            transform.Rotate(Vector3.up, RotateSpeed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            // Solo la nave local puede recoger pods
            if (!other.CompareTag("LocalShip")) return;

            var ship = other.GetComponent<ShipController>();
            if (ship == null || !ship.IsLocalPlayer) return;

            Apply(other.gameObject);
        }

        void Apply(GameObject shipObj)
        {
            switch (Type)
            {
                case PowerupType.Fuel:
                    shipObj.GetComponent<FuelSystem>()?.Refuel(0.15f);
                    Debug.Log("[Powerup] +15% combustible");
                    break;

                case PowerupType.Boost:
                    shipObj.GetComponent<BurnoutSystem>()?.ForceFullBar();
                    Debug.Log("[Powerup] Barra Burnout al 100%");
                    break;

                case PowerupType.Shield:
                    shipObj.GetComponent<CollisionSystem>()?.ActivateShield(10f);
                    Debug.Log("[Powerup] Escudo 10s activo");
                    break;

                case PowerupType.EMP:
                    BroadcastEMP(shipObj);
                    Debug.Log("[Powerup] EMP disparado");
                    break;

                case PowerupType.GravityAssist:
                    ApplyGravityAssist(shipObj);
                    Debug.Log($"[Powerup] Gravity assist +{GravityAssistImpulse} u/s");
                    break;
            }

            // Efecto visual de recolección
            if (CollectFX != null)
                Instantiate(CollectFX, transform.position, Quaternion.identity);

            // Notifica al chat
            var chat = FindObjectOfType<Network.P2PChatSystem>();
            chat?.PowerupBroadcast(Type.ToString());

            // Avisa al SpawnManager para que programe el respawn
            FindObjectOfType<PowerupSystem>()?.OnPodCollected();

            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }

        void ApplyGravityAssist(GameObject shipObj)
        {
            var rb = shipObj.GetComponent<Rigidbody>();
            if (rb == null) return;
            // Impulso gratis en la dirección de vuelo + carga de Burnout
            Vector3 dir = rb.linearVelocity.normalized;
            if (dir == Vector3.zero) dir = shipObj.transform.forward;
            rb.linearVelocity += dir * GravityAssistImpulse;
            shipObj.GetComponent<BurnoutSystem>()?.RegisterGravityAssist();

            var score = FindObjectOfType<Navigator.Core.MissionScore>();
            if (score != null) score.GravityAssistUsed = true;
        }

        void BroadcastEMP(GameObject shipObj)
        {
            // Each player simulates only their own ship, so the rival applies the EMP on receipt
            var senda = FindObjectOfType<SendaBridge>();
            if (senda == null || !senda.Initialized) return;
            senda.Broadcast(NavMsg.Build(NavMsgType.shipEvent, senda.NodeId,
                $"{{\"emp\":{EmpSeconds},\"name\":\"SISTEMA\",\"text\":\"EMP recibido: Burnout bloqueado {EmpSeconds}s\"}}"));
        }

        void ApplyColor()
        {
            if (PodRenderer == null) return;
            Color c = TypeColor();
            _mpb.SetColor(_colorId, c);
            _mpb.SetColor(_emitId,  c * 1.8f);
            PodRenderer.SetPropertyBlock(_mpb);
        }

        Color TypeColor() => Type switch
        {
            PowerupType.Fuel          => new Color(0.2f, 1f, 0.3f),    // verde
            PowerupType.Boost         => new Color(1f, 0.6f, 0f),      // naranja
            PowerupType.Shield        => new Color(0.2f, 0.6f, 1f),    // azul
            PowerupType.EMP           => new Color(0.8f, 0.2f, 1f),    // violeta
            PowerupType.GravityAssist => new Color(1f, 1f, 0.2f),      // amarillo
            _ => Color.white,
        };

        string TypeLabel() => Type switch
        {
            PowerupType.Fuel          => "COMBUSTIBLE",
            PowerupType.Boost         => "BOOST",
            PowerupType.Shield        => "ESCUDO",
            PowerupType.EMP           => "EMP",
            PowerupType.GravityAssist => "GRAVITY ASSIST",
            _ => Type.ToString(),
        };
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
