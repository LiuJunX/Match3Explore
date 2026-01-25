using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Unity.UI
{
    /// <summary>
    /// Modal panel displaying game result (Victory/Defeat).
    /// Layout:
    /// ┌─────────────────┐
    /// │    VICTORY!     │
    /// │   Score: 1250   │
    /// │   [Restart]     │
    /// └─────────────────┘
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        private const float PanelWidth = 400f;
        private const float PanelHeight = 250f;
        private const int TitleFontSize = 48;
        private const int ScoreFontSize = 32;
        private const int ButtonFontSize = 24;

        private RectTransform _rect;
        private Image _overlay;
        private RectTransform _modalRect;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _scoreText;
        private Button _restartButton;

        private bool _isVisible;

        /// <summary>
        /// Event fired when restart button is clicked.
        /// </summary>
        public event Action OnRestartClicked;

        /// <summary>
        /// Initialize the result panel.
        /// </summary>
        public void Initialize()
        {
            _rect = gameObject.AddComponent<RectTransform>();
            UIFactory.SetAnchors(_rect, AnchorPreset.Stretch);
            _rect.sizeDelta = Vector2.zero;
            _rect.anchoredPosition = Vector2.zero;

            // Semi-transparent overlay
            _overlay = gameObject.AddComponent<Image>();
            _overlay.color = new Color(0, 0, 0, 0.6f);
            _overlay.raycastTarget = true;

            // Modal container
            var modalPanel = UIFactory.CreatePanel(
                transform,
                new Color(0.15f, 0.15f, 0.2f, 0.95f),
                "ModalPanel");
            _modalRect = modalPanel.GetComponent<RectTransform>();
            UIFactory.SetAnchors(_modalRect, AnchorPreset.Center);
            _modalRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _modalRect.anchoredPosition = Vector2.zero;

            // Add rounded corners effect (subtle border)
            var outline = modalPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1, 1, 1, 0.3f);
            outline.effectDistance = new Vector2(2, -2);

            // Vertical layout for modal content
            var layout = modalPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(30, 30, 40, 30);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title text
            _titleText = UIFactory.CreateText(
                _modalRect,
                "VICTORY!",
                TitleFontSize,
                new Color(1f, 0.85f, 0.3f),
                TextAlignmentOptions.Center,
                "TitleText");
            _titleText.fontStyle = FontStyles.Bold;
            var titleLayout = _titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 60;

            // Score text
            _scoreText = UIFactory.CreateText(
                _modalRect,
                "Score: 0",
                ScoreFontSize,
                Color.white,
                TextAlignmentOptions.Center,
                "ScoreText");
            var scoreLayout = _scoreText.gameObject.AddComponent<LayoutElement>();
            scoreLayout.preferredHeight = 45;

            // Restart button
            _restartButton = UIFactory.CreateButton(
                _modalRect,
                "Restart",
                OnRestartButtonClicked,
                new Color(0.3f, 0.6f, 0.3f, 1f),
                Color.white,
                ButtonFontSize,
                "RestartButton");
            var restartLayout = _restartButton.gameObject.AddComponent<LayoutElement>();
            restartLayout.preferredWidth = 150;
            restartLayout.preferredHeight = 50;
        }

        private void OnRestartButtonClicked()
        {
            OnRestartClicked?.Invoke();
        }

        /// <summary>
        /// Show the result panel with victory/defeat state.
        /// </summary>
        public void Show(bool isVictory, int score)
        {
            gameObject.SetActive(true);
            _isVisible = true;

            if (isVictory)
            {
                _titleText.text = "VICTORY!";
                _titleText.color = new Color(1f, 0.85f, 0.3f); // Gold
            }
            else
            {
                _titleText.text = "DEFEAT";
                _titleText.color = new Color(0.8f, 0.3f, 0.3f); // Red
            }

            _scoreText.text = $"Score: {score:N0}";
        }

        /// <summary>
        /// Hide the result panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            _isVisible = false;
        }

        /// <summary>
        /// Whether the panel is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;
    }
}
