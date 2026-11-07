using System;
using System.Collections.Generic;
using Barry.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Barry.UI
{
    /// <summary>Actions list from /actions. Collapsed state shows only the "ACTIONS · B" tab.</summary>
    public sealed class ActionsPanelView : MonoBehaviour
    {
        [SerializeField] GameObject expanded;
        [SerializeField] GameObject collapsedTab;
        [SerializeField] TMP_Text collapsedLabel;
        [SerializeField] RectTransform listRoot;
        [SerializeField] Button itemTemplate;      // inactive template, cloned per action
        [SerializeField] TMP_Text footer;

        readonly List<Button> spawned = new List<Button>();
        ActionsPanelModel model;

        public event Action<int> ActionChosen;
        public event Action<int?> ActionHovered;

        void OnEnable() { ThemeApplier.Changed += OnTheme; }
        void OnDisable() { ThemeApplier.Changed -= OnTheme; }
        void OnTheme(ThemeTokens _) { if (model != null) Bind(model); }

        public void Bind(ActionsPanelModel m)
        {
            model = m;
            expanded.SetActive(!m.IsCollapsed);
            collapsedTab.SetActive(m.IsCollapsed);
            collapsedLabel.text = ActionsPanelModel.CollapsedTab;
            footer.text = ActionsPanelModel.Footer;

            foreach (var b in spawned) if (b != null) Destroy(b.gameObject);
            spawned.Clear();
            if (m.IsCollapsed) return;

            var k = ThemeApplier.Current;
            foreach (var item in m.Items)
            {
                var b = Instantiate(itemTemplate, listRoot);
                b.gameObject.SetActive(true);
                int id = item.Id;
                var label = b.GetComponentInChildren<TMP_Text>();
                label.text = item.IsRunning ? item.Name + " · running…" : item.Name;
                b.interactable = item.IsEnabled;
                var img = b.GetComponent<Image>();
                if (k != null)
                {
                    img.color = item.IsHovered ? k.accentSoft : k.neutralFill;
                    label.color = item.IsEnabled || item.IsRunning ? k.textPrimary : k.textSecondary;
                }
                b.onClick.AddListener(() => ActionChosen?.Invoke(id));
                var trig = b.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trig, EventTriggerType.PointerEnter, () => ActionHovered?.Invoke(id));
                AddTrigger(trig, EventTriggerType.PointerExit, () => ActionHovered?.Invoke(null));
                spawned.Add(b);
            }
        }

        static void AddTrigger(EventTrigger t, EventTriggerType type, Action a)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(_ => a());
            t.triggers.Add(e);
        }
    }
}
