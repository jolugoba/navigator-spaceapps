// Navigator 2.0 — RemoteShipManager.cs
// Cuando llega un ShipState de otro jugador:
//   → instancia su nave en el espacio AR
//   → la mueve/rota suavemente cada frame
//   → muestra su nombre flotando encima
//   → muestra efecto Burnout si está activo
// Sin esto los jugadores están conectados pero no se ven.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Navigator.Network;
using Navigator.Game;

namespace Navigator.Game
{
    public class RemoteShipManager : MonoBehaviour
    {
        [Header("Prefab nave rival (NV-02 KRONOS de Meshy)")]
        public GameObject RemoteShipPrefab;   // asignar NV-02 desde Assets/Prefabs/Ships/

        [Header("Prefab nombre flotante")]
        public GameObject NameTagPrefab;      // Canvas WorldSpace con TMP_Text

        [Header("Efectos")]
        public Material ExhaustMaterial;       // llama del motor del rival
        public ParticleSystem BurnoutParticlesPrefab; // exhaust blanco Burnout

        [Header("Cámara AR (las etiquetas miran hacia ella)")]
        public Camera ARCamera;

        [Header("Escala")]
        public float ShipSize        = 3f;    // mismo tamaño que la nave local
        public float ModelYaw        = 0f;    // orienta la nariz del GLB hacia +Z (lo calcula Assign Models)
        public float CollisionRadius = 1.6f;

        // ── Runtime ────────────────────────────────────────────────────────────
        private ShipSyncService _sync;
        private readonly Dictionary<string, RemoteShipInstance> _ships = new();

        void Awake()
        {
            _sync = GetComponent<ShipSyncService>();
            if (_sync != null)
            {
                _sync.OnRemoteUpdate.AddListener(OnRemoteUpdate);
                _sync.OnRemoteName.AddListener(OnRemoteName);
                _sync.OnRemoteShip.AddListener(OnRemoteShip);
            }
        }

        private readonly Dictionary<string, string> _names = new();
        private readonly Dictionary<string, int>    _hulls = new();   // hangar index each rival picked

        // A rival chose (or changed) their ship in the hangar
        void OnRemoteShip(string nodeId, int index)
        {
            if (_hulls.TryGetValue(nodeId, out var current) && current == index) return;
            _hulls[nodeId] = index;
            if (_ships.TryGetValue(nodeId, out var inst) && inst.Root != null && ShipCatalog.Instance != null)
                ShipCatalog.Instance.ApplyTo(inst.Root, index);
        }

        void OnRemoteName(string nodeId, string name)
        {
            _names[nodeId] = name;
            if (_ships.TryGetValue(nodeId, out var inst) && inst.NameTag != null)
            {
                var label = inst.NameTag.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = name;
            }
        }

        // Latest hull / fuel a rival reported (for the scanner)
        public bool TryGetStatus(string nodeId, out int lives, out float fuel)
        {
            lives = 0; fuel = 0f;
            if (!_ships.TryGetValue(nodeId, out var inst)) return false;
            lives = inst.Lives; fuel = inst.FuelFraction;
            return true;
        }

        public string NameOf(string nodeId) =>
            _names.TryGetValue(nodeId, out var n) ? n : Short(nodeId);

        static string Short(string id) =>
            string.IsNullOrEmpty(id) ? "Rival" : id[..System.Math.Min(8, id.Length)];

        // ── Recibe posiciones del otro jugador (cada 50ms vía Senda) ──────────

        void OnRemoteUpdate(Dictionary<string, ShipStateNet> states)
        {
            foreach (var kv in states)
            {
                string nodeId = kv.Key;
                var    state  = kv.Value;

                if (!_ships.TryGetValue(nodeId, out var instance))
                {
                    string name = _names.TryGetValue(nodeId, out var n) ? n
                                : state.DisplayName ?? Short(nodeId);
                    instance = SpawnRemoteShip(nodeId, name);
                    _ships[nodeId] = instance;
                }

                // Guarda el estado objetivo — Update() interpola suavemente
                // Both players fly in the same world frame (origin = launch point)
                instance.TargetPosition = state.Position;
                instance.TargetRotation = Quaternion.Euler(state.Pitch, state.Yaw, state.Roll);
                instance.Boosting       = state.Boosting;
                instance.Throttle       = state.Throttle;
                instance.BurnoutActive  = state.BurnoutActive;
                instance.FuelFraction   = state.FuelFraction;
                instance.Lives          = state.Lives;
            }

            // Elimina naves que ya no envían datos (desconectados)
            RemoveStale(states);
        }

        void Update()
        {
            foreach (var kv in _ships)
            {
                var inst = kv.Value;
                if (inst.Root == null) continue;

                // Interpolación suave — evita saltos entre paquetes de 50ms
                inst.Root.position = Vector3.Lerp(
                    inst.Root.position, inst.TargetPosition, Time.deltaTime * 18f);
                inst.Root.rotation = Quaternion.Slerp(
                    inst.Root.rotation, inst.TargetRotation, Time.deltaTime * 12f);

                // El nombre siempre mira a la cámara AR
                if (inst.NameTag != null && ARCamera != null)
                    inst.NameTag.transform.LookAt(ARCamera.transform);

                var fx = inst.Root.GetComponent<ThrusterFX>();
                if (fx != null)
                {
                    fx.Thrust  = inst.Throttle;
                    fx.Boost   = inst.Boosting;
                    fx.Burnout = inst.BurnoutActive;
                    fx.Fuel    = inst.FuelFraction;
                }

                // Efecto Burnout activo
                if (inst.BurnoutParticles != null)
                    inst.BurnoutParticles.gameObject.SetActive(inst.BurnoutActive);
            }
        }

        // ── Instanciar nave rival ─────────────────────────────────────────────

        RemoteShipInstance SpawnRemoteShip(string nodeId, string displayName)
        {
            GameObject root;
            var catalog = ShipCatalog.Instance;
            if (catalog != null && _hulls.TryGetValue(nodeId, out int hull) && catalog.Has(hull))
            {
                root = new GameObject();
                catalog.Build(hull, root.transform);   // the ship they picked in the hangar
            }
            else if (RemoteShipPrefab != null)
            {
                root = new GameObject();
                var model = Instantiate(RemoteShipPrefab, root.transform);
                model.name = "Model";
                model.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
                Navigator.Core.ModelFit.FitToSize(model, ShipSize);
            }
            else
            {
                root = new GameObject();
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(capsule.GetComponent<Collider>());
                capsule.transform.SetParent(root.transform, false);
                capsule.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                capsule.transform.localScale    = new Vector3(1f, ShipSize * 0.5f, 1f);
            }
            root.name = $"RemoteShip_{Short(nodeId)}";
            if (TagExists("RemoteShip")) root.tag = "RemoteShip";

            // Kinematic proxy so the local ship's collisions (takedowns) register
            var col = root.AddComponent<SphereCollider>();
            col.radius = CollisionRadius;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
            root.AddComponent<CollisionSystem>().RemoteNodeId = nodeId;
            Scannable.Tag(root, "ship", nodeId, ShipSize);
            var thruster = root.AddComponent<ThrusterFX>();
            thruster.ShipLength = ShipSize;
            thruster.SetMaterial(ExhaustMaterial);

            // Personalizar con colores del rival (se actualizan con lobbyInfo)
            var customizer = root.GetComponentInChildren<ShipCustomizer>();
            if (customizer != null)
                customizer.SetPrimary(new Color(0.1f, 0.1f, 0.1f));  // negro KRONOS por defecto

            // Nombre flotante encima de la nave
            GameObject nameTag = null;
            if (NameTagPrefab != null)
            {
                nameTag = Instantiate(NameTagPrefab, root.transform);
                nameTag.transform.localPosition = new Vector3(0f, ShipSize * 0.9f, 0f);
                var label = nameTag.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = displayName;
            }

            // Partículas Burnout (desactivadas al inicio)
            ParticleSystem burnoutFx = null;
            if (BurnoutParticlesPrefab != null)
            {
                burnoutFx = Instantiate(BurnoutParticlesPrefab, root.transform);
                burnoutFx.transform.localPosition = new Vector3(0f, 0f, -ShipSize * 0.5f); // detrás de la nave
                burnoutFx.gameObject.SetActive(false);
            }

            Debug.Log($"[RemoteShip] Nave de {displayName} apareció en AR.");

            return new RemoteShipInstance
            {
                NodeId         = nodeId,
                Root           = root.transform,
                NameTag        = nameTag,
                BurnoutParticles = burnoutFx,
            };
        }

        static bool TagExists(string tag)
        {
            try { GameObject.FindWithTag(tag); return true; }
            catch (UnityException) { return false; }
        }

        // ── Limpieza ──────────────────────────────────────────────────────────

        void RemoveStale(Dictionary<string, ShipStateNet> current)
        {
            var toRemove = new List<string>();
            foreach (var nodeId in _ships.Keys)
                if (!current.ContainsKey(nodeId)) toRemove.Add(nodeId);

            foreach (var nodeId in toRemove)
            {
                if (_ships[nodeId].Root != null)
                    Destroy(_ships[nodeId].Root.gameObject);
                _ships.Remove(nodeId);
                Debug.Log($"[RemoteShip] {Short(nodeId)} desconectado — nave eliminada.");
            }
        }

        void OnDestroy()
        {
            foreach (var inst in _ships.Values)
                if (inst.Root != null) Destroy(inst.Root.gameObject);
        }
    }

    // ── Datos de cada nave remota en escena ───────────────────────────────────
    class RemoteShipInstance
    {
        public string          NodeId;
        public Transform       Root;
        public GameObject      NameTag;
        public ParticleSystem  BurnoutParticles;

        // Objetivo de interpolación (se actualiza cada paquete)
        public Vector3    TargetPosition;
        public Quaternion TargetRotation;
        public bool       Boosting;
        public float      Throttle;
        public bool       BurnoutActive;
        public float      FuelFraction;
        public int        Lives;
    }
}
