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
        private const float MarginX = 0.10f, MarginY = 0.08f;
        private static readonly Vector2 ContainerSize = new Vector2(1140f, 640f);

        private Font _font;
        private RectTransform _container;
        private Text _title;
        private readonly Dictionary<int, Button> _nodeButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, Image> _nodeImages = new Dictionary<int, Image>();

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            if (RunState.Map == null) RunState.NewRun(System.Environment.TickCount);
            BuildUi();
            RefreshNodes();

            if (RunState.RunWon) ShowOverlay("Run cleared! You beat the boss.");
            else if (RunState.RunLost) ShowOverlay("Game Over.");
        }

        // ---- positions -----------------------------------------------------

        private Vector2 LocalPos(MapNode n)
        {
            float xNorm = Mathf.Lerp(MarginX, 1f - MarginX, n.X);
            float yNorm = Mathf.Lerp(MarginY, 1f - MarginY, (float)n.Row / (RunState.Map.RowCount - 1));
            return new Vector2((xNorm - 0.5f) * ContainerSize.x, (yNorm - 0.5f) * ContainerSize.y);
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
            SetAnchors(_title.rectTransform, 0.04f, 0.93f, 0.96f, 0.99f);
            _title.text = "Run Map  -  reach the BOSS at the top";

            var container = MakePanel(canvasRt, new Color(0f, 0f, 0f, 0f));
            var crt = container.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = ContainerSize;
            crt.anchoredPosition = new Vector2(0f, -10f);
            _container = crt;

            // edges first (behind nodes)
            foreach (var n in RunState.Map.Nodes)
                foreach (var t in n.Next)
                    MakeEdge(LocalPos(n), LocalPos(RunState.Map.Get(t)));

            // nodes
            foreach (var n in RunState.Map.Nodes) MakeNode(n);
        }

        private void MakeEdge(Vector2 a, Vector2 b)
        {
            var rt = NewRect("Edge", _container);
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
            var rt = NewRect("Node" + n.Id, _container);
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
            label.text = n.Type == NodeType.Start ? "Start" : n.Type == NodeType.Boss ? "BOSS" : n.Type == NodeType.Heal ? "+" : "";

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
            switch (status)
            {
                case NodeStatus.Current: return new Color(0.95f, 0.82f, 0.25f);
                case NodeStatus.Available: return new Color(0.3f, 0.7f, 0.35f);
                case NodeStatus.Cleared: return new Color(0.32f, 0.45f, 0.62f);
                default: return new Color(0.22f, 0.24f, 0.28f);
            }
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
            RunState.Select(id);
            SceneManager.LoadScene("Battle");
        }

        private void ShowOverlay(string message)
        {
            var canvasRt = (RectTransform)_container.parent;
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
