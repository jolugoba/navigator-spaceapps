// Navigator 2.0 — ScoreboardUI.cs
// Final screen: score reveal, ranking of every player, campaign stars and the mission debrief.
// Waits for every rival's final score before naming a winner.
// Guacalita Inc. — SAL / Claude

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    public class ScoreboardUI : MonoBehaviour
    {
        public const int ScoreBarMax = 13000;   // ~theoretical max of a flyby (see MissionScore)

        [Header("Panel raíz")]
        public GameObject Panel;

        [Header("Jugador local")]
        public TMP_Text LocalPlayerName;
        public TMP_Text LocalScoreLabel;
        public TMP_Text LocalArrivalLabel;
        public TMP_Text LocalScienceLabel;
        public TMP_Text LocalFuelLabel;
        public TMP_Text LocalTakedownLabel;
        public Image    LocalScoreBar;

        [Header("Rivales / campaña")]
        public GameObject RemoteBlock;        // ranking in multiplayer, stars in campaign
        public TMP_Text RemotePlayerName;     // block title
        public TMP_Text RemoteScoreLabel;     // best rival score / stars
        public TMP_Text RemoteArrivalLabel;   // ranking list / goal text
        public Image    RemoteScoreBar;

        [Header("Resultado")]
        public TMP_Text WinnerLabel;
        public TMP_Text WinnerSubLabel;
        public GameObject VictoryFX;

        [Header("Informe de misión")]
        public TMP_Text DebriefLabel;

        [Header("Botones")]
        public Button   PlayAgainButton;      // retry (keeps the design)
        public Button   MainMenuButton;
        public Button   NextMissionButton;    // campaign only
        public Button   ShareButton;          // turn this mission into a challenge
        public Button   SkyButton;            // where is this planet tonight?

        [Header("Espera de rivales")]
        public float MaxRivalWaitS = 90f;     // someone may have quit: don't wait forever
        public int   RankingRows   = 6;

        [Header("Animación")]
        public float RevealDelayS = 0.4f;

        private readonly Dictionary<string, int> _rivals = new();
        private int    _expectedRivals;
        private int    _localScore;
        private string _localName = "";

        private CampaignMission _campaign;
        private int  _stars;
        private bool _improved, _hasNext;

        void Awake()
        {
            PlayAgainButton  ?.onClick.AddListener(() => GameManager.Instance?.Retry());
            MainMenuButton   ?.onClick.AddListener(() => GameManager.Instance?.ExitToLobby());
            NextMissionButton?.onClick.AddListener(() => GameManager.Instance?.NextMission());
            ShareButton?.onClick.AddListener(() => GameManager.Instance?.ShareChallenge());
            SkyButton  ?.onClick.AddListener(() =>
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.Mission != null) gm.OpenSkyFinder(gm.Mission.TargetPlanetId);
            });
        }

        public void SetCampaignResult(CampaignMission mission, int stars, bool improved, bool hasNext)
        {
            _campaign = mission; _stars = stars; _improved = improved; _hasNext = hasNext;
        }

        public void Show(int localScore, int expectedRivals, MissionScore scoreData, string failReason = null,
                         string report = null)
        {
            _localScore     = localScore;
            _expectedRivals = expectedRivals;
            _rivals.Clear();
            if (DebriefLabel != null) DebriefLabel.text = report ?? "";
            if (Panel != null) Panel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(RevealAnimation(scoreData, failReason));
        }

        // Rival final scores, keyed by display name
        public void SetRivalScores(Dictionary<string, int> scores)
        {
            foreach (var kv in scores) _rivals[kv.Key] = kv.Value;
            if (_rivals.Count > 0) RefreshRanking();
        }

        IEnumerator RevealAnimation(MissionScore data, string failReason)
        {
            bool multiplayer = _expectedRivals > 0;

            if (LocalScoreBar  != null) LocalScoreBar.fillAmount  = 0f;
            if (RemoteScoreBar != null) RemoteScoreBar.fillAmount = 0f;
            if (LocalScoreLabel  != null) LocalScoreLabel.text  = "0";
            if (RemoteScoreLabel != null) RemoteScoreLabel.text = "-";
            if (WinnerLabel    != null) WinnerLabel.text    = "";
            if (WinnerSubLabel != null) WinnerSubLabel.text = "";
            if (VictoryFX      != null) VictoryFX.SetActive(false);
            if (RemoteBlock    != null) RemoteBlock.SetActive(multiplayer || _campaign != null);
            if (NextMissionButton != null) NextMissionButton.gameObject.SetActive(false);
            if (ShareButton != null) ShareButton.gameObject.SetActive(data != null && data.ArrivalPosition > 0);
            SetButtons(false);

            if (data != null)
            {
                if (LocalArrivalLabel != null)
                    LocalArrivalLabel.text = data.ArrivalPosition > 0 ? ArrivalText(data.ArrivalPosition)
                                                                      : failReason ?? Loc.T("fail.notarrived");
                if (LocalScienceLabel  != null) LocalScienceLabel.text  = Loc.F("sb.science", data.SciencePoints);
                if (LocalFuelLabel     != null) LocalFuelLabel.text     = Loc.F("sb.fuel", data.FuelFraction);
                if (LocalTakedownLabel != null) LocalTakedownLabel.text = Loc.F("sb.takedowns", data.TakedownCount);
            }

            yield return new WaitForSeconds(0.4f);
            yield return AnimateScore(LocalScoreLabel, LocalScoreBar, 0, _localScore, 1.2f);

            if (!multiplayer)
            {
                if (_campaign != null) ShowCampaign(failReason);
                else ShowSoloResult(failReason);
                SetButtons(true);
                yield break;
            }

            // Never name a winner while someone is still flying
            if (_rivals.Count < _expectedRivals)
            {
                if (RemoteArrivalLabel != null) RemoteArrivalLabel.text = Loc.T("sb.waiting");
                if (WinnerSubLabel     != null) WinnerSubLabel.text     = Loc.T("sb.rivalflying");
                SetButtons(true);
                float waited = 0f;
                while (_rivals.Count < _expectedRivals && waited < MaxRivalWaitS)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                if (_rivals.Count == 0)
                {
                    ShowSoloResult(failReason);
                    yield break;
                }
            }

            yield return new WaitForSeconds(RevealDelayS);
            int best = _rivals.Values.Max();
            yield return AnimateScore(RemoteScoreLabel, RemoteScoreBar, 0, best, 1.0f);
            RefreshRanking();
            yield return new WaitForSeconds(RevealDelayS);

            ShowResult(best);
            if (_campaign != null) ShowCampaignButtons();
            SetButtons(true);
        }

        void RefreshRanking()
        {
            if (RemoteArrivalLabel == null || _campaign != null && _expectedRivals == 0) return;
            if (RemotePlayerName != null) RemotePlayerName.text = Loc.T("sb.ranking");

            var all = new List<(string name, int score, bool me)>();
            foreach (var kv in _rivals) all.Add((kv.Key, kv.Value, false));
            all.Add((string.IsNullOrEmpty(_localName) ? Loc.T("you") : _localName, _localScore, true));
            all.Sort((a, b) => b.score.CompareTo(a.score));

            // Big rooms: top rows, then "…" and your own place
            var sb = new StringBuilder();
            int myIndex = all.FindIndex(r => r.me);
            for (int i = 0; i < all.Count; i++)
            {
                bool show = i < RankingRows || i == myIndex;
                if (!show) { if (i == RankingRows) sb.AppendLine("..."); continue; }
                var (name, score, me) = all[i];
                string line = $"{i + 1}. {name}  {score:N0}";
                sb.AppendLine(me ? $"<color=#33CCFF><b>{line}</b></color>" : line);
            }
            RemoteArrivalLabel.text = sb.ToString().TrimEnd();
        }

        IEnumerator AnimateScore(TMP_Text label, Image bar, int from, int to, float duration)
        {
            if (label == null && bar == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                int   cur  = Mathf.RoundToInt(Mathf.Lerp(from, to, ease));
                if (label != null) label.text = cur.ToString("N0");
                if (bar   != null) bar.fillAmount = Mathf.Clamp01((float)cur / ScoreBarMax);
                yield return null;
            }
            if (label != null) label.text = to.ToString("N0");
            if (bar   != null) bar.fillAmount = Mathf.Clamp01((float)to / ScoreBarMax);
        }

        void ShowResult(int bestRival)
        {
            if (WinnerLabel == null) return;
            int place = 1 + _rivals.Values.Count(v => v > _localScore);
            bool win  = _localScore > bestRival;
            bool draw = _localScore == bestRival;

            WinnerLabel.text  = win ? Loc.T("sb.win") : draw ? Loc.T("sb.draw") : Loc.T("sb.lose");
            WinnerLabel.color = win ? UIStyle.Gold : draw ? UIStyle.Text : UIStyle.Muted;
            if (WinnerSubLabel != null)
                WinnerSubLabel.text = win  ? Loc.T("sb.win.sub")
                                    : draw ? Loc.T("sb.draw.sub")
                                    : Loc.F("sb.place", place, _rivals.Count + 1);
            if (win && VictoryFX != null) VictoryFX.SetActive(true);
        }

        void ShowSoloResult(string failReason)
        {
            if (WinnerLabel == null) return;
            bool failed = !string.IsNullOrEmpty(failReason);
            WinnerLabel.text  = failed ? Loc.T("sb.failed") : Loc.T("sb.success");
            WinnerLabel.color = failed ? UIStyle.Bad : UIStyle.Gold;
            if (WinnerSubLabel != null)
                WinnerSubLabel.text = failed ? Loc.F("sb.failed.sub", failReason) : Loc.F("sb.success.sub", _localScore);
            if (!failed && VictoryFX != null) VictoryFX.SetActive(true);
        }

        // Campaign: stars against the mission's goals, and the way forward
        void ShowCampaign(string failReason)
        {
            if (_campaign.IsChallenge) { ShowChallenge(failReason); return; }
            ShowSoloResult(failReason);
            if (RemotePlayerName != null)
                RemotePlayerName.text = Loc.F("camp.missionN", _campaign.Index + 1, Loc.T($"camp.{_campaign.Key}.name"));
            if (RemoteScoreLabel != null)
            {
                RemoteScoreLabel.text  = Campaign.StarString(_stars);
                RemoteScoreLabel.color = _stars > 0 ? UIStyle.Gold : UIStyle.Muted;
            }
            if (RemoteScoreBar != null)
                RemoteScoreBar.fillAmount = Mathf.Clamp01((float)_localScore / _campaign.StarGoals[2]);
            if (RemoteArrivalLabel != null)
            {
                string goals = Loc.F("camp.goals", _campaign.StarGoals[0], _campaign.StarGoals[1], _campaign.StarGoals[2]);
                string status = _stars == 0 ? Loc.T("camp.failedgoal")
                              : _improved ? Loc.T("camp.newbest") : "";
                if (_hasNext && _stars > 0) status += (status.Length > 0 ? " " : "") + Loc.T("camp.unlocked");
                RemoteArrivalLabel.text = $"{goals}\n{status}";
            }
            ShowCampaignButtons();
        }

        void ShowChallenge(string failReason)
        {
            bool beat = _stars > 0;
            int diff = Mathf.Abs(_localScore - _campaign.TargetScore);
            if (WinnerLabel != null)
            {
                WinnerLabel.text  = beat ? Loc.T("chal.win") : Loc.T("chal.lose");
                WinnerLabel.color = beat ? UIStyle.Gold : UIStyle.Muted;
            }
            if (WinnerSubLabel != null)
                WinnerSubLabel.text = beat ? Loc.F("chal.win.sub", _campaign.Challenger, diff)
                                           : Loc.F("chal.lose.sub", _campaign.Challenger, diff);
            if (RemotePlayerName != null) RemotePlayerName.text = Loc.F("chal.title", _campaign.Challenger);
            if (RemoteScoreLabel != null) { RemoteScoreLabel.text = _campaign.TargetScore.ToString("N0"); RemoteScoreLabel.color = UIStyle.Bad; }
            if (RemoteScoreBar   != null) RemoteScoreBar.fillAmount = Mathf.Clamp01((float)_campaign.TargetScore / ScoreBarMax);
            if (RemoteArrivalLabel != null) RemoteArrivalLabel.text = Loc.F("chal.rules", _campaign.BudgetM);
            if (beat && VictoryFX != null) VictoryFX.SetActive(true);
        }

        void ShowCampaignButtons()
        {
            if (NextMissionButton != null) NextMissionButton.gameObject.SetActive(_hasNext && _stars > 0);
        }

        void SetButtons(bool interactable)
        {
            if (PlayAgainButton   != null) PlayAgainButton.interactable   = interactable;
            if (MainMenuButton    != null) MainMenuButton.interactable    = interactable;
            if (NextMissionButton != null) NextMissionButton.interactable = interactable;
            if (ShareButton       != null) ShareButton.interactable       = interactable;
            if (SkyButton         != null) SkyButton.interactable         = interactable;
        }

        public void SetLocalPlayerName(string name)
        {
            _localName = name;
            if (LocalPlayerName != null) LocalPlayerName.text = name;
        }

        static string ArrivalText(int pos) => pos switch
        {
            1 => Loc.T("sb.pos1"),
            2 => Loc.T("sb.pos2"),
            3 => Loc.T("sb.pos3"),
            _ => Loc.F("sb.posn", pos),
        };
    }
}
