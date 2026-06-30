using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MonsterCatcher.Map.View
{
    public sealed class MapController : MonoBehaviour
    {
        private const float RowGap = 130f, VMargin = 80f, HMargin = 70f;

        private static Vector2 ContentSize =>
            new Vector2(1100f, (RunState.Map.RowCount - 1) * RowGap + 2f * VMargin);

        private Font _font;
        private RectTransform _content;
        private ScrollRect _scroll;
        private Text _title;
        private readonly Dictionary<int, Button> _nodeButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, Image> _nodeImages = new Dictionary<int, Image>();
        private MonsterView _monsterView;
        private ShopView _shopView;
        private EventView _eventView;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            if (RunState.Map == null) RunState.NewRun(System.Environment.TickCount);
            else if (RunState.RunWon) RunState.NextTier(System.Environment.TickCount);
            BuildUi();
            RefreshNodes();

            if (RunState.RunLost) ShowOverlay("Game Over - you reached tier " + RunState.Tier + ".");
        }

        // ---- positions -----------------------------------------------------

        private Vector2 LocalPos(MapNode n)
        {
            Vector2 size = ContentSize;
            float halfW = size.x * 0.5f, halfH = size.y * 0.5f;
            // Start and Boss nodes stay centered on their lane (little/no x jitter).
            bool anchored = n.Type == NodeType.Start || n.Type == NodeType.Boss;
            float xJit = anchored ? 0f : Jitter(n.Id, 1, 38f);
            float x = Mathf.Lerp(-halfW + HMargin, halfW - HMargin, n.X) + xJit;
            float y = -halfH + VMargin + n.Row * RowGap + Jitter(n.Id, 2, 18f);
            return new Vector2(x, y);
        }

        // Deterministic, stable per (id, salt) offset in [-amp, amp]. Pure hash, no Random.
        private static float Jitter(int id, int salt, float amp)
        {
            unchecked
            {
                int h = (int)2166136261;
                h = (h ^ id) * 16777619;
                h = (h ^ salt) * 16777619;
                h ^= h >> 13;
                h *= unchecked((int)0x5bd1e995);
                h ^= h >> 15;
                // map low bits to [0,1)
                float t = (h & 0x7fffffff) / 2147483648f;
                return (t * 2f - 1f) * amp;
            }
        }

        // ---- build ---------------------------------------------------------

        private void BuildUi()
        {
            var canvasGo = new GameObject("MapCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRt = (RectTransform)canvasGo.transform;

            var bg = MakePanel(canvasRt, new Color(0.10f, 0.12f, 0.16f, 1f));
            Stretch(bg.rectTransform);

            _title = MakeText(canvasRt, 26, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(_title.rectTransform, 0.18f, 0.93f, 0.82f, 0.99f);
            _title.text = "Tier " + RunState.Tier + "  -  reach the BOSS";

            var monstersBtn = MakeButton(canvasRt, new Color(0.25f, 0.42f, 0.55f), out var mlbl);
            SetAnchors((RectTransform)monstersBtn.transform, 0.02f, 0.93f, 0.16f, 0.99f);
            mlbl.text = "Monsters";
            monstersBtn.onClick.AddListener(OpenMonsterView);

            BuildScroll(canvasRt);

            // edges first (behind nodes)
            foreach (var n in RunState.Map.Nodes)
                foreach (var t in n.Next)
                    MakeEdge(LocalPos(n), LocalPos(RunState.Map.Get(t)));

            // nodes
            foreach (var n in RunState.Map.Nodes) MakeNode(n);
        }

        private void BuildScroll(RectTransform canvasRt)
        {
            // ScrollRoot fills the area under the title.
            var scrollRoot = NewRect("ScrollRoot", canvasRt);
            SetAnchors(scrollRoot, 0.02f, 0.02f, 0.98f, 0.90f);
            _scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 30f;

            // Viewport (stretched) with a near-invisible image (to catch drags) + mask.
            var viewport = NewRect("Viewport", scrollRoot);
            Stretch(viewport);
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();

            // Content holds edges + nodes, centered.
            var content = NewRect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = ContentSize;
            _content = content;

            _scroll.viewport = viewport;
            _scroll.content = content;

            // Vertical scrollbar pinned to the right edge of ScrollRoot.
            var sbRt = NewRect("Scrollbar", scrollRoot);
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.sizeDelta = new Vector2(14f, 0f);
            sbRt.anchoredPosition = Vector2.zero;
            var sbImg = sbRt.gameObject.AddComponent<Image>();
            sbImg.color = new Color(0f, 0f, 0f, 0.35f);
            var scrollbar = sbRt.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var handleRt = NewRect("Handle", sbRt);
            Stretch(handleRt);
            var handleImg = handleRt.gameObject.AddComponent<Image>();
            handleImg.color = new Color(0.6f, 0.62f, 0.7f, 0.9f);
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect = handleRt;

            _scroll.verticalScrollbar = scrollbar;
        }

        private void MakeEdge(Vector2 a, Vector2 b)
        {
            var rt = NewRect("Edge", _content);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.18f);
            img.raycastTarget = false;
            Vector2 dir = b - a;
            float len = dir.magnitude;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(len, 4f);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        private void MakeNode(MapNode n)
        {
            var rt = NewRect("Node" + n.Id, _content);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = n.Type == NodeType.Boss ? new Vector2(120f, 50f) : new Vector2(54f, 54f);
            rt.anchoredPosition = LocalPos(n);

            var img = rt.gameObject.AddComponent<Image>();
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            int id = n.Id;
            btn.onClick.AddListener(() => OnNodeClicked(id));

            var label = MakeText(rt, n.Type == NodeType.Boss ? 18 : 14, TextAnchor.MiddleCenter, Color.white);
            Stretch(label.rectTransform);
            label.text =
                n.Type == NodeType.Start ? "Start" :
                n.Type == NodeType.Boss ? "BOSS" :
                n.Type == NodeType.Heal ? "+" :
                n.Type == NodeType.Shop ? "$" :
                n.Type == NodeType.Event ? "?" : "";

            _nodeButtons[n.Id] = btn;
            _nodeImages[n.Id] = img;
        }

        // ---- state ---------------------------------------------------------

        private void RefreshNodes()
        {
            foreach (var n in RunState.Map.Nodes)
            {
                var status = RunState.StatusOf(n.Id);
                _nodeImages[n.Id].color = ColorFor(n.Type, status);
                _nodeButtons[n.Id].interactable = status == NodeStatus.Available;
            }

            if (_scroll != null)
            {
                int rows = Mathf.Max(1, RunState.Map.RowCount - 1);
                _scroll.verticalNormalizedPosition =
                    Mathf.Clamp01((float)RunState.Map.Get(RunState.CurrentNodeId).Row / rows);
            }
        }

        private static Color ColorFor(NodeType type, NodeStatus status)
        {
            if (type == NodeType.Boss)
            {
                if (status == NodeStatus.Available) return new Color(0.85f, 0.25f, 0.25f);
                if (status == NodeStatus.Cleared || status == NodeStatus.Current) return new Color(0.55f, 0.2f, 0.5f);
                return new Color(0.35f, 0.15f, 0.15f);
            }
            if (type == NodeType.Heal)
            {
                if (status == NodeStatus.Available) return new Color(0.95f, 0.45f, 0.7f);
                if (status == NodeStatus.Cleared || status == NodeStatus.Current) return new Color(0.6f, 0.35f, 0.5f);
                return new Color(0.42f, 0.28f, 0.35f);
            }
            if (type == NodeType.Shop)
            {
                if (status == NodeStatus.Available) return new Color(0.95f, 0.82f, 0.3f);
                if (status == NodeStatus.Cleared || status == NodeStatus.Current) return new Color(0.6f, 0.55f, 0.3f);
                return new Color(0.4f, 0.38f, 0.25f);
            }
            if (type == NodeType.Event)
            {
                if (status == NodeStatus.Available) return new Color(0.65f, 0.45f, 0.95f);
                if (status == NodeStatus.Cleared || status == NodeStatus.Current) return new Color(0.45f, 0.35f, 0.6f);
                return new Color(0.32f, 0.28f, 0.4f);
            }
            switch (status)
            {
                case NodeStatus.Current: return new Color(0.95f, 0.82f, 0.25f);
                case NodeStatus.Available: return new Color(0.3f, 0.7f, 0.35f);
                case NodeStatus.Cleared: return new Color(0.32f, 0.45f, 0.62f);
                default: return new Color(0.22f, 0.24f, 0.28f);
            }
        }

        private void OpenMonsterView()
        {
            if (_monsterView == null) _monsterView = new GameObject("MonsterView").AddComponent<MonsterView>();
            _monsterView.Toggle();
        }

        private void OnNodeClicked(int id)
        {
            if (!RunState.CanSelect(id)) return;
            if (RunState.Map.Get(id).Type == NodeType.Heal)
            {
                RunState.VisitHeal(id);
                if (_title != null) _title.text = "Party fully healed!";
                RefreshNodes();
                return;
            }
            if (RunState.Map.Get(id).Type == NodeType.Shop)
            {
                if (_shopView == null) _shopView = new GameObject("ShopView").AddComponent<ShopView>();
                _shopView.Open(id, () => { if (_title != null) _title.text = "Left the shop."; RefreshNodes(); });
                return;
            }
            if (RunState.Map.Get(id).Type == NodeType.Event)
            {
                if (_eventView == null) _eventView = new GameObject("EventView").AddComponent<EventView>();
                _eventView.Open(id, () => { if (_title != null) _title.text = "Event resolved."; RefreshNodes(); });
                return;
            }
            RunState.Select(id);
            SceneManager.LoadScene("Battle");
        }

        private void ShowOverlay(string message)
        {
            var canvasRt = (RectTransform)_scroll.transform.parent;
            var panel = MakePanel(canvasRt, new Color(0f, 0f, 0f, 0.75f));
            Stretch(panel.rectTransform);

            var text = MakeText(panel.transform, 34, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(text.rectTransform, 0.1f, 0.55f, 0.9f, 0.75f);
            text.text = message;

            var btn = MakeButton(panel.transform, new Color(0.2f, 0.45f, 0.3f, 1f), out var lbl);
            SetAnchors((RectTransform)btn.transform, 0.38f, 0.35f, 0.62f, 0.46f);
            lbl.text = "New Run";
            btn.onClick.AddListener(() =>
            {
                RunState.NewRun(System.Environment.TickCount);
                SceneManager.LoadScene("Map");
            });
        }

        // ---- factories -----------------------------------------------------

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
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
            label = MakeText(rt, 20, TextAnchor.MiddleCenter, Color.white);
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