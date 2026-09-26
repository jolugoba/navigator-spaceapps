// Navigator 2.0 — ShipCatalog.cs
// The hangar: every ship model the player can fly (Meshy NV-01 … NV-10, filled in by
// Navigator → Setup Scene). The choice is cosmetic: physics, fuel and ΔV come from the mission
// design, never from the hull, so no ship is "better". Rivals see the ship you picked.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Core;

namespace Navigator.Game
{
    public class ShipCatalog : MonoBehaviour
    {
        public GameObject[] Models;   // index 0 = NV-01
        public float[]      Yaw;      // turn that puts each model's nose on +Z (Assign Models)
        public float        ShipSize = 3f;

        public static ShipCatalog Instance { get; private set; }
        public static event System.Action<int> SelectionChanged;

        const string PrefKey = "navigator.ship";

        void Awake() => Instance = this;

        public int Count => Models != null ? Models.Length : 0;

        public static int Selected
        {
            get
            {
                int n = Instance != null ? Instance.Count : 0;
                int i = PlayerPrefs.GetInt(PrefKey, 0);
                return n == 0 ? 0 : Mathf.Clamp(i, 0, n - 1);
            }
            set
            {
                if (Instance == null || Instance.Count == 0) return;
                int i = ((value % Instance.Count) + Instance.Count) % Instance.Count;
                if (i == PlayerPrefs.GetInt(PrefKey, 0)) return;
                PlayerPrefs.SetInt(PrefKey, i);
                PlayerPrefs.Save();
                SelectionChanged?.Invoke(i);
            }
        }

        public bool Has(int index) => index >= 0 && index < Count && Models[index] != null;

        // Builds ship `index` under `parent` as a child named "Model", nose on +Z, `ShipSize` long
        public GameObject Build(int index, Transform parent)
        {
            if (!Has(index)) index = 0;
            if (!Has(index)) return null;

            // Measure with the parent unrotated so the size fit uses the ship's own axes
            Quaternion saved = parent.rotation;
            parent.rotation = Quaternion.identity;

            var model = Instantiate(Models[index], parent);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, Yaw != null && index < Yaw.Length ? Yaw[index] : 0f, 0f);
            ModelFit.FitToSize(model, ShipSize);

            parent.rotation = saved;
            return model;
        }

        // Replaces the "Model" child of the local ship with the selected hull
        public void ApplyTo(Transform ship, int index)
        {
            if (ship == null || !Has(index)) return;
            var old = ship.Find("Model");
            if (old != null)
            {
                old.name = "Model_old";
                Destroy(old.gameObject);
            }
            Build(index, ship);
        }
    }
}
