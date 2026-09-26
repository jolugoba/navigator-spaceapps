// Navigator 2.0 — ShieldFX.cs
// Energy bubble around the ship while it can't be damaged (shield pod, post-hit grace, Burnout)
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    [RequireComponent(typeof(CollisionSystem))]
    public class ShieldFX : MonoBehaviour
    {
        public Material BubbleMaterial;
        public float    Radius = 2.4f;

        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color ShieldColor  = new(0.3f, 0.75f, 1f);
        static readonly Color BurnoutColor = new(1f, 0.55f, 0.1f);

        private CollisionSystem _collision;
        private BurnoutSystem   _burnout;
        private Renderer        _bubble;
        private MaterialPropertyBlock _mpb;
        private float _alpha;

        void Start()
        {
            _collision = GetComponent<CollisionSystem>();
            _burnout   = GetComponent<BurnoutSystem>();
            _mpb       = new MaterialPropertyBlock();

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ShieldBubble";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * Radius * 2f;
            _bubble = go.GetComponent<Renderer>();
            _bubble.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (BubbleMaterial != null) _bubble.sharedMaterial = BubbleMaterial;
            _bubble.enabled = false;
        }

        void Update()
        {
            bool burn   = _burnout != null && _burnout.BurnoutActive;
            bool active = burn || (_collision != null && _collision.IsInvincible);
            _alpha = Mathf.MoveTowards(_alpha, active ? 1f : 0f, 4f * Time.deltaTime);
            _bubble.enabled = _alpha > 0.01f;
            if (!_bubble.enabled) return;

            float pulse = 0.18f + 0.08f * Mathf.Sin(Time.time * 8f);
            Color c = burn ? BurnoutColor : ShieldColor;
            c.a = pulse * _alpha;
            _mpb.SetColor(ColorId, c);
            _bubble.SetPropertyBlock(_mpb);
            _bubble.transform.localScale = Vector3.one * Radius * 2f * (1f + 0.03f * Mathf.Sin(Time.time * 5f));
        }
    }
}
