// Navigator 2.0 — ScoreboardUI.cs
// Pantalla final: scores de ambos jugadores con animación de reveal
// Espera el score del rival si todavía está volando
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    public class ScoreboardUI : MonoBehaviour
    {
        public const int RemotePending = -1;   // rival todavía jugando
        public const int RemoteSolo    = -2;   // no hay rival conectado
        public const int ScoreBarMax   = 13000; // máximo teórico ~13,250 (ver MissionScore)

        [Header("Panel raíz")]
        public GameObject Panel;

        [Header("Jugador local")]
        public TMP_Text LocalPlayerName;
        public TMP_Text LocalScoreLabel;      // "4,650"
        public TMP_Text LocalArrivalLabel;    // "#1 PRIMERO"
        public TMP_Text LocalScienceLabel;    // "2,400 pts ciencia"
        public TMP_Text LocalFuelLabel;       // "Combustible: 38%"
        public TMP_Text LocalTakedownLabel;   // "3 takedowns"
        public Image    LocalScoreBar;

        [Header("Jugador rival")]
        public GameObject RemoteBlock;        // se oculta en modo solo
        public TMP_Text RemotePlayerName;
        public TMP_Text RemoteScoreLabel;
        public TMP_Text RemoteArrivalLabel;   // "Esperando…" mientras el rival vuela
        public Image    RemoteScoreBar;

        [Header("Resultado")]
        public TMP_Text WinnerLabel;          // "¡VICTORIA!" / "DERROTA" / "EMPATE"
        public TMP_Text WinnerSubLabel;
        public GameObject VictoryFX;          // partículas celebración (opcional)

        [Header("Informe de misión")]
        public TMP_Text DebriefLabel;         // cómo cada decisión afectó el resultado

        [Header("Botones")]
        public Button PlayAgainButton;
        public Button MainMenuButton;

        [Header("Animación")]
        public float RevealDelayS = 0.4f;

        private int _remoteScore = RemotePending;

        void Awake()
        {
            PlayAgainButton?.onClick.AddListener(() => GameManager.Instance?.OnLobbyReady());
            MainMenuButton ?.onClick.AddListener(() => GameManager.Instance?.ExitToLobby());
        }

        // ── Mostrar scores ─────────────────────────────────────────────────────

        public void Show(int localScore, int remoteScore, MissionScore scoreData, string failReason = null,
                         string report = null)
        {
            _remoteScore = remoteScore;
            if (DebriefLabel != null) DebriefLabel.text = report ?? "";
            if (Panel != null) Panel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(RevealAnimation(localScore, scoreData, failReason));
        }

        // Llamado cuando llega el missionEnd del rival
        public void SetRemoteScore(int score) => _remoteScore = score;

        IEnumerator RevealAnimation(int localScore, MissionScore data, string failReason)
        {
            bool solo = _remoteScore == RemoteSolo;

            if (LocalScoreBar  != null) LocalScoreBar.fillAmount  = 0f;
            if (RemoteScoreBar != null) RemoteScoreBar.fillAmount = 0f;
            if (LocalScoreLabel  != null) LocalScoreLabel.text  = "0";
            if (RemoteScoreLabel != null) RemoteScoreLabel.text = "—";
            if (WinnerLabel    != null) WinnerLabel.text    = "";
            if (WinnerSubLabel != null) WinnerSubLabel.text = "";
            if (VictoryFX      != null) VictoryFX.SetActive(false);
            if (RemoteBlock    != null) RemoteBlock.SetActive(!solo);
            SetButtons(false);

            if (data != null)
            {
                if (LocalArrivalLabel != null)
                    LocalArrivalLabel.text = data.ArrivalPosition > 0
                        ? ArrivalText(data.ArrivalPosition)
                        : failReason ?? Loc.T("fail.notarrived");
                if (LocalScienceLabel  != null) LocalScienceLabel.text  = Loc.F("sb.science", data.SciencePoints);
                if (LocalFuelLabel     != null) LocalFuelLabel.text     = Loc.F("sb.fuel", data.FuelFraction);
                if (LocalTakedownLabel != null)
                    LocalTakedownLabel.text = Loc.F("sb.takedowns", data.TakedownCount);
            }

            yield return new WaitForSeconds(0.4f);
            yield return AnimateScore(LocalScoreLabel, LocalScoreBar, 0, localScore, 1.2f);

            if (solo)
            {
                ShowSoloResult(localScore, failReason);
                SetButtons(true);
                yield break;
            }

            // The rival may still be flying or doing science — never declare a winner early
            if (_remoteScore == RemotePending)
            {
                if (RemoteArrivalLabel != null) RemoteArrivalLabel.text = Loc.T("sb.waiting");
                if (WinnerSubLabel     != null) WinnerSubLabel.text     = Loc.T("sb.rivalflying");
                SetButtons(true);
                while (_remoteScore == RemotePending) yield return null;
            }

            if (RemoteArrivalLabel != null) RemoteArrivalLabel.text = Loc.T("sb.rivaldone");
            yield return new WaitForSeconds(RevealDelayS);
            yield return AnimateScore(RemoteScoreLabel, RemoteScoreBar, 0, _remoteScore, 1.0f);
            yield return new WaitForSeconds(RevealDelayS);

            ShowResult(localScore, _remoteScore);
            SetButtons(true);
        }

        IEnumerator AnimateScore(TMP_Text label, Image bar, int from, int to, float duration)
        {
            if (label == null && bar == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t    = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);  // ease-out cubic
                int   cur  = Mathf.RoundToInt(Mathf.Lerp(from, to, ease));

                if (label != null) label.text = cur.ToString("N0");
                if (bar   != null) bar.fillAmount = Mathf.Clamp01((float)cur / ScoreBarMax);
                yield return null;
            }

            if (label != null) label.text = to.ToString("N0");
            if (bar   != null) bar.fillAmount = Mathf.Clamp01((float)to / ScoreBarMax);
        }

        void ShowResult(int local, int remote)
        {
            if (WinnerLabel == null) return;

            bool win  = local  > remote;
            bool draw = local == remote;

            WinnerLabel.text  = win ? Loc.T("sb.win") : draw ? Loc.T("sb.draw") : Loc.T("sb.lose");
            WinnerLabel.color = win ? UIStyle.Gold : draw ? UIStyle.Text : UIStyle.Muted;

            if (WinnerSubLabel != null)
                WinnerSubLabel.text = win  ? Loc.T("sb.win.sub") :
                                      draw ? Loc.T("sb.draw.sub") :
                                             Loc.T("sb.lose.sub");

            if (win && VictoryFX != null) VictoryFX.SetActive(true);
        }

        void ShowSoloResult(int local, string failReason)
        {
            if (WinnerLabel == null) return;
            bool failed = !string.IsNullOrEmpty(failReason);
            WinnerLabel.text  = failed ? Loc.T("sb.failed") : Loc.T("sb.success");
            WinnerLabel.color = failed ? UIStyle.Bad : UIStyle.Gold;
            if (WinnerSubLabel != null)
                WinnerSubLabel.text = failed ? Loc.F("sb.failed.sub", failReason)
                                             : Loc.F("sb.success.sub", local);
            if (!failed && VictoryFX != null) VictoryFX.SetActive(true);
        }

        void SetButtons(bool interactable)
        {
            if (PlayAgainButton != null) PlayAgainButton.interactable = interactable;
            if (MainMenuButton  != null) MainMenuButton.interactable  = interactable;
        }

        // ── Setters externos ───────────────────────────────────────────────────

        public void SetLocalPlayerName(string name)
        {
            if (LocalPlayerName != null) LocalPlayerName.text = name;
        }

        public void SetRemotePlayerName(string name)
        {
            if (RemotePlayerName != null) RemotePlayerName.text = name;
        }

        static string ArrivalText(int pos) => pos switch
        {
            1 => Loc.T("sb.pos1"),
            2 => Loc.T("sb.pos2"),
            3 => Loc.T("sb.pos3"),
            _ => Loc.T("sb.arrived"),
        };
    }
}
