using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using MonsterCatcher.Battle;

namespace MonsterCatcher.Map.View
{
    // Runtime-built event overlay: pick one of three offered events, optionally target a
    // monster, then see the result. Three stages share one canvas; the body panel is
    // rebuilt per stage. Effects call RunState helpers; species/HP ops happen here.
    public sealed class EventView : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private RectTransform _canvasRt;
        private RectTransform _body;
        private Text _title;
        private int _nodeId;
        private System.Action _onLeave;
        private System.Random _rng;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            Build();
            _root.SetActive(false);
        }

        public void Open(int nodeId, System.Action onLeave)
        {
            _nodeId = nodeId;
            _onLeave = onLeave;
            _rng = new System.Random(nodeId);
            _root.SetActive(true);
            ShowChoosing();
        }

        private void Build()
        {
            var canvasGo = new GameObject("EventCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo;
            _canvasRt = (RectTransform)canvasGo.transform;

            var dim = MakePanel(_canvasRt, new Color(0.06f, 0.07f, 0.10f, 0.98f));
            Stretch(dim.rectTransform);

            _title = MakeText(_canvasRt, 28, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(_title.rectTransform, 0.20f, 0.92f, 0.80f, 0.99f);
            _title.text = "Event";

            var bodyPanel = MakePanel(_canvasRt, new Color(1f, 1f, 1f, 0.04f));
            SetAnchors(bodyPanel.rectTransform, 0.10f, 0.08f, 0.90f, 0.88f);
            _body = bodyPanel.rectTransform;
        }

        // ---- stage 1: choosing ----
        private void ShowChoosing()
        {
            _title.text = "Event";
            ClearChildren(_body);

            var applicable = new List<string>();
            foreach (var info in EventCatalog.All)
            {
                if (IsApplicable(info)) applicable.Add(info.Id);
            }

            var offer = EventCatalog.RandomOffer(_nodeId, 3, applicable);
            for (int i = 0; i < offer.Count; i++)
            {
                var info = EventCatalog.ById(offer[i]);
                if (info == null) continue;
                float top = 0.94f - i * 0.31f;

                var btn = MakeButton(_body, new Color(0.22f, 0.30f, 0.42f), out var lbl);
                SetAnchors((RectTransform)btn.transform, 0.04f, top - 0.27f, 0.96f, top);

                lbl.alignment = TextAnchor.UpperLeft;
                lbl.fontSize = 20;
                lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                var lrt = (RectTransform)lbl.transform;
                lrt.offsetMin = new Vector2(14f, 10f);
                lrt.offsetMax = new Vector2(-14f, -10f);
                lbl.text = info.Name + "\n\n" + info.Description;

                var picked = info;
                btn.onClick.AddListener(() =>
                {
                    if (picked.NeedsMonsterTarget) ShowTargeting(picked);
                    else ShowResult(Apply(picked, -1));
                });
            }
        }

        private bool IsApplicable(EventInfo info)
        {
            switch (info.Condition)
            {
                case EventCondition.None:
                    return true;
                case EventCondition.TeamAboveOne:
                    return RunState.TeamAboveOne();
                case EventCondition.HasItems:
                    return RunState.HasAnyItem();
                case EventCondition.HasEvolvableNow:
                    return AnyRoster((sp, save) => sp.CanEvolveAt(save.Level));
                case EventCondition.HasAnyEvolution:
                    return AnyRoster((sp, save) => sp.EvolvesInto != null);
                default:
                    return false;
            }
        }

        private static bool AnyRoster(System.Func<SpeciesData, MonsterSave, bool> pred)
        {
            var roster = RunState.PlayerRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                var save = roster[i];
                var sp = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
                if (sp == null) continue;
                if (pred(sp, save)) return true;
            }
            return false;
        }

        // ---- stage 2: targeting ----
        private void ShowTargeting(EventInfo info)
        {
            _title.text = "Choose a monster";
            ClearChildren(_body);

            var roster = RunState.PlayerRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                int idx = i;
                var save = roster[i];
                var sp = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);

                bool enabled;
                if (info.Id == "EvolutionCatalyst") enabled = sp != null && sp.CanEvolveAt(save.Level);
                else if (info.Id == "RecklessEvolution") enabled = sp != null && sp.EvolvesInto != null;
                else enabled = true;

                var btn = MakeButton(_body,
                    enabled ? new Color(0.22f, 0.30f, 0.42f) : new Color(0.30f, 0.30f, 0.30f), out var lbl);
                float top = 0.96f - i * 0.105f;
                SetAnchors((RectTransform)btn.transform, 0.05f, top - 0.095f, 0.95f, top);
                lbl.text = save.SpeciesName + "   Lv." + save.Level;
                btn.interactable = enabled;
                btn.onClick.AddListener(() => ShowResult(Apply(info, idx)));
            }

            var back = MakeButton(_body, new Color(0.40f, 0.32f, 0.28f), out var blbl);
            SetAnchors((RectTransform)back.transform, 0.05f, 0.01f, 0.30f, 0.09f);
            blbl.text = "Back";
            back.onClick.AddListener(ShowChoosing);
        }

        // ---- stage 3: result ----
        private void ShowResult(string message)
        {
            _title.text = "Event";
            ClearChildren(_body);

            var text = MakeText(_body, 20, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(text.rectTransform, 0.05f, 0.30f, 0.95f, 0.92f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = message;

            var cont = MakeButton(_body, new Color(0.25f, 0.45f, 0.32f), out var clbl);
            SetAnchors((RectTransform)cont.transform, 0.38f, 0.06f, 0.62f, 0.18f);
            clbl.text = "Continue";
            cont.onClick.AddListener(() =>
            {
                RunState.VisitEvent(_nodeId);
                _root.SetActive(false);
                _onLeave?.Invoke();
            });
        }

        // ---- effects ----
        private string Apply(EventInfo info, int t)
        {
            switch (info.Id)
            {
                // ---- pure boons ----
                case "MindExpansion":
                    RunState.ExpandRoster(1);
                    return "Your mind expands - you can now carry one more monster.";
                case "AncientAwakening":
                {
                    RunState.GrantRandomAbility(t, _nodeId * 7 + t * 13);
                    return Name(t) + " awakens an ancient power and learns a new ability!";
                }
                case "TrainingGrounds":
                    RunState.AddLevels(t, +4);
                    return Name(t) + " trained hard and gained +4 levels!";
                case "WarDrums":
                    RunState.AddLevelsAll(+2);
                    return "The war drums sound - your whole team gains +2 levels!";
                case "SacredSpring":
                    RunState.HealParty();
                    return "The sacred spring restores your whole team to full health.";
                case "HiddenCache":
                    RunState.AddGold(75);
                    return "You found a hidden cache of 75 gold!";
                case "SupplyDrop":
                    RunState.AddItem("Potion", 2);
                    RunState.AddItem("MonsterCatcher", 1);
                    RunState.AddItem("Revive", 1);
                    return "A supply drop! You receive 2 Potions, a Monster Catcher and a Revive.";
                case "EvolutionCatalyst":
                {
                    var (from, to) = Evolve(t, false);
                    return from == null ? Name(t) + " can't evolve." : from + " evolved into " + to + "!";
                }
                case "LuckyVein":
                    RunState.AddGold(130);
                    return "You strike a lucky vein and pocket 130 gold!";
                case "MentorsGift":
                {
                    RunState.AddLevels(t, +2);
                    RunState.GrantRandomAbility(t, _nodeId * 7 + t * 13);
                    return Name(t) + " gains +2 levels and learns a new ability!";
                }
                case "TwinDrills":
                {
                    int count = RunState.PlayerRoster.Count;
                    if (count <= 1)
                    {
                        RunState.AddLevels(0, +3);
                        return Name(0) + " drills hard and gains +3 levels!";
                    }
                    int a = _rng.Next(count);
                    int b = _rng.Next(count - 1);
                    if (b >= a) b++;
                    RunState.AddLevels(a, +3);
                    RunState.AddLevels(b, +3);
                    return Name(a) + " and " + Name(b) + " each gain +3 levels from intense drilling!";
                }
                case "Quartermaster":
                {
                    int n = ItemCatalog.All.Count;
                    for (int i = 0; i < 4; i++)
                        RunState.AddItem(ItemCatalog.All[_rng.Next(n)].Id, 1);
                    return "The quartermaster hands you 4 assorted items.";
                }

                // ---- trade-offs ----
                case "BloodPact":
                    RunState.AddLevelsAll(+3);
                    DamageAllToFraction(0.5);
                    return "A blood pact: your team gains +3 levels, but everyone drops to 50% HP.";
                case "CursedRiches":
                {
                    RunState.AddGold(150);
                    int idx = _rng.Next(RunState.PlayerRoster.Count);
                    RunState.AddLevels(idx, -2);
                    return "You claim 150 cursed gold, but " + Name(idx) + " loses 2 levels.";
                }
                case "ForbiddenTome":
                {
                    int seed = _nodeId * 7 + t * 13;
                    RunState.GrantRandomAbility(t, seed);
                    RunState.GrantRandomAbility(t, seed + 101);
                    RunState.AddLevels(t, -3);
                    return Name(t) + " learns two abilities from the forbidden tome, but loses 3 levels.";
                }
                case "SacrificialRite":
                {
                    string released = Name(t);
                    RunState.ReleaseMonster(t);
                    RunState.AddLevelsAll(+5);
                    return "You release " + released + ". The survivors each gain +5 levels.";
                }
                case "DevilsBargain":
                    RunState.ExpandRoster(1);
                    RunState.AddGold(100);
                    RunState.AddLevelsAll(-1);
                    return "The devil grants a roster slot and 100 gold, but the team loses 1 level each.";
                case "RecklessEvolution":
                {
                    var (from, to) = Evolve(t, true);
                    RunState.AddLevels(t, -2);
                    return (from == null ? Name(t) + " can't evolve" : from + " is forced to evolve into " + to)
                        + ", but loses 2 levels.";
                }
                case "GlassCannonBrew":
                {
                    RunState.AddLevels(t, +6);
                    SetHpAbsolute(t, 1);
                    return Name(t) + " gains +6 levels, but is left at 1 HP.";
                }
                case "SoulTax":
                {
                    RunState.GrantRandomAbility(t, _nodeId * 7 + t * 13);
                    RunState.AddLevelsAll(-1);
                    return Name(t) + " learns a new ability, but the whole team pays 1 level.";
                }
                case "PawnEverything":
                    RunState.LoseRandomItemType(_nodeId);
                    RunState.AddGold(100);
                    return "You pawn off a stack of items for 100 gold.";
                case "PhoenixRite":
                    RunState.HealParty();
                    RunState.SpendGoldClamped(80);
                    return "The phoenix rite fully heals your team and costs you up to 80 gold.";

                // ---- gambles ----
                case "GamblersDice":
                    if (_rng.NextDouble() < 0.6)
                    {
                        RunState.AddGold(200);
                        return "The dice land in your favor - you win 200 gold!";
                    }
                    RunState.SpendGoldClamped(60);
                    return "The dice betray you - you lose up to 60 gold.";
                case "MysteryBox":
                {
                    if (_rng.NextDouble() < 0.5)
                    {
                        int seed = _nodeId * 7 + t * 13;
                        RunState.GrantRandomAbility(t, seed);
                        RunState.GrantRandomAbility(t, seed + 101);
                        return "The mystery box pays off - " + Name(t) + " learns two abilities!";
                    }
                    RunState.AddLevels(t, -4);
                    return "The mystery box backfires - " + Name(t) + " loses 4 levels.";
                }

                default:
                    return "Nothing happens.";
            }
        }

        // ---- species / HP ops ----
        private (string from, string to) Evolve(int index, bool force)
        {
            var roster = RunState.PlayerRoster;
            if (index < 0 || index >= roster.Count) return (null, null);
            var save = roster[index];
            var sp = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
            if (sp != null && sp.EvolvesInto != null && (force || sp.CanEvolveAt(save.Level)))
            {
                string from = sp.DisplayName;
                string to = sp.EvolvesInto.name;
                save.SpeciesName = to;
                return (from, to);
            }
            return (null, null);
        }

        private void SetHpAbsolute(int index, int hp)
        {
            var roster = RunState.PlayerRoster;
            if (index < 0 || index >= roster.Count) return;
            roster[index].CurrentHp = Mathf.Max(1, hp);
        }

        private void DamageAllToFraction(double frac)
        {
            var roster = RunState.PlayerRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                var save = roster[i];
                var sp = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
                if (sp == null) continue;
                var mon = new Pokemon(sp, save.Level, sp.MovesAtLevel(save.Level), save.AbilityIds);
                save.CurrentHp = Mathf.Max(1, (int)(mon.MaxHp * frac));
            }
        }

        private static string Name(int index)
        {
            var roster = RunState.PlayerRoster;
            if (index < 0 || index >= roster.Count) return "Your monster";
            var save = roster[index];
            var sp = Resources.Load<SpeciesData>("Species/" + save.SpeciesName);
            return sp != null ? sp.DisplayName : save.SpeciesName;
        }

        // ---- factories ----
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

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
            label = MakeText(rt, 16, TextAnchor.MiddleCenter, Color.white);
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