// Navigator 2.0 — ScoreboardUI.cs
// Pantalla final: scores de ambos jugadores con animación de reveal
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
        [Header("Panel raíz")]
        public GameObject Panel;

        [Header("Jugador local")]
        public TMP_Text LocalPlayerName;
        public TMP_Text LocalScoreLabel;      // "4,650"
        public TMP_Text LocalArrivalLabel;    // "#1 🏆"
        public TMP_Text LocalScienceLabel;    // "2,400 ciencia"
        public TMP_Text LocalFuelLabel;       // "Combustible: 38%"
        public TMP_Text LocalTakedownLabel;   // "3 takedowns"
        public Image    LocalScoreBar;

        [Header("Jugador rival")]
        public TMP_Text RemotePlayerName;
        public TMP_Text RemoteScoreLabel;
        public TMP_Text RemoteArrivalLabel;
        public Image    RemoteScoreBar;

        [Header("Resultado")]
        public TMP_Text WinnerLabel;          // "¡VICTORIA!" / "Derrota" / "EMPATE"
        public TMP_Text WinnerSubLabel;       // "¡Misión completada!" / "Mejor suerte la próxima"
        public GameObject VictoryFX;          // partículas celebración (opcional)

        [Header("Botones")]
        public Button PlayAgainButton;
        public Button MainMenuButton;

        [Header("Animación")]
        public float RevealDelayS = 0.4f;

        // ── Mostrar scores ─────────────────────────────────────────────────────

        public void Show(int localScore, int remoteScore, MissionScore scoreData)
        {
            Panel?.SetActive(true);
            StartCoroutine(RevealAnimation(localScore, remoteScore, scoreData));
        }

        IEnumerator RevealAnimation(int localScore, int remoteScore, MissionScore data)
        {
            // Reset
            if (LocalScoreBar  != null) LocalScoreBar.fillAmount  = 0f;
            if (RemoteScoreBar != null) RemoteScoreBar.fillAmount = 0f;
            if (WinnerLabel    != null) WinnerLabel.text          = "";
            if (VictoryFX      != null) VictoryFX.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            // ── Score local ────────────────────────────────────────────────────
            if (data != null)
            {
                if (LocalArrivalLabel  != null)
                    LocalArrivalLabel.text = ArrivalText(data.ArrivalPosition);
                if (LocalScienceLabel  != null)
                    LocalScienceLabel.text  = $"{data.SciencePoints} pts ciencia";
                if (LocalFuelLabel     != null)
                    LocalFuelLabel.text     = $"Combustible: {data.FuelFraction:P0}";
                if (LocalTakedownLabel != null)
                    LocalTakedownLabel.text = $"{data.TakedownCount} takedown{(data.TakedownCount != 1 ? "s" : "")}";
            }

            // Animación del score local (cuenta de 0 al total)
            yield return AnimateScore(LocalScoreLabel, LocalScoreBar, 0, localScore, 1.2f);

            yield return new WaitForSeconds(RevealDelayS);

            // ── Score rival ────────────────────────────────────────────────────
            yield return AnimateScore(RemoteScoreLabel, RemoteScoreBar, 0, remoteScore, 1.0f);

            yield return new WaitForSeconds(RevealDelayS);

            // ── Resultado final ────────────────────────────────────────────────
            ShowResult(localScore, remoteScore);
        }

        IEnumerator AnimateScore(TMP_Text label, Image bar, int from, int to, float duration)
        {
            if (label == null && bar == null) yield break;

            float elapsed = 0f;
            int   maxRef  = 5000;   // score máximo de referencia para la barra

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t   = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);  // ease-out cubic
                int   cur  = Mathf.RoundToInt(Mathf.Lerp(from, to, ease));

                if (label != null) label.text = cur.ToString("N0");
                if (bar   != null) bar.fillAmount = Mathf.Clamp01((float)cur / maxRef);
                yield return null;
            }

            if (label != null) label.text = to.ToString("N0");
            if (bar   != null) bar.fillAmount = Mathf.Clamp01((float)to / maxRef);
        }

        void ShowResult(int local, int remote)
        {
            if (WinnerLabel == null) return;

            bool win  = local  > remote;
            bool draw = local == remote;

            WinnerLabel.text = win  ? "¡VICTORIA!" :
                               draw ? "EMPATE"      : "Derrota";
            WinnerLabel.color = win  ? new Color(1f, 0.85f, 0f) :   // dorado
                                draw ? Color.white               :
                                       new Color(0.6f, 0.6f, 0.7f);

            if (WinnerSubLabel != null)
                WinnerSubLabel.text = win  ? "¡Misión completada con éxito!" :
                                      draw ? "¡Igualados al milímetro!" :
                                             "Mejor suerte la próxima, cadete.";

            if (win && VictoryFX != null)
                VictoryFX.SetActive(true);
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

        // ── Helpers ────────────────────────────────────────────────────────────

        string ArrivalText(int pos) => pos switch
        {
            1 => "#1 PRIMERO",
            2 => "#2 SEGUNDO",
            3 => "#3 TERCERO",
            _ => "Llegó",
        };
    }
}
