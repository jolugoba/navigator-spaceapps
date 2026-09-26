// Navigator 2.0 — Powerup.cs
// Un pod flotante: el jugador lo recoge al pasar encima (Trigger).
// Vive en su propio archivo: Unity solo serializa un MonoBehaviour si el archivo se llama como la clase.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Network;
using TMPro;

namespace Navigator.Game
{
    // ── El pod individual ──────────────────────────────────────────────────────

    [RequireComponent(typeof(Collider))]
    public class Powerup : MonoBehaviour
    {
        public PowerupType Type;

        // HUD listens to show "+15% FUEL", "SHIELD 10 s"…
        public static event System.Action<PowerupType> Collected;

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

            AudioFX.Play(Sfx.Pickup);
            Collected?.Invoke(Type);

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
}
