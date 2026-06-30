using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MonsterCatcher.Map.View
{
    // Runtime-built shop overlay: gold + the item catalog with prices and Buy buttons.
    public sealed class ShopView : MonoBehaviour
    {
        private Font _font;
        private GameObject _root;
        private RectTransform _list;
        private Text _goldText;
        private int _nodeId;
        private System.Action _onLeave;
        private System.Collections.Generic.List<string> _offer;

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
            _offer = ItemCatalog.RandomOffer(nodeId, 3);   // 3 random wares per shop
            _root.SetActive(true);
            Refresh();
        }

        private void Build()
        {
            var canvasGo = new GameObject("ShopCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo;
            var rt = (RectTransform)canvasGo.transform;

            var dim = MakePanel(rt, new Color(0.06f, 0.07f, 0.10f, 0.98f));
            Stretch(dim.rectTransform);

            var title = MakeText(rt, 28, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(title.rectTransform, 0.35f, 0.92f, 0.65f, 0.99f);
            title.text = "Shop";

            _goldText = MakeText(rt, 22, TextAnchor.MiddleLeft, new Color(0.96f, 0.86f, 0.4f));
            SetAnchors(_goldText.rectTransform, 0.06f, 0.92f, 0.34f, 0.99f);

            var leave = MakeButton(rt, new Color(0.45f, 0.3f, 0.25f), out var llbl);
            SetAnchors((RectTransform)leave.transform, 0.80f, 0.92f, 0.94f, 0.99f);
            llbl.text = "Leave";
            leave.onClick.AddListener(Leave);

            var listPanel = MakePanel(rt, new Color(1f, 1f, 1f, 0.04f));
            SetAnchors(listPanel.rectTransform, 0.10f, 0.08f, 0.90f, 0.88f);
            _list = listPanel.rectTransform;
        }

        private void Refresh()
        {
            _goldText.text = "Gold: " + RunState.Gold;
            for (int i = _list.childCount - 1; i >= 0; i--) Destroy(_list.GetChild(i).gameObject);

            for (int i = 0; i < _offer.Count; i++)
            {
                var info = ItemCatalog.ById(_offer[i]);
                if (info == null) continue;
                float top = 0.90f - i * 0.30f;

                var rowText = MakeText(_list, 17, TextAnchor.MiddleLeft, Color.white);
                SetAnchors(rowText.rectTransform, 0.03f, top - 0.26f, 0.71f, top);
                rowText.horizontalOverflow = HorizontalWrapMode.Wrap;
                rowText.text = info.Name + "  -  " + info.Description + "   (have " + RunState.ItemCount(info.Id) + ")";

                bool canBuy = RunState.Gold >= info.Price;
                var buy = MakeButton(_list, canBuy ? new Color(0.25f, 0.45f, 0.32f) : new Color(0.3f, 0.3f, 0.3f), out var blbl);
                SetAnchors((RectTransform)buy.transform, 0.73f, top - 0.22f, 0.97f, top - 0.04f);
                blbl.text = "Buy  " + info.Price + "g";
                buy.interactable = canBuy;
                string id = info.Id; int price = info.Price;
                buy.onClick.AddListener(() => { if (RunState.TrySpendGold(price)) { RunState.AddItem(id, 1); Refresh(); } });
            }
        }

        private void Leave()
        {
            RunState.VisitShop(_nodeId);
            _root.SetActive(false);
            _onLeave?.Invoke();
        }

        // ---- factories ----
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
