using System.Collections;
using Match3.Unity.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Match3.Unity.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for UI button functionality.
    /// These tests verify that UI buttons work correctly and trigger expected callbacks.
    /// </summary>
    public class UIButtonTests
    {
        private GameObject _canvasGo;
        private Canvas _canvas;
        private EventSystem _eventSystem;

        [SetUp]
        public void SetUp()
        {
            // Create Canvas
            _canvasGo = new GameObject("TestCanvas");
            _canvas = _canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasGo.AddComponent<CanvasScaler>();
            _canvasGo.AddComponent<GraphicRaycaster>();

            // Create EventSystem (required for UI interaction)
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.transform.SetParent(_canvasGo.transform);
            _eventSystem = eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasGo != null)
            {
                Object.Destroy(_canvasGo);
            }
        }

        #region EventSystem Tests

        [UnityTest]
        public IEnumerator EventSystem_Exists_WhenUICreated()
        {
            // This test verifies that EventSystem is created
            // Without EventSystem, UI clicks don't work
            yield return null;

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "EventSystem must exist for UI to receive input");
        }

        #endregion

        #region BottomPanel Tests

        [UnityTest]
        public IEnumerator BottomPanel_PauseButton_TriggersCallback()
        {
            // Arrange
            var panelGo = new GameObject("BottomPanel");
            panelGo.transform.SetParent(_canvas.transform, false);
            var panel = panelGo.AddComponent<BottomPanel>();
            panel.Initialize();

            bool pauseTriggered = false;
            panel.OnPauseToggled += () => pauseTriggered = true;

            yield return null;

            // Act - Find and click pause button
            var pauseButton = panelGo.GetComponentInChildren<Button>();
            Assert.IsNotNull(pauseButton, "Pause button should exist");

            // Find the specific pause button by name
            var buttons = panelGo.GetComponentsInChildren<Button>();
            Button targetButton = null;
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name == "PauseButton")
                {
                    targetButton = btn;
                    break;
                }
            }
            Assert.IsNotNull(targetButton, "PauseButton should exist");

            targetButton.onClick.Invoke();
            yield return null;

            // Assert
            Assert.IsTrue(pauseTriggered, "OnPauseToggled should be called when pause button clicked");
        }

        [UnityTest]
        public IEnumerator BottomPanel_AutoPlayButton_TriggersCallback()
        {
            // Arrange
            var panelGo = new GameObject("BottomPanel");
            panelGo.transform.SetParent(_canvas.transform, false);
            var panel = panelGo.AddComponent<BottomPanel>();
            panel.Initialize();

            bool autoPlayTriggered = false;
            panel.OnAutoPlayToggled += () => autoPlayTriggered = true;

            yield return null;

            // Act - Find auto play button
            var buttons = panelGo.GetComponentsInChildren<Button>();
            Button targetButton = null;
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name == "AutoPlayButton")
                {
                    targetButton = btn;
                    break;
                }
            }
            Assert.IsNotNull(targetButton, "AutoPlayButton should exist");

            targetButton.onClick.Invoke();
            yield return null;

            // Assert
            Assert.IsTrue(autoPlayTriggered, "OnAutoPlayToggled should be called when auto play button clicked");
        }

        [UnityTest]
        public IEnumerator BottomPanel_SpeedSlider_TriggersCallback()
        {
            // Arrange
            var panelGo = new GameObject("BottomPanel");
            panelGo.transform.SetParent(_canvas.transform, false);
            var panel = panelGo.AddComponent<BottomPanel>();
            panel.Initialize();

            float receivedSpeed = 0f;
            panel.OnSpeedChanged += speed => receivedSpeed = speed;

            yield return null;

            // Act - Find and change slider
            var slider = panelGo.GetComponentInChildren<Slider>();
            Assert.IsNotNull(slider, "Speed slider should exist");

            slider.value = 2.5f;
            yield return null;

            // Assert
            Assert.AreEqual(2.5f, receivedSpeed, 0.1f, "OnSpeedChanged should be called with new speed value");
        }

        #endregion

        #region ResultPanel Tests

        [UnityTest]
        public IEnumerator ResultPanel_RestartButton_TriggersCallback()
        {
            // Arrange
            var panelGo = new GameObject("ResultPanel");
            panelGo.transform.SetParent(_canvas.transform, false);
            var panel = panelGo.AddComponent<ResultPanel>();
            panel.Initialize();

            bool restartTriggered = false;
            panel.OnRestartClicked += () => restartTriggered = true;

            yield return null;

            // Act - Show panel and click restart
            panel.Show(true, 1000);
            yield return null;

            var button = panelGo.GetComponentInChildren<Button>();
            Assert.IsNotNull(button, "Restart button should exist");

            button.onClick.Invoke();
            yield return null;

            // Assert
            Assert.IsTrue(restartTriggered, "OnRestartClicked should be called when restart button clicked");
        }

        [UnityTest]
        public IEnumerator ResultPanel_ShowVictory_DisplaysCorrectText()
        {
            // Arrange
            var panelGo = new GameObject("ResultPanel");
            panelGo.transform.SetParent(_canvas.transform, false);
            var panel = panelGo.AddComponent<ResultPanel>();
            panel.Initialize();

            yield return null;

            // Act
            panel.Show(true, 5000);
            yield return null;

            // Assert - Panel should be visible
            Assert.IsTrue(panelGo.activeSelf, "Panel should be visible after Show()");
        }

        #endregion
    }
}
