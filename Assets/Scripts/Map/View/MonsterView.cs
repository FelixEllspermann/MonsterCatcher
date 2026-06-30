using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MonsterCatcher.Battle;

namespace MonsterCatcher.Map.View
{
    // Runtime-built overlay: party list + selected monster's stats (hover for help), lore,
    // ability, and moves (click for a description popup), plus Release.
    public sealed class MonsterView : MonoBehaviour
    {
        private static readonly Color SectionBg = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color Header = new Color(0.55f, 0.68f, 0.85f);

        private Font _font;
        private GameObject _root;
        private RectTransform _canvasRt;
        private RectTransform _listCol;
        private RectTransform _detail;
        private int _selected;
        private bool _confirmingRelease;

        private GameObject _tipGo; private RectTransform _tipRt; private Text _tipText;
        private GameObject _movePopup; private Text _movePopupText;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            _root.SetActive(false);
        }

        public void Toggle()
        {
            if (_root.activeSelf) { _root.SetActive(false); return; }
            _selected = 0;
            _confirmingRelease = false;
            _root.SetActive(true);
            HideTip();
            if (_movePopup != null) _movePopup.SetActive(false);
            RebuildList();
            RebuildDetail();
        }

        public void Hide() => _root.SetActive(false);

        private void Build()
        {
            var canvasGo = new GameObject("MonsterViewCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo;
            _canvasRt = (RectTransform)canvasGo.transform;

            var dim = MakePanel(_canvasRt, new Color(0.07f, 0.08f, 0.12f, 0.98f));
            Stretch(dim.rectTransform);

            var title = MakeText(_canvasRt, 28, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(title.rectTransform, 0.05f, 0.92f, 0.85f, 0.99f);
            title.text = "Your Monsters";

            var close = MakeButton(_canvasRt, new Color(0.5f, 0.2f, 0.2f), out var clbl);
            SetAnchors((RectTransform)close.transform, 0.87f, 0.93f, 0.98f, 0.99f);
            clbl.text = "Close";
            close.onClick.AddListener(Hide);

            var listPanel = MakePanel(_canvasRt, SectionBg);
            SetAnchors(listPanel.rectTransform, 0.03f, 0.05f, 0.29f, 0.90f);
            _listCol = listPanel.rectTransform;

            var detailPanel = MakePanel(_canvasRt, new Color(1f, 1f, 1f, 0.02f));
            SetAnchors(detailPanel.rectTransform, 0.31f, 0.05f, 0.97f, 0.90f);
            _detail = detailPanel.rectTransform;

            BuildTooltip();
            BuildMovePopup();
        }

        private void RebuildList()
        {
            ClearChildren(_listCol);
            var roster = RunState.PlayerRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                int idx = i;
                var btn = MakeButton(_listCol,
                    idx == _selected ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.2f, 0.24f, 0.3f), out var lbl);
                float top = 0.98f - i * 0.095f;
                SetAnchors((RectTransform)btn.transform, 0.05f, top - 0.085f, 0.95f, top);
                var save = roster[i];
                lbl.text = save.SpeciesName + "   Lv." + save.Level;
                btn.onClick.AddListener(() =>
                {
                    _selected = idx; _confirmingRelease = false;
                    RebuildList(); RebuildDetail();
                });
            }
        }

        private void RebuildDetail()
        {
            ClearChildren(_detail);
            var roster = RunState.PlayerRoster;
            if (_selected < 0 || _selected >= roster.Count) return;
            var save = roster[_selected];
            var species = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
            if (species == null) return;

            var moves = species.MovesAtLevel(save.Level);
            var mon = new Pokemon(species, save.Level, moves, save.AbilityIds);

            // ---- header (sprite + name / type / level) ----
            var header = Section(0.0f, 0.86f, 1.0f, 1.0f, null);
            if (species.FrontSprite != null)
            {
                var sr = NewRect("Sprite", header);
                SetAnchors(sr, 0.01f, 0.05f, 0.13f, 0.95f);
                var img = sr.gameObject.AddComponent<Image>();
                img.sprite = species.FrontSprite; img.preserveAspect = true; img.raycastTarget = false;
            }
            string types = species.Type1.ToString() + (species.HasSecondType ? " / " + species.Type2 : "");
            var head = MakeText(header, 23, TextAnchor.MiddleLeft, Color.white);
            SetAnchors(head.rectTransform, 0.15f, 0f, 0.98f, 1f);
            head.text = species.DisplayName + "      " + types + "      Lv." + save.Level;

            // ---- lore ----
            var loreSec = Section(0.0f, 0.71f, 1.0f, 0.845f, "LORE");
            var lore = MakeText(loreSec, 15, TextAnchor.UpperLeft, new Color(0.8f, 0.85f, 0.9f));
            SetAnchors(lore.rectTransform, 0.02f, 0.05f, 0.98f, 0.74f);
            lore.horizontalOverflow = HorizontalWrapMode.Wrap;
            lore.text = species.LoreText;

            // ---- stats (hover for help) ----
            var statSec = Section(0.0f, 0.37f, 0.49f, 0.695f, "STATS  (hover)");
            AddStatRow(statSec, 0, "HP", mon.MaxHp, Stat.Hp);
            AddStatRow(statSec, 1, "Attack", mon.EffectiveStat(Stat.Attack), Stat.Attack);
            AddStatRow(statSec, 2, "Defense", mon.EffectiveStat(Stat.Defense), Stat.Defense);
            AddStatRow(statSec, 3, "Sp. Atk", mon.EffectiveStat(Stat.SpAttack), Stat.SpAttack);
            AddStatRow(statSec, 4, "Sp. Def", mon.EffectiveStat(Stat.SpDefense), Stat.SpDefense);
            AddStatRow(statSec, 5, "Speed", mon.EffectiveStat(Stat.Speed), Stat.Speed);

            // ---- ability ----
            var abilSec = Section(0.51f, 0.37f, 1.0f, 0.695f, "ABILITY");
            var abilSb = new StringBuilder();
            foreach (var id in save.AbilityIds)
            {
                var info = AbilityCatalog.ById(id);
                if (info != null) abilSb.Append((abilSb.Length > 0 ? "\n\n" : "") + info.Name + "\n" + info.Description);
            }
            var abil = MakeText(abilSec, 15, TextAnchor.UpperLeft, new Color(0.96f, 0.86f, 0.5f));
            SetAnchors(abil.rectTransform, 0.04f, 0.05f, 0.97f, 0.74f);
            abil.horizontalOverflow = HorizontalWrapMode.Wrap;
            abil.text = abilSb.ToString();

            // ---- moves (click for details) ----
            var moveSec = Section(0.0f, 0.12f, 1.0f, 0.355f, "MOVES  (click for details)");
            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                var btn = MakeButton(moveSec, new Color(0.18f, 0.26f, 0.34f), out var lbl);
                float top = 0.80f - i * 0.20f;
                SetAnchors((RectTransform)btn.transform, 0.02f, top - 0.18f, 0.98f, top);
                lbl.alignment = TextAnchor.MiddleLeft;
                var rt = (RectTransform)lbl.transform;
                rt.offsetMin = new Vector2(10f, rt.offsetMin.y);
                lbl.text = m.DisplayName + "    " + m.Type + "/" + m.Category
                    + "    Pow " + (m.Power > 0 ? m.Power.ToString() : "-")
                    + "    Acc " + (m.Accuracy > 0 ? m.Accuracy.ToString() : "-");
                var move = m;
                btn.onClick.AddListener(() => ShowMovePopup(move));
            }

            // ---- release ----
            bool canRelease = roster.Count > 1;
            if (!_confirmingRelease)
            {
                var rel = MakeButton(_detail,
                    canRelease ? new Color(0.5f, 0.25f, 0.25f) : new Color(0.3f, 0.3f, 0.3f), out var rlbl);
                SetAnchors((RectTransform)rel.transform, 0.0f, 0.0f, 0.26f, 0.10f);
                rlbl.text = "Release";
                rel.interactable = canRelease;
                rel.onClick.AddListener(() => { _confirmingRelease = true; RebuildDetail(); });
            }
            else
            {
                var q = MakeText(_detail, 15, TextAnchor.MiddleLeft, Color.white);
                SetAnchors(q.rectTransform, 0.0f, 0.0f, 0.40f, 0.10f);
                q.text = "Release " + species.DisplayName + "?";
                var yes = MakeButton(_detail, new Color(0.55f, 0.25f, 0.25f), out var ylbl);
                SetAnchors((RectTransform)yes.transform, 0.42f, 0.0f, 0.57f, 0.10f);
                ylbl.text = "Yes";
                yes.onClick.AddListener(() =>
                {
                    if (RunState.ReleaseMonster(_selected)) _selected = 0;
                    _confirmingRelease = false;
                    RebuildList(); RebuildDetail();
                });
                var no = MakeButton(_detail, new Color(0.3f, 0.35f, 0.3f), out var nlbl);
                SetAnchors((RectTransform)no.transform, 0.59f, 0.0f, 0.73f, 0.10f);
                nlbl.text = "No";
                no.onClick.AddListener(() => { _confirmingRelease = false; RebuildDetail(); });
            }
        }

        private RectTransform Section(float a, float b, float c, float d, string header)
        {
            var panel = MakePanel(_detail, SectionBg);
            SetAnchors(panel.rectTransform, a, b, c, d);
            if (!string.IsNullOrEmpty(header))
            {
                var h = MakeText(panel.rectTransform, 12, TextAnchor.UpperLeft, Header);
                SetAnchors(h.rectTransform, 0.02f, 0.78f, 0.98f, 0.99f);
                h.text = header;
            }
            return panel.rectTransform;
        }

        private void AddStatRow(RectTransform parent, int i, string name, int value, Stat stat)
        {
            var row = MakeText(parent, 15, TextAnchor.MiddleLeft, Color.white);
            float top = 0.77f - i * 0.125f;
            SetAnchors(row.rectTransform, 0.04f, top - 0.12f, 0.97f, top);
            row.raycastTarget = true;   // hoverable
            row.text = name + "   " + value;
            AddHover(row.gameObject, () => StatDescription(stat));
        }

        private static string StatDescription(Stat s)
        {
            switch (s)
            {
                case Stat.Hp: return "HP - how much damage it can take before fainting.";
                case Stat.Attack: return "Attack - raises the damage of physical moves.";
                case Stat.Defense: return "Defense - reduces damage taken from physical moves.";
                case Stat.SpAttack: return "Sp. Attack - raises the damage of special moves.";
                case Stat.SpDefense: return "Sp. Defense - reduces damage taken from special moves.";
                case Stat.Speed: return "Speed - the faster monster moves first each turn.";
                default: return "";
            }
        }

        private static string MoveDescription(MoveData m)
        {
            var sb = new StringBuilder();
            sb.Append(m.Type + " / " + m.Category + "\n");
            sb.Append("Power: " + (m.Power > 0 ? m.Power.ToString() : "-")
                + "      Accuracy: " + (m.Accuracy > 0 ? m.Accuracy + "%" : "never misses")
                + "      PP: " + m.MaxPp);
            var fx = new List<string>();
            if (m.Category == MoveCategory.Status) fx.Add("A status move - deals no direct damage.");
            if (m.ChargesUp) fx.Add("Charges on the first turn, then strikes on the second.");
            if (m.RecoilPercent > 0) fx.Add("The user takes " + m.RecoilPercent + "% of the damage dealt as recoil.");
            if (m.DrainPercent > 0) fx.Add("Heals the user for " + m.DrainPercent + "% of the damage dealt.");
            if (m.HighCrit) fx.Add("Has a high critical-hit ratio.");
            if (m.Priority > 0) fx.Add("Strikes first (+" + m.Priority + " priority).");
            if (m.Priority < 0) fx.Add("Strikes last (" + m.Priority + " priority).");
            if (m.InflictsStatus != StatusCondition.None && m.StatusChance > 0)
                fx.Add(m.StatusChance + "% chance to inflict " + m.InflictsStatus + ".");
            if (m.StatStageDelta != 0 && m.StatChangeChance > 0)
            {
                string who = m.StatChangeTargetsSelf ? "the user's" : "the target's";
                string dir = m.StatStageDelta > 0 ? "raises" : "lowers";
                string chance = m.StatChangeChance >= 100 ? "" : m.StatChangeChance + "% chance to ";
                fx.Add(chance + dir + " " + who + " " + m.StatToChange + " by " + System.Math.Abs(m.StatStageDelta) + ".");
            }
            foreach (var f in fx) sb.Append("\n\n" + f);
            return sb.ToString();
        }

        // ---- tooltip (pointer-following) ----
        private void BuildTooltip()
        {
            var p = MakePanel(_canvasRt, new Color(0f, 0f, 0f, 0.93f));
            _tipRt = p.rectTransform;
            _tipRt.anchorMin = _tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            _tipRt.pivot = new Vector2(0f, 1f);
            _tipRt.sizeDelta = new Vector2(360f, 58f);
            p.raycastTarget = false;
            _tipText = MakeText(_tipRt, 14, TextAnchor.MiddleLeft, Color.white);
            SetAnchors(_tipText.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
            _tipGo = p.gameObject;
            _tipGo.SetActive(false);
        }

        private void ShowTip(string text, Vector2 screenPos)
        {
            if (string.IsNullOrEmpty(text) || _tipRt == null) return;
            _tipText.text = text;
            _tipRt.SetAsLastSibling();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, screenPos, null, out var local))
                _tipRt.anchoredPosition = local + new Vector2(14f, -14f);
            _tipGo.SetActive(true);
        }

        private void HideTip() { if (_tipGo != null) _tipGo.SetActive(false); }

        private void AddHover(GameObject target, System.Func<string> textProvider)
        {
            var et = target.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(d => ShowTip(textProvider(), ((PointerEventData)d).position));
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(d => HideTip());
            et.triggers.Add(exit);
        }

        // ---- move-description popup ----
        private void BuildMovePopup()
        {
            var p = MakePanel(_canvasRt, new Color(0.10f, 0.12f, 0.17f, 0.99f));
            SetAnchors(p.rectTransform, 0.30f, 0.28f, 0.70f, 0.74f);
            _movePopupText = MakeText(p.rectTransform, 16, TextAnchor.UpperLeft, Color.white);
            SetAnchors(_movePopupText.rectTransform, 0.05f, 0.18f, 0.95f, 0.93f);
            _movePopupText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var close = MakeButton(p.rectTransform, new Color(0.4f, 0.25f, 0.25f), out var clbl);
            SetAnchors((RectTransform)close.transform, 0.38f, 0.05f, 0.62f, 0.14f);
            clbl.text = "Close";
            close.onClick.AddListener(() => _movePopup.SetActive(false));
            _movePopup = p.gameObject;
            _movePopup.SetActive(false);
        }

        private void ShowMovePopup(MoveData m)
        {
            _movePopupText.text = m.DisplayName + "\n\n" + MoveDescription(m);
            ((RectTransform)_movePopup.transform).SetAsLastSibling();
            _movePopup.SetActive(true);
        }

        // ---- factories ----
        private static void ClearChildren(RectTransform rt)
        {
            for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private Text MakeText(Transform parent, int size, TextAnchor anchor, Color color)
        {
            var rt = NewRect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.alignment = anchor; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Image MakePanel(Transform parent, Color color)
        {
            var rt = NewRect("Panel", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Button MakeButton(Transform parent, Color color, out Text label)
        {
            var rt = NewRect("Button", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            label = MakeText(rt, 18, TextAnchor.MiddleCenter, Color.white);
            Stretch(label.rectTransform);
            return btn;
        }

        private static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rt) => SetAnchors(rt, 0f, 0f, 1f, 1f);
    }
}
