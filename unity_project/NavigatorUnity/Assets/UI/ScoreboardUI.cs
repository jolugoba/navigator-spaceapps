// Navigator 2.0 â€” ScoreboardUI.cs
// Pantalla final: scores de ambos jugadores con animaciÃ³n de reveal
// Guacalita Inc. â€” SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    public class ScoreboardUI : MonoBehaviour
    {
        [Header("Panel raÃ­z")]
        public GameObject Panel;

        [Header("Jugador local")]
        public TMP_Text LocalPlayerName;
        public TMP_Text LocalScoreLabel;      // "4,650"
        public TMP_Text LocalArrivalLabel;    // "#1 ðŸ†"
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
        public TMP_Text WinnerLabel;          // "Â¡VICTORIA!" / "Derrota" / "EMPATE"
        public TMP_Text WinnerSubLabel;       // "Â¡MisiÃ³n completada!" / "Mejor suerte la prÃ³xima"
        public GameObject VictoryFX;          // partÃ­culas celebraciÃ³n (opcional)

        [Header("Botones")]
        public Button PlayAgainButton;
        public Button MainMenuButton;

        [Header("AnimaciÃ³n")]
        public float RevealDelayS = 0.4f;

        // â”€â”€ Mostrar scores â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void Show(int localScore, int remoteScore, MissionScore scoreData)
        {
            if (Panel != null) Panel.SetActive(true);
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

            // â”€â”€ Score local â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

            // AnimaciÃ³n del score local (cuenta de 0 al total)
            yield return AnimateScore(LocalScoreLabel, LocalScoreBar, 0, localScore, 1.2f);

            yield return new WaitForSeconds(RevealDelayS);

            // â”€â”€ Score rival â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            yield return AnimateScore(RemoteScoreLabel, RemoteScoreBar, 0, remoteScore, 1.0f);

            yield return new WaitForSeconds(RevealDelayS);

            // â”€â”€ Resultado final â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            ShowResult(localScore, remoteScore);
        }

        IEnumerator AnimateScore(TMP_Text label, Image bar, int from, int to, float duration)
        {
            if (label == null && bar == null) yield break;

            float elapsed = 0f;
            int   maxRef  = 5000;   // score mÃ¡ximo de referencia para la barra

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

            WinnerLabel.text = win  ? "Â¡VICTORIA!" :
                               draw ? "EMPATE"      : "Derrota";
            WinnerLabel.color = win  ? new Color(1f, 0.85f, 0f) :   // dorado
                                draw ? Color.white               :
                                       new Color(0.6f, 0.6f, 0.7f);

            if (WinnerSubLabel != null)
                WinnerSubLabel.text = win  ? "Â¡MisiÃ³n completada con Ã©xito!" :
                                      draw ? "Â¡Igualados al milÃ­metro!" :
                                             "Mejor suerte la prÃ³xima, cadete.";

            if (win && VictoryFX != null)
                VictoryFX.SetActive(true);
        }

        // â”€â”€ Setters externos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void SetLocalPlayerName(string name)
        {
            if (LocalPlayerName != null) LocalPlayerName.text = name;
        }

        public void SetRemotePlayerName(string name)
        {
            if (RemotePlayerName != null) RemotePlayerName.text = name;
        }

        // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        string ArrivalText(int pos) => pos switch
        {
            1 => "#1 PRIMERO",
            2 => "#2 SEGUNDO",
            3 => "#3 TERCERO",
            _ => "LlegÃ³",
        };
    }
}
