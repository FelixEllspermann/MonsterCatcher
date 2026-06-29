using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MonsterCatcher.Map;

namespace MonsterCatcher.Battle.View
{
    /// <summary>
    /// Runtime-built Pokemon-style battle UI. Builds its own Canvas, HP bars, a typewriter
    /// message box and a FIGHT/SWITCH/ITEMS command menu, then drives a <see cref="BattleController"/>.
    /// Attach next to a BattleController on a single GameObject in an otherwise empty scene.
    /// </summary>
    [RequireComponent(typeof(BattleController))]
    public sealed class BattleHud : MonoBehaviour
    {
        private enum MenuState { Main, Attack, Switch, Items }

        private struct Step
        {
            public string Text;
            public System.Action Apply;
        }

        private const float CharDelay = 0.015f;
        private const float LinePause = 0.55f;

        private static readonly Color ColMain = new Color(0.16f, 0.26f, 0.45f, 1f);
        private static readonly Color ColMove = new Color(0.18f, 0.30f, 0.40f, 1f);
        private static readonly Color ColSwitch = new Color(0.30f, 0.25f, 0.12f, 1f);
        private static readonly Color ColBack = new Color(0.32f, 0.18f, 0.18f, 1f);

        private BattleController _controller;
        private Font _font;

        private Text _enemyName, _enemyHpText, _playerName, _playerHpText, _message;
        private Image _enemyHpFill, _playerHpFill;
        private Image _enemySprite, _playerSprite;

        private Pokemon _shownEnemy, _shownPlayer;
        private int _dispEnemyHp, _dispPlayerHp;
        private readonly Dictionary<Pokemon, int> _preHp = new Dictionary<Pokemon, int>();

        private GameObject _mainMenu, _attackMenu, _switchMenu, _itemsMenu;
        private readonly List<Button> _moveButtons = new List<Button>();
        private readonly List<Text> _moveLabels = new List<Text>();
        private readonly List<Button> _partyButtons = new List<Button>();
        private readonly List<Text> _partyLabels = new List<Text>();
        private Button _switchBack;

        private MenuState _menuState;
        private bool _isPlaying;
        private Coroutine _playRoutine;
        private Button _continueButton;
        private GameObject _evoMenu;
        private int _evoIndex;

        // ---- Lifecycle -----------------------------------------------------

        private void Start()
        {
            _controller = GetComponent<BattleController>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            EnsureEventSystem();
            _controller.StartBattle();
            BuildUi();
            _controller.TurnResolved += OnTurnResolved;

            var eng = _controller.Engine;
            _shownPlayer = eng.Player.Active; _dispPlayerHp = _shownPlayer.CurrentHp;
            _shownEnemy = eng.Enemy.Active; _dispEnemyHp = _shownEnemy.CurrentHp;
            UpdatePanel(BattleSide.Player);
            UpdatePanel(BattleSide.Enemy);

            HideMenus();
            StartPlay(
                new List<Step> { new Step { Text = "A wild " + _shownEnemy.Species.DisplayName + " appeared!" } },
                () => ShowMenu(MenuState.Main));
        }

        private void OnDestroy()
        {
            if (_controller != null) _controller.TurnResolved -= OnTurnResolved;
        }

        // ---- Input ---------------------------------------------------------

        private void OnMoveClicked(int i)
        {
            if (_isPlaying) return;
            var eng = _controller.Engine;
            if (eng.IsOver || eng.AwaitingForcedSwitch(BattleSide.Player)) return;
            if (i < 0 || i >= eng.Player.Active.Moves.Count) return;
            if (!eng.Player.Active.Moves[i].HasPp) return;
            SnapshotHp();
            _controller.PlayerUseMove(i);
        }

        private void OnPartyClicked(int i)
        {
            if (_isPlaying) return;
            var eng = _controller.Engine;
            if (eng.IsOver || !eng.Player.CanSwitchTo(i)) return;
            bool forced = eng.AwaitingForcedSwitch(BattleSide.Player);
            SnapshotHp();
            if (forced) _controller.ResolvePlayerForcedSwitch(i);
            else _controller.PlayerSwitch(i);
        }

        private void SnapshotHp()
        {
            _preHp.Clear();
            var eng = _controller.Engine;
            foreach (var m in eng.Player.Members) _preHp[m] = m.CurrentHp;
            foreach (var m in eng.Enemy.Members) _preHp[m] = m.CurrentHp;
        }

        // ---- Turn playback -------------------------------------------------

        private void OnTurnResolved(IReadOnlyList<BattleEvent> events)
        {
            var steps = new List<Step>(events.Count);
            foreach (var e in events)
            {
                var ev = e;
                steps.Add(new Step { Text = Describe(ev), Apply = () => ApplyVisual(ev) });
            }
            StartPlay(steps, PostTurn);
        }

        private void StartPlay(List<Step> steps, System.Action onDone)
        {
            if (_playRoutine != null) StopCoroutine(_playRoutine);
            _playRoutine = StartCoroutine(PlaySteps(steps, onDone));
        }

        private IEnumerator PlaySteps(List<Step> steps, System.Action onDone)
        {
            _isPlaying = true;
            HideMenus();
            foreach (var s in steps)
            {
                s.Apply?.Invoke();
                yield return TypeMessage(s.Text);
            }
            _isPlaying = false;
            onDone?.Invoke();
        }

        private IEnumerator TypeMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            _message.text = "";
            for (int i = 0; i < text.Length; i++)
            {
                _message.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(CharDelay);
            }
            yield return new WaitForSeconds(LinePause);
        }

        private void PostTurn()
        {
            var eng = _controller.Engine;
            if (eng.IsOver)
            {
                HideMenus();
                if (RunState.InRun && _controller.PendingEvolutions.Count > 0)
                {
                    _evoIndex = 0;
                    ShowEvolutionPrompt();
                }
                else
                {
                    _message.text = ResultText(eng.Result);
                    if (RunState.InRun && _continueButton != null)
                        _continueButton.transform.parent.gameObject.SetActive(true);
                }
                return;
            }
            if (eng.AwaitingForcedSwitch(BattleSide.Player)) { ShowMenu(MenuState.Switch); return; }
            ShowMenu(MenuState.Main);
        }

        private void ShowEvolutionPrompt()
        {
            var pending = _controller.PendingEvolutions;
            if (_evoIndex >= pending.Count)
            {
                if (_evoMenu != null) _evoMenu.SetActive(false);
                _message.text = ResultText(_controller.Engine.Result);
                if (_continueButton != null) _continueButton.transform.parent.gameObject.SetActive(true);
                return;
            }
            var offer = pending[_evoIndex];
            _message.text = offer.FromName + " is evolving into " + offer.ToName + "!";
            if (_continueButton != null) _continueButton.transform.parent.gameObject.SetActive(false);
            if (_evoMenu != null) _evoMenu.SetActive(true);
        }

        private void OnEvolveYes()
        {
            var pending = _controller.PendingEvolutions;
            if (_evoIndex < pending.Count) _controller.EvolveRosterMonster(pending[_evoIndex].RosterIndex);
            _evoIndex++;
            ShowEvolutionPrompt();
        }

        private void OnEvolveNo()
        {
            _evoIndex++;
            ShowEvolutionPrompt();
        }

        // ---- Visual state from events -------------------------------------

        private void ApplyVisual(BattleEvent e)
        {
            switch (e)
            {
                case DamageEvent d: AdjustHp(d.Target, -d.Amount); break;
                case StatusDamageEvent sd: AdjustHp(sd.Target, -sd.Amount); break;
                case FaintedEvent f: SetHp(f.Target, 0); break;
                case SwitchedInEvent si: SwitchPanel(si.Side, si.Pokemon); break;
            }
        }

        private BattleSide SideOf(Pokemon p)
        {
            foreach (var m in _controller.Engine.Player.Members)
                if (ReferenceEquals(m, p)) return BattleSide.Player;
            return BattleSide.Enemy;
        }

        private void SwitchPanel(BattleSide side, Pokemon p)
        {
            int hp = _preHp.TryGetValue(p, out var v) ? v : p.CurrentHp;
            if (side == BattleSide.Player) { _shownPlayer = p; _dispPlayerHp = hp; }
            else { _shownEnemy = p; _dispEnemyHp = hp; }
            UpdatePanel(side);
        }

        private void AdjustHp(Pokemon target, int delta)
        {
            var side = SideOf(target);
            if (side == BattleSide.Player)
            {
                if (!ReferenceEquals(_shownPlayer, target)) SwitchPanel(side, target);
                _dispPlayerHp = Mathf.Clamp(_dispPlayerHp + delta, 0, target.MaxHp);
            }
            else
            {
                if (!ReferenceEquals(_shownEnemy, target)) SwitchPanel(side, target);
                _dispEnemyHp = Mathf.Clamp(_dispEnemyHp + delta, 0, target.MaxHp);
            }
            UpdatePanel(side);
        }

        private void SetHp(Pokemon target, int value)
        {
            var side = SideOf(target);
            if (side == BattleSide.Player) _dispPlayerHp = value;
            else _dispEnemyHp = value;
            UpdatePanel(side);
        }

        private void UpdatePanel(BattleSide side)
        {
            if (side == BattleSide.Player)
            {
                var p = _shownPlayer;
                _playerName.text = p.Species.DisplayName + "  Lv." + p.Level + "  (You)" + StatusTag(p);
                _playerHpText.text = "HP " + _dispPlayerHp + "/" + p.MaxHp;
                SetBar(_playerHpFill, (float)_dispPlayerHp / p.MaxHp);
                if (_playerSprite != null)
                {
                    _playerSprite.sprite = p.Species.BackSprite;
                    _playerSprite.enabled = p.Species.BackSprite != null;
                }
            }
            else
            {
                var e = _shownEnemy;
                _enemyName.text = e.Species.DisplayName + "  Lv." + e.Level + "  (Foe)" + StatusTag(e);
                _enemyHpText.text = "HP " + _dispEnemyHp + "/" + e.MaxHp;
                SetBar(_enemyHpFill, (float)_dispEnemyHp / e.MaxHp);
                if (_enemySprite != null)
                {
                    _enemySprite.sprite = e.Species.FrontSprite;
                    _enemySprite.enabled = e.Species.FrontSprite != null;
                }
            }
        }

        private static void SetBar(Image fill, float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fill.color = HpColor(ratio);
        }

        // ---- Menus ---------------------------------------------------------

        private void HideMenus()
        {
            _mainMenu.SetActive(false);
            _attackMenu.SetActive(false);
            _switchMenu.SetActive(false);
            _itemsMenu.SetActive(false);
            if (_continueButton != null) _continueButton.transform.parent.gameObject.SetActive(false);
            if (_evoMenu != null) _evoMenu.SetActive(false);
        }

        private void ShowMenu(MenuState s)
        {
            _menuState = s;
            if (_continueButton != null) _continueButton.transform.parent.gameObject.SetActive(false);
            if (_evoMenu != null) _evoMenu.SetActive(false);
            _mainMenu.SetActive(s == MenuState.Main);
            _attackMenu.SetActive(s == MenuState.Attack);
            _switchMenu.SetActive(s == MenuState.Switch);
            _itemsMenu.SetActive(s == MenuState.Items);

            var eng = _controller.Engine;
            bool forced = eng.AwaitingForcedSwitch(BattleSide.Player);
            if (_switchBack != null) _switchBack.gameObject.SetActive(!forced);

            if (s == MenuState.Attack) RefreshAttackMenu();
            if (s == MenuState.Switch) RefreshSwitchMenu();

            var p = eng.Player.Active;
            switch (s)
            {
                case MenuState.Main: _message.text = "What will " + p.Species.DisplayName + " do?"; break;
                case MenuState.Attack: _message.text = "Choose a move."; break;
                case MenuState.Switch: _message.text = forced ? "Choose your next Pokemon!" : "Switch to which Pokemon?"; break;
                case MenuState.Items: _message.text = "You have no items yet."; break;
            }
        }

        private void RefreshAttackMenu()
        {
            var p = _controller.Engine.Player.Active;
            for (int i = 0; i < _moveButtons.Count; i++)
            {
                if (i < p.Moves.Count)
                {
                    var slot = p.Moves[i];
                    _moveButtons[i].gameObject.SetActive(true);
                    _moveLabels[i].text = slot.Move.DisplayName + "\n" + slot.Move.Type + "  PP " + slot.CurrentPp + "/" + slot.MaxPp;
                    _moveButtons[i].interactable = slot.HasPp;
                }
                else
                {
                    _moveButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void RefreshSwitchMenu()
        {
            var party = _controller.Engine.Player;
            for (int i = 0; i < _partyButtons.Count; i++)
            {
                var m = party.Members[i];
                _partyLabels[i].text = m.Species.DisplayName + "\n" + m.CurrentHp + "/" + m.MaxHp + (m.IsFainted ? "  (KO)" : "");
                _partyButtons[i].interactable = party.CanSwitchTo(i);
            }
        }

        // ---- Text helpers --------------------------------------------------

        private static string StatusTag(Pokemon p) =>
            p.Status == StatusCondition.None ? "" : "  [" + p.Status + "]";

        private static Color HpColor(float ratio)
        {
            if (ratio > 0.5f) return new Color(0.25f, 0.8f, 0.3f);
            if (ratio > 0.2f) return new Color(0.95f, 0.8f, 0.2f);
            return new Color(0.9f, 0.25f, 0.2f);
        }

        private static string ResultText(BattleResult r)
        {
            switch (r)
            {
                case BattleResult.PlayerWon: return "You won the battle!";
                case BattleResult.EnemyWon: return "You lost the battle...";
                case BattleResult.Draw: return "The battle ended in a draw.";
                default: return "";
            }
        }

        private string Describe(BattleEvent e)
        {
            switch (e)
            {
                case MoveUsedEvent m: return m.User.Species.DisplayName + " used " + m.Move.DisplayName + "!";
                case ChargingEvent ce: return ce.User.Species.DisplayName + " is charging up!";
                case RecoilEvent re: return re.User.Species.DisplayName + " is hit by recoil!";
                case DrainEvent de: return de.User.Species.DisplayName + " drained HP!";
                case MissedEvent m: return m.User.Species.DisplayName + "'s attack missed!";
                case DamageEvent d:
                    return d.Target.Species.DisplayName + " took " + d.Amount + " damage"
                           + (d.WasCritical ? " (a critical hit!)" : "") + "." + EffNote(d.Effectiveness);
                case StatusInflictedEvent s: return s.Target.Species.DisplayName + " was afflicted by " + s.Status + "!";
                case StatusDamageEvent sd: return sd.Target.Species.DisplayName + " is hurt by " + sd.Status + "!";
                case StatChangedEvent sc:
                    return sc.Target.Species.DisplayName + "'s " + sc.Stat + (sc.DeltaStages > 0 ? " rose!" : " fell!");
                case ActionPreventedEvent ap:
                    return ap.User.Species.DisplayName + (ap.Reason == StatusCondition.Sleep ? " is fast asleep." : " is paralyzed! It can't move!");
                case FaintedEvent f: return f.Target.Species.DisplayName + " fainted!";
                case SwitchedInEvent si: return (si.Side == BattleSide.Player ? "Go! " : "Foe sent out ") + si.Pokemon.Species.DisplayName + "!";
                case BattleEndedEvent be: return ResultText(be.Result);
                default: return "";
            }
        }

        private static string EffNote(double eff)
        {
            if (eff <= 0.0) return " It had no effect...";
            if (eff > 1.0) return " It's super effective!";
            if (eff < 1.0) return " It's not very effective...";
            return "";
        }

        // ---- UI construction ----------------------------------------------

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("BattleCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRt = (RectTransform)canvasGo.transform;

            var bg = MakePanel(canvasRt, new Color(0.12f, 0.14f, 0.20f, 1f));
            Stretch(bg.rectTransform);

            // Enemy info (top-left)
            var enemyPanel = MakePanel(canvasRt, new Color(0f, 0f, 0f, 0.35f));
            SetAnchors(enemyPanel.rectTransform, 0.04f, 0.72f, 0.46f, 0.92f);
            BuildInfoPanel(enemyPanel.rectTransform, out _enemyName, out _enemyHpText, out _enemyHpFill);

            // Player info (mid-right)
            var playerPanel = MakePanel(canvasRt, new Color(0f, 0f, 0f, 0.35f));
            SetAnchors(playerPanel.rectTransform, 0.54f, 0.47f, 0.96f, 0.67f);
            BuildInfoPanel(playerPanel.rectTransform, out _playerName, out _playerHpText, out _playerHpFill);

            // Monster sprites (enemy front upper-right, player back lower-left)
            _enemySprite = MakePanel(canvasRt, Color.white);
            SetAnchors(_enemySprite.rectTransform, 0.56f, 0.66f, 0.80f, 0.92f);
            _enemySprite.preserveAspect = true;
            _enemySprite.raycastTarget = false;
            _enemySprite.enabled = false;

            _playerSprite = MakePanel(canvasRt, Color.white);
            SetAnchors(_playerSprite.rectTransform, 0.12f, 0.34f, 0.36f, 0.62f);
            _playerSprite.preserveAspect = true;
            _playerSprite.raycastTarget = false;
            _playerSprite.enabled = false;

            // Message box (bottom)
            var msgPanel = MakePanel(canvasRt, new Color(0f, 0f, 0f, 0.6f));
            SetAnchors(msgPanel.rectTransform, 0.04f, 0.04f, 0.96f, 0.17f);
            _message = MakeText(msgPanel.rectTransform, 22, TextAnchor.MiddleLeft, Color.white);
            SetAnchors(_message.rectTransform, 0.02f, 0f, 0.98f, 1f);
            _message.verticalOverflow = VerticalWrapMode.Truncate;

            // Command area (above the message box) — four overlapping menu rows
            _mainMenu = MakeRow(canvasRt);
            MakeButton(_mainMenu.transform, ColMain, out var aLbl); aLbl.text = "Attack";
            _mainMenu.transform.GetChild(_mainMenu.transform.childCount - 1).GetComponent<Button>().onClick.AddListener(() => { if (!_isPlaying) ShowMenu(MenuState.Attack); });
            MakeButton(_mainMenu.transform, ColMain, out var sLbl); sLbl.text = "Switch";
            _mainMenu.transform.GetChild(_mainMenu.transform.childCount - 1).GetComponent<Button>().onClick.AddListener(() => { if (!_isPlaying) ShowMenu(MenuState.Switch); });
            MakeButton(_mainMenu.transform, ColMain, out var iLbl); iLbl.text = "Items";
            _mainMenu.transform.GetChild(_mainMenu.transform.childCount - 1).GetComponent<Button>().onClick.AddListener(() => { if (!_isPlaying) ShowMenu(MenuState.Items); });

            // Attack menu: 2x2 move grid (left) + Back column (right), scales cleanly to 4 moves.
            var atkContainer = MakePanel(canvasRt, new Color(0f, 0f, 0f, 0f));
            SetAnchors(atkContainer.rectTransform, 0.04f, 0.19f, 0.96f, 0.34f);
            _attackMenu = atkContainer.gameObject;

            var moveGrid = MakePanel(atkContainer.transform, new Color(0f, 0f, 0f, 0f));
            SetAnchors(moveGrid.rectTransform, 0f, 0f, 0.80f, 1f);
            var gridCol = moveGrid.gameObject.AddComponent<VerticalLayoutGroup>();
            gridCol.spacing = 8;
            gridCol.childControlWidth = true;
            gridCol.childControlHeight = true;
            gridCol.childForceExpandWidth = true;
            gridCol.childForceExpandHeight = true;
            for (int r = 0; r < 2; r++)
            {
                var row = MakePanel(moveGrid.transform, new Color(0f, 0f, 0f, 0f));
                var gridRow = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                gridRow.spacing = 8;
                gridRow.childControlWidth = true;
                gridRow.childControlHeight = true;
                gridRow.childForceExpandWidth = true;
                gridRow.childForceExpandHeight = true;
                for (int col = 0; col < 2; col++)
                {
                    int idx = r * 2 + col;
                    var btn = MakeButton(row.transform, ColMove, out var lbl);
                    btn.onClick.AddListener(() => OnMoveClicked(idx));
                    _moveButtons.Add(btn);
                    _moveLabels.Add(lbl);
                }
            }
            var atkBack = MakeButton(atkContainer.transform, ColBack, out var atkBackLbl);
            SetAnchors((RectTransform)atkBack.transform, 0.82f, 0.12f, 1f, 0.88f);
            atkBackLbl.text = "Back";
            atkBack.onClick.AddListener(() => { if (!_isPlaying) ShowMenu(MenuState.Main); });

            _switchMenu = MakeRow(canvasRt);
            int partyCount = _controller.Engine.Player.Members.Count;
            for (int i = 0; i < partyCount; i++)
            {
                int idx = i;
                var btn = MakeButton(_switchMenu.transform, ColSwitch, out var lbl);
                btn.onClick.AddListener(() => OnPartyClicked(idx));
                _partyButtons.Add(btn);
                _partyLabels.Add(lbl);
            }
            _switchBack = MakeButton(_switchMenu.transform, ColBack, out var swBackLbl);
            swBackLbl.text = "Back";
            _switchBack.onClick.AddListener(() => { if (!_isPlaying) ShowMenu(MenuState.Main); });

            _itemsMenu = MakeRow(canvasRt);
            var noItems = MakeButton(_itemsMenu.transform, new Color(0.25f, 0.25f, 0.25f, 1f), out var noItemsLbl);
            noItemsLbl.text = "(no items yet)";
            noItems.interactable = false;
            var itBack = MakeButton(_itemsMenu.transform, ColBack, out var itBackLbl);
            itBackLbl.text = "Back";
            itBack.onClick.AddListener(() => { if (!_isPlaying) ShowMenu(MenuState.Main); });

            var contRow = MakeRow(canvasRt);
            _continueButton = MakeButton(contRow.transform, new Color(0.2f, 0.45f, 0.3f, 1f), out var contLbl);
            contLbl.text = "Continue";
            _continueButton.onClick.AddListener(() => SceneManager.LoadScene("Map"));
            contRow.SetActive(false);

            _evoMenu = MakeRow(canvasRt);
            var evoYes = MakeButton(_evoMenu.transform, new Color(0.2f, 0.5f, 0.3f, 1f), out var evoYesLbl);
            evoYesLbl.text = "Evolve";
            evoYes.onClick.AddListener(OnEvolveYes);
            var evoNo = MakeButton(_evoMenu.transform, ColBack, out var evoNoLbl);
            evoNoLbl.text = "Not now";
            evoNo.onClick.AddListener(OnEvolveNo);
            _evoMenu.SetActive(false);
        }

        private void BuildInfoPanel(RectTransform panel, out Text name, out Text hpText, out Image hpFill)
        {
            name = MakeText(panel, 22, TextAnchor.MiddleLeft, Color.white);
            SetAnchors(name.rectTransform, 0.04f, 0.60f, 0.98f, 0.98f);

            hpText = MakeText(panel, 16, TextAnchor.MiddleLeft, Color.white);
            SetAnchors(hpText.rectTransform, 0.04f, 0.34f, 0.98f, 0.58f);

            var barBg = MakePanel(panel, new Color(0.08f, 0.08f, 0.08f, 1f));
            SetAnchors(barBg.rectTransform, 0.04f, 0.10f, 0.96f, 0.30f);
            hpFill = MakePanel(barBg.rectTransform, new Color(0.25f, 0.8f, 0.3f, 1f));
            SetAnchors(hpFill.rectTransform, 0f, 0f, 1f, 1f);
        }

        private GameObject MakeRow(RectTransform parent)
        {
            var img = MakePanel(parent, new Color(0f, 0f, 0f, 0f));
            SetAnchors(img.rectTransform, 0.04f, 0.19f, 0.96f, 0.34f);
            var hlg = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            return img.gameObject;
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
            t.font = _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
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
