using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Unity.UI
{
    /// <summary>
    /// Top UI panel displaying objectives and moves remaining.
    /// Layout: [Obj1: 3/5] [Obj2: 2/3] [Obj3: 0/1]    Moves: 15    Score: 1250
    /// </summary>
    public sealed class TopPanel : MonoBehaviour
    {
        private const int MaxObjectives = 4;
        private const float PanelHeight = 60f;
        private const int FontSize = 24;
        private const int ScoreFontSize = 28;

        private RectTransform _rect;
        private HorizontalLayoutGroup _layout;
        private ObjectiveDisplay[] _objectiveDisplays;
        private TextMeshProUGUI _movesText;
        private TextMeshProUGUI _scoreText;

        private int _currentMoves;
        private int _currentScore;

        /// <summary>
        /// Initialize the top panel.
        /// </summary>
        public void Initialize()
        {
            _rect = gameObject.AddComponent<RectTransform>();
            UIFactory.SetAnchors(_rect, AnchorPreset.TopStretch);
            _rect.sizeDelta = new Vector2(0, PanelHeight);
            _rect.anchoredPosition = new Vector2(0, 0);

            // Background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // Main layout
            _layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            _layout.spacing = 20f;
            _layout.padding = new RectOffset(20, 20, 10, 10);
            _layout.childAlignment = TextAnchor.MiddleLeft;
            _layout.childControlWidth = false;
            _layout.childControlHeight = true;
            _layout.childForceExpandWidth = false;
            _layout.childForceExpandHeight = false;

            // Create objective displays
            _objectiveDisplays = new ObjectiveDisplay[MaxObjectives];
            for (int i = 0; i < MaxObjectives; i++)
            {
                _objectiveDisplays[i] = CreateObjectiveDisplay(i);
                _objectiveDisplays[i].Root.gameObject.SetActive(false);
            }

            // Spacer to push moves/score to right
            var spacer = new GameObject("Spacer");
            var spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.SetParent(transform, false);
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // Moves display
            _movesText = UIFactory.CreateText(
                transform,
                "Moves: --",
                FontSize,
                Color.white,
                TextAlignmentOptions.MidlineRight,
                "MovesText");
            var movesLayout = _movesText.gameObject.AddComponent<LayoutElement>();
            movesLayout.preferredWidth = 120;

            // Score display
            _scoreText = UIFactory.CreateText(
                transform,
                "Score: 0",
                ScoreFontSize,
                new Color(1f, 0.85f, 0.3f),
                TextAlignmentOptions.MidlineRight,
                "ScoreText");
            var scoreLayout = _scoreText.gameObject.AddComponent<LayoutElement>();
            scoreLayout.preferredWidth = 150;
        }

        private ObjectiveDisplay CreateObjectiveDisplay(int index)
        {
            var container = new GameObject($"Objective{index}");
            var rect = container.AddComponent<RectTransform>();
            rect.SetParent(transform, false);

            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.preferredWidth = 100;

            // Color indicator
            var indicator = UIFactory.CreateImage(rect, null, Color.white, "Indicator");
            var indicatorRect = indicator.GetComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(24, 24);
            var indicatorLayout = indicator.gameObject.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = 24;
            indicatorLayout.preferredHeight = 24;

            // Progress text
            var text = UIFactory.CreateText(
                rect,
                "0/0",
                FontSize - 2,
                Color.white,
                TextAlignmentOptions.MidlineLeft,
                "ProgressText");
            var textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 60;

            return new ObjectiveDisplay
            {
                Root = rect,
                Indicator = indicator,
                ProgressText = text
            };
        }

        /// <summary>
        /// Update objectives display.
        /// </summary>
        public void UpdateObjectives(ObjectiveProgress[] objectives)
        {
            if (objectives == null) return;

            for (int i = 0; i < MaxObjectives; i++)
            {
                if (i < objectives.Length)
                {
                    var obj = objectives[i];
                    var display = _objectiveDisplays[i];

                    display.Root.gameObject.SetActive(true);
                    display.Indicator.color = obj.Color;
                    display.ProgressText.text = $"{obj.Current}/{obj.Target}";

                    // Dim completed objectives
                    if (obj.IsCompleted)
                    {
                        display.ProgressText.color = new Color(0.5f, 1f, 0.5f);
                    }
                    else
                    {
                        display.ProgressText.color = Color.white;
                    }
                }
                else
                {
                    _objectiveDisplays[i].Root.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Update moves remaining display.
        /// </summary>
        public void UpdateMoves(int remaining)
        {
            _currentMoves = remaining;
            _movesText.text = $"Moves: {remaining}";

            // Warning color when low on moves
            if (remaining <= 3)
            {
                _movesText.color = new Color(1f, 0.3f, 0.3f);
            }
            else if (remaining <= 5)
            {
                _movesText.color = new Color(1f, 0.7f, 0.3f);
            }
            else
            {
                _movesText.color = Color.white;
            }
        }

        /// <summary>
        /// Update score display.
        /// </summary>
        public void UpdateScore(int score)
        {
            _currentScore = score;
            _scoreText.text = $"Score: {score:N0}";
        }

        private struct ObjectiveDisplay
        {
            public RectTransform Root;
            public Image Indicator;
            public TextMeshProUGUI ProgressText;
        }
    }
}
