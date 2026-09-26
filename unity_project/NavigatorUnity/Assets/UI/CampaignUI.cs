// Navigator 2.0 — CampaignUI.cs
// NASA Program screen: the six campaign missions, their stars and what unlocks next
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    public class CampaignUI : MonoBehaviour
    {
        public GameObject Panel;
        public Button     OpenButton;       // lobby "CAMPAIGN"
        public Button     BackButton;
        public Button[]   MissionButtons;
        public TMP_Text   TotalLabel;

        void Awake()
        {
            OpenButton?.onClick.AddListener(Show);
            BackButton?.onClick.AddListener(Hide);
            for (int i = 0; i < MissionButtons.Length; i++)
            {
                int idx = i;
                MissionButtons[i]?.onClick.AddListener(() =>
                {
                    Hide();
                    GameManager.Instance?.StartCampaignMission(idx);
                });
            }
            if (Panel != null) Panel.SetActive(false);
        }

        void OnEnable()  => Loc.Changed += Refresh;
        void OnDisable() => Loc.Changed -= Refresh;

        public void Show()
        {
            if (Panel != null) Panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (Panel != null) Panel.SetActive(false);
        }

        void Refresh()
        {
            if (TotalLabel != null)
                TotalLabel.text = Loc.F("camp.total", Campaign.TotalStars(), Campaign.Missions.Length * 3);

            for (int i = 0; i < MissionButtons.Length && i < Campaign.Missions.Length; i++)
            {
                var m = Campaign.Missions[i];
                var button = MissionButtons[i];
                if (button == null) continue;
                bool open = Campaign.IsUnlocked(i);
                button.interactable = open;

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label == null) continue;
                label.text = open
                    ? $"<b>{i + 1}. {Loc.T($"camp.{m.Key}.name")}</b>   <color=#FFD633>{Campaign.StarString(Campaign.Stars(i))}</color>\n" +
                      $"<size=72%><color=#8FA3C7>{Loc.Planet(m.Planet)} · {Loc.Objective(m.Objective)} · ${m.BudgetM:N0}M · " +
                      $"{Loc.F("camp.goal1", m.StarGoals[0])}</color></size>"
                    : $"<b>{i + 1}. {Loc.T($"camp.{m.Key}.name")}</b>\n<size=72%>{Loc.F("camp.locked", i)}</size>";
            }
        }
    }
}
