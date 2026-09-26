// Navigator 2.0 — ShareChallengeUI.cs
// Turns a finished mission into a challenge: QR to scan plus a text code to paste in any chat
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    public class ShareChallengeUI : MonoBehaviour
    {
        public GameObject Panel;
        public RawImage   QRImage;
        public TMP_Text   SummaryLabel;
        public TMP_Text   CodeLabel;
        public Button     CopyButton;
        public TMP_Text   CopyLabel;
        public Button     CloseButton;

        private string _code;

        void Awake()
        {
            CopyButton ?.onClick.AddListener(Copy);
            CloseButton?.onClick.AddListener(() => { if (Panel != null) Panel.SetActive(false); });
            if (Panel != null) Panel.SetActive(false);
        }

        public void Show(MissionDesign design, int score, string playerName)
        {
            if (design == null) return;
            _code = Challenge.Encode(design, score, playerName);

            if (QRImage != null)
            {
                try { QRImage.texture = LobbyManager.GenerateQR(_code, 512); }
                catch (System.Exception e) { Debug.LogError($"[Share] QR failed: {e.Message}"); }
            }
            int budget = Mathf.CeilToInt(design.TotalCostM / 10f) * 10;
            if (SummaryLabel != null)
                SummaryLabel.text = Loc.F("share.summary", Loc.Objective(design.Objective), Loc.Planet(design.TargetPlanetId),
                                          score, budget);
            if (CodeLabel != null) CodeLabel.text = _code;
            if (CopyLabel != null) CopyLabel.text = Loc.T("share.copy");
            if (Panel != null) Panel.SetActive(true);
        }

        void Copy()
        {
            // Paste it in any chat; the friend taps "Paste challenge" in the lobby
            WebPlatform.Copy(Loc.F("share.message", _code));
            if (CopyLabel != null) CopyLabel.text = Loc.T("share.copied");
        }
    }
}
