using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MonsterCatcher.Battle;

namespace MonsterCatcher.Map.View
{
    // Runtime-built overlay (same uGUI pattern as MapController): party list on the left,
    // selected monster's sprite/stats/lore/moves/ability on the right, plus Release.
    public sealed class MonsterView : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private RectTransform _listCol;
        private RectTransform _detail;
        private int _selected;
        private bool _confirmingRelease;

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
            var rt = (RectTransform)canvasGo.transform;

            var dim = MakePanel(rt, new Color(0.05f, 0.06f, 0.09f, 0.97f));
            Stretch(dim.rectTransform);

            var title = MakeText(rt, 28, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(title.rectTransform, 0.05f, 0.92f, 0.85f, 0.99f);
            title.text = "Your Monsters";

            var close = MakeButton(rt, new Color(0.5f, 0.2f, 0.2f), out var clbl);
            SetAnchors((RectTransform)close.transform, 0.87f, 0.93f, 0.98f, 0.99f);
            clbl.text = "Close";
            close.onClick.AddListener(Hide);

            var listPanel = MakePanel(rt, new Color(1f, 1f, 1f, 0.04f));
            SetAnchors(listPanel.rectTransform, 0.03f, 0.05f, 0.30f, 0.90f);
            _listCol = listPanel.rectTransform;

            var detailPanel = MakePanel(rt, new Color(1f, 1f, 1f, 0.04f));
            SetAnchors(detailPanel.rectTransform, 0.32f, 0.05f, 0.97f, 0.90f);
            _detail = detailPanel.rectTransform;
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

            if (species.FrontSprite != null)
            {
                var sr = NewRect("Sprite", _detail);
                SetAnchors(sr, 0.03f, 0.80f, 0.19f, 0.98f);
                var img = sr.gameObject.AddComponent<Image>();
                img.sprite = species.FrontSprite; img.preserveAspect = true; img.raycastTarget = false;
            }

            string types = species.Type1.ToString() + (species.HasSecondType ? "/" + species.Type2 : "");
            var head = MakeText(_detail, 22, TextAnchor.UpperLeft, Color.white);
            SetAnchors(head.rectTransform, 0.21f, 0.87f, 0.98f, 0.99f);
            head.text = species.DisplayName + "    " + types + "    Lv." + save.Level;

            var lore = MakeText(_detail, 15, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.9f));
            SetAnchors(lore.rectTransform, 0.21f, 0.73f, 0.98f, 0.87f);
            lore.horizontalOverflow = HorizontalWrapMode.Wrap;
            lore.text = species.LoreText;

            var stats = MakeText(_detail, 16, TextAnchor.UpperLeft, Color.white);
            SetAnchors(stats.rectTransform, 0.03f, 0.48f, 0.50f, 0.72f);
            stats.text =
                "HP  " + mon.MaxHp +
                "\nAtk " + mon.EffectiveStat(Stat.Attack) +
                "\nDef " + mon.EffectiveStat(Stat.Defense) +
                "\nSpA " + mon.EffectiveStat(Stat.SpAttack) +
                "\nSpD " + mon.EffectiveStat(Stat.SpDefense) +
                "\nSpe " + mon.EffectiveStat(Stat.Speed);

            var abilSb = new StringBuilder("Ability");
            foreach (var id in save.AbilityIds)
            {
                var info = AbilityCatalog.ById(id);
                if (info != null) abilSb.Append("\n" + info.Name + "\n  " + info.Description);
            }
            var abil = MakeText(_detail, 15, TextAnchor.UpperLeft, new Color(0.96f, 0.86f, 0.5f));
            SetAnchors(abil.rectTransform, 0.51f, 0.48f, 0.98f, 0.72f);
            abil.horizontalOverflow = HorizontalWrapMode.Wrap;
            abil.text = abilSb.ToString();

            var mvSb = new StringBuilder("Moves");
            foreach (var m in moves)
                mvSb.Append("\n" + m.DisplayName + " - " + m.Type + "/" + m.Category
                    + "  Pow " + (m.Power > 0 ? m.Power.ToString() : "-")
                    + "  Acc " + (m.Accuracy > 0 ? m.Accuracy.ToString() : "-")
                    + MoveEffectNote(m));
            var mv = MakeText(_detail, 15, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 0.95f));
            SetAnchors(mv.rectTransform, 0.03f, 0.14f, 0.98f, 0.47f);
            mv.horizontalOverflow = HorizontalWrapMode.Wrap;
            mv.text = mvSb.ToString();

            bool canRelease = roster.Count > 1;
            if (!_confirmingRelease)
            {
                var rel = MakeButton(_detail,
                    canRelease ? new Color(0.5f, 0.25f, 0.25f) : new Color(0.3f, 0.3f, 0.3f), out var rlbl);
                SetAnchors((RectTransform)rel.transform, 0.03f, 0.02f, 0.28f, 0.11f);
                rlbl.text = "Release";
                rel.interactable = canRelease;
                rel.onClick.AddListener(() => { _confirmingRelease = true; RebuildDetail(); });
            }
            else
            {
                var q = MakeText(_detail, 15, TextAnchor.MiddleLeft, Color.white);
                SetAnchors(q.rectTransform, 0.03f, 0.02f, 0.40f, 0.11f);
                q.text = "Release " + species.DisplayName + "?";
                var yes = MakeButton(_detail, new Color(0.55f, 0.25f, 0.25f), out var ylbl);
                SetAnchors((RectTransform)yes.transform, 0.42f, 0.02f, 0.57f, 0.11f);
                ylbl.text = "Yes";
                yes.onClick.AddListener(() =>
                {
                    if (RunState.ReleaseMonster(_selected)) _selected = 0;
                    _confirmingRelease = false;
                    RebuildList(); RebuildDetail();
                });
                var no = MakeButton(_detail, new Color(0.3f, 0.35f, 0.3f), out var nlbl);
                SetAnchors((RectTransform)no.transform, 0.59f, 0.02f, 0.73f, 0.11f);
                nlbl.text = "No";
                no.onClick.AddListener(() => { _confirmingRelease = false; RebuildDetail(); });
            }
        }

        private static string MoveEffectNote(MoveData m)
        {
            var parts = new List<string>();
            if (m.ChargesUp) parts.Add("2-turn");
            if (m.RecoilPercent > 0) parts.Add("recoil " + m.RecoilPercent + "%");
            if (m.DrainPercent > 0) parts.Add("drain " + m.DrainPercent + "%");
            if (m.HighCrit) parts.Add("high-crit");
            if (m.Priority != 0) parts.Add("prio " + m.Priority);
            if (m.InflictsStatus != StatusCondition.None && m.StatusChance > 0)
                parts.Add(m.StatusChance + "% " + m.InflictsStatus);
            if (m.StatStageDelta != 0 && m.StatChangeChance > 0)
                parts.Add((m.StatChangeTargetsSelf ? "self " : "foe ") + m.StatToChange
                    + (m.StatStageDelta > 0 ? "+" : "") + m.StatStageDelta);
            return parts.Count == 0 ? "" : "  [" + string.Join(", ", parts) + "]";
        }

        // ---- factories (mirror MapController) ----
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
