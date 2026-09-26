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
        public ParticleSystem BurnoutParticlesPrefab; // exhaust blanco Burnout

        [Header("Cámara AR (para transformar posiciones)")]
        public Camera ARCamera;

        // ── Runtime ────────────────────────────────────────────────────────────
        private ShipSyncService _sync;
        private readonly Dictionary<string, RemoteShipInstance> _ships = new();

        void Awake()
        {
            _sync = GetComponent<ShipSyncService>();
            if (_sync != null)
                _sync.OnRemoteUpdate.AddListener(OnRemoteUpdate);
        }

        // ── Recibe posiciones del otro jugador (cada 50ms vía Senda) ──────────

        void OnRemoteUpdate(Dictionary<string, ShipStateNet> states)
        {
            foreach (var kv in states)
            {
                string nodeId = kv.Key;
                var    state  = kv.Value;

                if (!_ships.TryGetValue(nodeId, out var instance))
                {
                    instance = SpawnRemoteShip(nodeId, state.DisplayName ?? nodeId[..8]);
                    _ships[nodeId] = instance;
                }

                // Guarda el estado objetivo — Update() interpola suavemente
                instance.TargetPosition = MissionToARPosition(state.Position);
                instance.TargetRotation = Quaternion.Euler(state.Pitch, state.Yaw, state.Roll);
                instance.Boosting       = state.Boosting;
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

                // Efecto Burnout activo
                if (inst.BurnoutParticles != null)
                    inst.BurnoutParticles.gameObject.SetActive(inst.BurnoutActive);
            }
        }

        // ── Instanciar nave rival ─────────────────────────────────────────────

        RemoteShipInstance SpawnRemoteShip(string nodeId, string displayName)
        {
            var root = Instantiate(RemoteShipPrefab);
            root.name = $"RemoteShip_{nodeId[..8]}";
            root.tag  = "RemoteShip";

            // Personalizar con colores del rival (se actualizan con lobbyInfo)
            var customizer = root.GetComponent<ShipCustomizer>();
            if (customizer != null)
                customizer.SetPrimary(new Color(0.1f, 0.1f, 0.1f));  // negro KRONOS por defecto

            // Nombre flotante encima de la nave
            GameObject nameTag = null;
            if (NameTagPrefab != null)
            {
                nameTag = Instantiate(NameTagPrefab, root.transform);
                nameTag.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                var label = nameTag.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = displayName;
            }

            // Partículas Burnout (desactivadas al inicio)
            ParticleSystem burnoutFx = null;
            if (BurnoutParticlesPrefab != null)
            {
                burnoutFx = Instantiate(BurnoutParticlesPrefab, root.transform);
                burnoutFx.transform.localPosition = new Vector3(0f, 0f, -1.2f); // detrás de la nave
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

        // ── Coordenadas misión → espacio AR ───────────────────────────────────
        // Ambos jugadores usan el planeta objetivo como "norte"
        // Posición relativa en misión = posición relativa en AR
        // Escala: 1 unidad de misión = 0.01m en AR (ajustable)

        Vector3 MissionToARPosition(Vector3 missionPos)
        {
            if (ARCamera == null) return missionPos * 0.01f;

            // El origen AR = posición inicial del jugador local
            // La dirección "adelante" = dirección al planeta
            Vector3 forward = ARCamera.transform.forward;
            Vector3 right   = ARCamera.transform.right;
            Vector3 up      = ARCamera.transform.up;

            return ARCamera.transform.position
                 + forward * missionPos.z * 0.008f
                 + right   * missionPos.x * 0.008f
                 + up      * missionPos.y * 0.008f;
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
                Debug.Log($"[RemoteShip] {nodeId[..8]} desconectado — nave eliminada.");
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
        public bool       BurnoutActive;
        public float      FuelFraction;
        public int        Lives;
    }
}
