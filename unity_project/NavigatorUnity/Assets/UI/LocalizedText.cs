// Navigator 2.0 — LocalizedText.cs
// Fixed UI text that follows the selected language
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string Key;

        void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        void OnDisable() => Loc.Changed -= Apply;

        void Apply()
        {
            if (!string.IsNullOrEmpty(Key)) GetComponent<TMP_Text>().text = Loc.T(Key);
        }
    }
}
