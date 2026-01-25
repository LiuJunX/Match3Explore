using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Unity.UI
{
    /// <summary>
    /// Bottom UI panel with speed control, pause, and auto-play.
    /// Layout: [Speed: 1.0x ====o====] [Pause] [Auto]
    /// </summary>
    public sealed class BottomPanel : MonoBehaviour
    {
        private const float PanelHeight = 60f;
        private const float MinSpeed = 0.1f;
        private const float MaxSpeed = 5.0f;
        private const float DefaultSpeed = 1.0f;
        private const int FontSize = 20;

        private RectTransform _rect;
        private Slider _speedSlider;
        private TextMeshProUGUI _speedLabel;
        private Button _pauseButton;
        private TextMeshProUGUI _pauseButtonText;
        private Button _autoPlayButton;
        private TextMeshProUGUI _autoPlayButtonText;

        private bool _isPaused;
        private bool _isAutoPlaying;

        /// <summary>
        /// Event fired when speed slider value changes.
        /// </summary>
        public event Action<float> OnSpeedChanged;

        /// <summary>
        /// Event fired when pause button is clicked.
        /// </summary>
        public event Action OnPauseToggled;

        /// <summary>
        /// Event fired when auto-play button is clicked.
        /// </summary>
        public event Action OnAutoPlayToggled;

        /// <summary>
        /// Initialize the bottom panel.
        /// </summary>
        public void Initialize()
        {
            _rect = gameObject.AddComponent<RectTransform>();
            UIFactory.SetAnchors(_rect, AnchorPreset.BottomStretch);
            _rect.sizeDelta = new Vector2(0, PanelHeight);
            _rect.anchoredPosition = new Vector2(0, 0);

            // Background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // Main layout
            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15f;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Speed label
            _speedLabel = UIFactory.CreateText(
                transform,
                "Speed: 1.0x",
                FontSize,
                Color.white,
                TextAlignmentOptions.MidlineRight,
                "SpeedLabel");
            var speedLabelLayout = _speedLabel.gameObject.AddComponent<LayoutElement>();
            speedLabelLayout.preferredWidth = 100;

            // Speed slider
            _speedSlider = UIFactory.CreateSlider(transform, MinSpeed, MaxSpeed, DefaultSpeed, "SpeedSlider");
            var sliderRect = _speedSlider.GetComponent<RectTransform>();
            var sliderLayout = _speedSlider.gameObject.AddComponent<LayoutElement>();
            sliderLayout.preferredWidth = 200;
            sliderLayout.preferredHeight = 30;

            _speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);

            // Spacer
            var spacer = new GameObject("Spacer");
            var spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.SetParent(transform, false);
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // Pause button
            _pauseButton = UIFactory.CreateButton(
                transform,
                "Pause",
                OnPauseButtonClicked,
                new Color(0.3f, 0.3f, 0.5f, 0.9f),
                Color.white,
                FontSize,
                "PauseButton");
            var pauseRect = _pauseButton.GetComponent<RectTransform>();
            var pauseLayout = _pauseButton.gameObject.AddComponent<LayoutElement>();
            pauseLayout.preferredWidth = 80;
            pauseLayout.preferredHeight = 40;
            _pauseButtonText = _pauseButton.GetComponentInChildren<TextMeshProUGUI>();

            // Auto-play button
            _autoPlayButton = UIFactory.CreateButton(
                transform,
                "Auto",
                OnAutoPlayButtonClicked,
                new Color(0.3f, 0.5f, 0.3f, 0.9f),
                Color.white,
                FontSize,
                "AutoPlayButton");
            var autoRect = _autoPlayButton.GetComponent<RectTransform>();
            var autoLayout = _autoPlayButton.gameObject.AddComponent<LayoutElement>();
            autoLayout.preferredWidth = 80;
            autoLayout.preferredHeight = 40;
            _autoPlayButtonText = _autoPlayButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnSpeedSliderChanged(float value)
        {
            // Round to 1 decimal place for display
            var roundedValue = Mathf.Round(value * 10f) / 10f;
            _speedLabel.text = $"Speed: {roundedValue:F1}x";
            OnSpeedChanged?.Invoke(roundedValue);
        }

        private void OnPauseButtonClicked()
        {
            OnPauseToggled?.Invoke();
        }

        private void OnAutoPlayButtonClicked()
        {
            OnAutoPlayToggled?.Invoke();
        }

        /// <summary>
        /// Set the paused state (updates button appearance).
        /// </summary>
        public void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;

            if (_pauseButtonText != null)
            {
                _pauseButtonText.text = isPaused ? "Play" : "Pause";
            }

            if (_pauseButton != null)
            {
                var colors = _pauseButton.colors;
                colors.normalColor = isPaused
                    ? new Color(0.5f, 0.3f, 0.3f, 0.9f)
                    : new Color(0.3f, 0.3f, 0.5f, 0.9f);
                _pauseButton.colors = colors;

                var image = _pauseButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = colors.normalColor;
                }
            }
        }

        /// <summary>
        /// Set the auto-play state (updates button appearance).
        /// </summary>
        public void SetAutoPlay(bool isAutoPlaying)
        {
            _isAutoPlaying = isAutoPlaying;

            if (_autoPlayButtonText != null)
            {
                _autoPlayButtonText.text = isAutoPlaying ? "Stop" : "Auto";
            }

            if (_autoPlayButton != null)
            {
                var colors = _autoPlayButton.colors;
                colors.normalColor = isAutoPlaying
                    ? new Color(0.5f, 0.5f, 0.2f, 0.9f)
                    : new Color(0.3f, 0.5f, 0.3f, 0.9f);
                _autoPlayButton.colors = colors;

                var image = _autoPlayButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = colors.normalColor;
                }
            }
        }

        /// <summary>
        /// Get the current speed value.
        /// </summary>
        public float GetSpeed()
        {
            return _speedSlider != null ? _speedSlider.value : DefaultSpeed;
        }

        /// <summary>
        /// Set the speed slider value.
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (_speedSlider != null)
            {
                _speedSlider.value = Mathf.Clamp(speed, MinSpeed, MaxSpeed);
            }
        }

        private void OnDestroy()
        {
            // Remove slider listener to prevent memory leaks
            if (_speedSlider != null)
            {
                _speedSlider.onValueChanged.RemoveListener(OnSpeedSliderChanged);
            }
        }
    }
}
