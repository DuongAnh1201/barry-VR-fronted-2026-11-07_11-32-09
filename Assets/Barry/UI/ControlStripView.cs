using System;
using Barry.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Barry.UI
{
    public sealed class ControlStripView : MonoBehaviour
    {
        public static readonly string[] Levels = { "LOW", "MEDIUM", "HIGH" };

        [SerializeField] Button[] levelButtons = new Button[3];   // LOW, MEDIUM, HIGH
        [SerializeField] Button stopButton;
        [SerializeField] Button exitButton;
        [SerializeField] TMP_Text stopHint;

        string selected = "MEDIUM";

        public event Action<string> IntensityChosen;
        public event Action StopClicked;
        public event Action ExitClicked;

        void Awake()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                string level = Levels[i];
                levelButtons[i].onClick.AddListener(() => IntensityChosen?.Invoke(level));
            }
            stopButton.onClick.AddListener(() => StopClicked?.Invoke());
            exitButton.onClick.AddListener(() => ExitClicked?.Invoke());
            if (stopHint != null) stopHint.text = "A button · or point + trigger";
        }

        void OnEnable() { ThemeApplier.Changed += OnTheme; Render(); }
        void OnDisable() { ThemeApplier.Changed -= OnTheme; }
        void OnTheme(ThemeTokens _) => Render();

        public void SetIntensity(string level) { selected = level; Render(); }

        void Render()
        {
            var k = ThemeApplier.Current;
            if (k == null) return;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool on = Levels[i] == selected;
                levelButtons[i].GetComponent<Image>().color = on ? k.accent : k.neutralFill;
                levelButtons[i].GetComponentInChildren<TMP_Text>().color = on ? Color.white : k.textPrimary;
            }
            stopButton.GetComponent<Image>().color = k.dangerSolid;   // always solid danger + white text
            stopButton.GetComponentInChildren<TMP_Text>().color = k.stopText;
        }
    }
}
