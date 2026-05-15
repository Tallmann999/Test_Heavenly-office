using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace HeavenOffice
{
    public sealed class HeavenOfficeViewController : MonoBehaviour
    {
        private HeavenOfficeSessionController controller;
        private readonly List<Button> stampButtons = new List<Button>();
        private readonly List<GameObject> runtimeEffects = new List<GameObject>();

        private RectTransform root;
        private Text scoreText;
        private Text progressText;
        private Text timerText;
        private Text mistakesText;
        private Text comboText;
        private Text soulNameText;
        private Text descriptionText;
        private Text goodActsText;
        private Text badActsText;
        private Text notesText;
        private Text ruleHintText;
        private Text feedbackText;
        private Text soulReactionText;
        private Button stampTargetButton;
        private RectTransform leftStampPanel;
        private RectTransform rightStampPanel;
        private Image documentImage;
        private Image selectedStampPreview;
        private string selectedStampId;
        private string activeDocumentId;
        private Font uiFont;
        private Color defaultDocumentColor;

        public void Initialize(HeavenOfficeSessionController sessionController)
        {
            controller = sessionController;
            EnsureEventSystem();
            BuildView();
            Subscribe();
        }

        private void Update()
        {
            controller?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClearRuntimeEffects();
        }

        private void Subscribe()
        {
            controller.SnapshotChanged += OnSnapshotChanged;
            controller.SoulPresented += OnSoulPresented;
            controller.DecisionResolved += OnDecisionResolved;
            controller.SessionFinished += OnSessionFinished;
        }

        private void Unsubscribe()
        {
            if (controller == null)
            {
                return;
            }

            controller.SnapshotChanged -= OnSnapshotChanged;
            controller.SoulPresented -= OnSoulPresented;
            controller.DecisionResolved -= OnDecisionResolved;
            controller.SessionFinished -= OnSessionFinished;
        }

        private void BuildView()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var canvasObject = new GameObject("Heaven Office Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            root = canvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var background = CreateImage("Background", root, new Color(0.08f, 0.09f, 0.11f));
            Stretch(background.rectTransform);

            var topPanel = CreatePanel("Top Panel", root, new Color(0.13f, 0.14f, 0.16f));
            Anchor(topPanel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -72f), Vector2.zero);
            var topLayout = topPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(18, 18, 10, 10);
            topLayout.spacing = 18;
            topLayout.childControlWidth = true;
            topLayout.childForceExpandWidth = true;
            scoreText = CreateHudText("Score", topPanel.rectTransform);
            progressText = CreateHudText("Progress", topPanel.rectTransform);
            timerText = CreateHudText("Timer", topPanel.rectTransform);
            mistakesText = CreateHudText("Mistakes", topPanel.rectTransform);
            comboText = CreateHudText("Combo", topPanel.rectTransform);

            leftStampPanel = CreateStampPanel("Left Stamps", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(18f, 88f), new Vector2(178f, -88f));
            rightStampPanel = CreateStampPanel("Right Stamps", root, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-178f, 88f), new Vector2(-18f, -88f));

            var center = CreatePanel("Work Area", root, new Color(0.17f, 0.18f, 0.18f));
            Anchor(center.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(196f, 88f), new Vector2(-196f, -88f));

            soulReactionText = CreateText("Soul Reaction", center.rectTransform, 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.9f, 0.93f, 0.94f));
            Anchor(soulReactionText.rectTransform, new Vector2(0.05f, 0.77f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);

            documentImage = CreatePanel("Soul Document", center.rectTransform, new Color(0.92f, 0.87f, 0.76f));
            defaultDocumentColor = documentImage.color;
            Anchor(documentImage.rectTransform, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.76f), Vector2.zero, Vector2.zero);

            var documentLayout = documentImage.gameObject.AddComponent<VerticalLayoutGroup>();
            documentLayout.padding = new RectOffset(22, 22, 18, 18);
            documentLayout.spacing = 7;
            documentLayout.childControlHeight = true;
            documentLayout.childForceExpandHeight = false;
            documentLayout.childControlWidth = true;
            documentLayout.childForceExpandWidth = true;

            soulNameText = CreateDocumentText("Soul Name", documentImage.rectTransform, 28, FontStyle.Bold);
            descriptionText = CreateDocumentText("Description", documentImage.rectTransform, 18, FontStyle.Normal);
            goodActsText = CreateDocumentText("Good Acts", documentImage.rectTransform, 17, FontStyle.Normal);
            badActsText = CreateDocumentText("Bad Acts", documentImage.rectTransform, 17, FontStyle.Normal);
            notesText = CreateDocumentText("Notes", documentImage.rectTransform, 17, FontStyle.Italic);

            stampTargetButton = CreateButton("Stamp Target", center.rectTransform, "ПОСТАВИТЬ ПЕЧАТЬ", new Color(0.28f, 0.29f, 0.31f), Color.white);
            Anchor(stampTargetButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.13f), Vector2.zero, Vector2.zero);
            stampTargetButton.onClick.AddListener(PlaceSelectedStamp);

            selectedStampPreview = CreateImage("Selected Stamp Preview", center.rectTransform, new Color(1f, 1f, 1f, 0f));
            Anchor(selectedStampPreview.rectTransform, new Vector2(0.43f, 0.78f), new Vector2(0.57f, 0.91f), Vector2.zero, Vector2.zero);

            var bottomPanel = CreatePanel("Bottom Panel", root, new Color(0.12f, 0.13f, 0.15f));
            Anchor(bottomPanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 78f));
            var bottomLayout = bottomPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            bottomLayout.padding = new RectOffset(18, 18, 8, 8);
            bottomLayout.spacing = 5;
            ruleHintText = CreateText("Rule Hint", bottomPanel.rectTransform, 18, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.9f));
            feedbackText = CreateText("Feedback", bottomPanel.rectTransform, 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.94f, 0.74f));
        }

        private void OnSoulPresented(Soul soul, HeavenOfficeSnapshot snapshot, IReadOnlyList<StampDefinition> stamps)
        {
            activeDocumentId = soul.Document.DocumentId;
            selectedStampId = string.Empty;
            ClearRuntimeEffects();
            documentImage.color = defaultDocumentColor;
            selectedStampPreview.color = new Color(1f, 1f, 1f, 0f);
            soulReactionText.text = "Следующая душа ожидает решения";
            soulNameText.text = soul.Document.SoulName;
            descriptionText.text = soul.Document.LifeDescription;
            goodActsText.text = "Добрые дела:\n" + FormatList(soul.Document.GoodActs);
            badActsText.text = "Дурные дела:\n" + FormatList(soul.Document.BadActs);
            notesText.text = "Пометки:\n" + FormatList(soul.Document.SpecialNotes);
            RebuildStampButtons(stamps);
            OnSnapshotChanged(snapshot);
        }

        private void OnSnapshotChanged(HeavenOfficeSnapshot snapshot)
        {
            scoreText.text = $"Счёт: {snapshot.Score}";
            progressText.text = $"Очередь: {snapshot.QueueIndex}/{snapshot.QueueCount}";
            timerText.text = $"Время: {snapshot.DocumentTimeRemaining:0.0}";
            mistakesText.text = $"Ошибки: {snapshot.MistakeCount}";
            comboText.text = $"Серия: {snapshot.Combo}";
            ruleHintText.text = snapshot.CurrentRuleHint;
            feedbackText.text = string.IsNullOrWhiteSpace(snapshot.LastDecisionFeedback) ? "Решение ещё не принято." : snapshot.LastDecisionFeedback;
            stampTargetButton.interactable = controller != null && controller.CanAcceptInput(activeDocumentId) && !string.IsNullOrEmpty(selectedStampId);
        }

        private void OnDecisionResolved(DecisionResult result)
        {
            stampTargetButton.interactable = false;
            documentImage.color = result.IsCorrect ? new Color(0.82f, 0.93f, 0.78f) : new Color(0.96f, 0.77f, 0.72f);
            soulReactionText.text = result.IsCorrect ? "Душа спокойно уходит по назначению" : "Душа замирает, архив гудит тревогой";
            CreateInkMark(result);
        }

        private void OnSessionFinished(HeavenOfficeSessionResult result)
        {
            ClearStampSelection();
            soulReactionText.text = result.CompletionReason == HeavenOfficeCompletionReason.QueueCompleted
                ? $"Смена закрыта. Итог: {result.FinalScore}"
                : $"Смена сорвана. Итог: {result.FinalScore}";
            feedbackText.text = $"Верно: {result.CorrectDecisionCount}, ошибок: {result.MistakeCount}, среднее время: {result.AverageReactionTime:0.0}";
        }

        private void RebuildStampButtons(IReadOnlyList<StampDefinition> stamps)
        {
            foreach (var button in stampButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            stampButtons.Clear();
            foreach (var stamp in stamps)
            {
                var parent = stamp.panelSide == StampPanelSide.Left ? leftStampPanel : rightStampPanel;
                var button = CreateButton($"Stamp {stamp.stampId}", parent, $"{stamp.symbol}\n{stamp.displayName}", stamp.color, Color.white);
                button.onClick.AddListener(() => SelectStamp(stamp));
                stampButtons.Add(button);
            }
        }

        private void SelectStamp(StampDefinition stamp)
        {
            if (controller == null || !controller.BeginStampInteraction(activeDocumentId))
            {
                return;
            }

            selectedStampId = stamp.stampId;
            selectedStampPreview.color = new Color(stamp.color.r, stamp.color.g, stamp.color.b, 0.7f);
            SetButtonSelectedStates();
            stampTargetButton.interactable = true;
        }

        private void PlaceSelectedStamp()
        {
            if (string.IsNullOrEmpty(selectedStampId) || controller == null)
            {
                return;
            }

            if (controller.PlaceStamp(activeDocumentId, selectedStampId))
            {
                ClearStampSelection();
            }
        }

        private void ClearStampSelection()
        {
            selectedStampId = string.Empty;
            selectedStampPreview.color = new Color(1f, 1f, 1f, 0f);
            SetButtonSelectedStates();
            if (stampTargetButton != null)
            {
                stampTargetButton.interactable = false;
            }
        }

        private void SetButtonSelectedStates()
        {
            foreach (var button in stampButtons)
            {
                var label = button.GetComponentInChildren<Text>();
                var image = button.GetComponent<Image>();
                var stamp = controller.GetAvailableStamps().FirstOrDefault(item => label != null && label.text.Contains(item.displayName));
                if (stamp != null && stamp.stampId == selectedStampId)
                {
                    image.color = Color.Lerp(stamp.color, Color.white, 0.25f);
                }
                else if (stamp != null)
                {
                    image.color = stamp.color;
                }
            }
        }

        private void CreateInkMark(DecisionResult result)
        {
            var ink = CreateText("Ink Mark", documentImage.rectTransform, 30, FontStyle.Bold, TextAnchor.MiddleCenter, result.IsCorrect ? new Color(0.1f, 0.35f, 0.75f, 0.8f) : new Color(0.65f, 0.05f, 0.04f, 0.8f));
            ink.text = string.IsNullOrEmpty(result.SelectedStampId) ? "TIME\nEXPIRED" : result.SelectedStampId.ToUpperInvariant();
            Anchor(ink.rectTransform, new Vector2(0.56f, 0.10f), new Vector2(0.95f, 0.32f), Vector2.zero, Vector2.zero);
            ink.transform.SetAsLastSibling();
            runtimeEffects.Add(ink.gameObject);
        }

        private void ClearRuntimeEffects()
        {
            foreach (var effect in runtimeEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }

            runtimeEffects.Clear();
        }

        private static string FormatList(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "- нет";
            }

            return string.Join("\n", values.Select(value => "- " + value));
        }

        private RectTransform CreateStampPanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var panel = CreatePanel(name, parent, new Color(0.11f, 0.12f, 0.14f));
            Anchor(panel.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 16, 16);
            layout.spacing = 14;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return panel.rectTransform;
        }

        private Text CreateHudText(string name, RectTransform parent)
        {
            var text = CreateText(name, parent, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            return text;
        }

        private Text CreateDocumentText(string name, RectTransform parent, int size, FontStyle style)
        {
            var text = CreateText(name, parent, size, style, TextAnchor.UpperLeft, new Color(0.12f, 0.10f, 0.08f));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Text CreateText(string name, RectTransform parent, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private Image CreatePanel(string name, RectTransform parent, Color color)
        {
            return CreateImage(name, parent, color);
        }

        private Image CreateImage(string name, RectTransform parent, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Button CreateButton(string name, RectTransform parent, string label, Color color, Color textColor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            var button = obj.GetComponent<Button>();
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(136f, 72f);
            var layoutElement = obj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 72f;
            layoutElement.minHeight = 64f;
            var text = CreateText("Label", rect, 19, FontStyle.Bold, TextAnchor.MiddleCenter, textColor);
            text.text = label;
            Stretch(text.rectTransform);
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect)
        {
            Anchor(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystem);
        }
    }
}
