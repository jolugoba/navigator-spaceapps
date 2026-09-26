// Navigator 2.0 — CreditsUI.cs
// Lobby → CREDITS: the NASA data, images and software the game is built on (full list: CREDITS.md)
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.UI;

namespace Navigator.UI
{
    public class CreditsUI : MonoBehaviour
    {
        public GameObject Panel;
        public Button     OpenButton;
        public Button     CloseButton;

        void Awake()
        {
            OpenButton?.onClick.AddListener(() => { if (Panel != null) Panel.SetActive(true); });
            CloseButton?.onClick.AddListener(() => { if (Panel != null) Panel.SetActive(false); });
            if (Panel != null) Panel.SetActive(false);
        }
    }
}
