using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Input;
using Match3.Unity.Bridge;
using Match3.Unity.Views;
using UnityEngine;

namespace Match3.Unity.Controllers
{
    /// <summary>
    /// Handles player input by delegating to Core's StandardInputSystem.
    /// Only converts Unity screen coordinates to Core format.
    /// </summary>
    public sealed class InputController : MonoBehaviour
    {
        private Match3Bridge _bridge;
        private BoardView _boardView;
        private Camera _mainCamera;

        private StandardInputSystem _inputSystem;

        /// <summary>
        /// Initialize the input controller.
        /// </summary>
        public void Initialize(Match3Bridge bridge, BoardView boardView)
        {
            _bridge = bridge;
            _boardView = boardView;
            _mainCamera = Camera.main;

            // Create Core's input system
            _inputSystem = new StandardInputSystem();
            _inputSystem.Configure(_bridge.CellSize);

            // Subscribe to Core's input events
            _inputSystem.TapDetected += OnTapDetected;
            _inputSystem.SwipeDetected += OnSwipeDetected;
        }

        private void OnDestroy()
        {
            if (_inputSystem != null)
            {
                _inputSystem.TapDetected -= OnTapDetected;
                _inputSystem.SwipeDetected -= OnSwipeDetected;
            }
        }

        private void Update()
        {
            if (_bridge == null || !_bridge.IsInitialized) return;
            if (_mainCamera == null) _mainCamera = Camera.main;

            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var (gridPos, screenPos) = GetPositions();
                if (gridPos.IsValid)
                {
                    // Pass to Core's input system
                    // Note: Core expects Y+ = down, Unity screen Y+ = up
                    // So we flip screenY for Core
                    _inputSystem.OnPointerDown(
                        gridPos.X, gridPos.Y,
                        screenPos.x, Screen.height - screenPos.y);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                var screenPos = Input.mousePosition;
                // Flip Y for Core
                _inputSystem.OnPointerUp(screenPos.x, Screen.height - screenPos.y);
            }
        }

        private void OnTapDetected(Position pos)
        {
            if (!_bridge.IsIdle()) return;

            _bridge.HandleTap(pos);
        }

        private void OnSwipeDetected(Position from, Direction direction)
        {
            if (!_bridge.IsIdle()) return;

            var to = _inputSystem.GetSwipeTarget(from, direction);
            if (_bridge.GetTileIdAt(to) >= 0)
            {
                _bridge.ApplyMove(from, to);
            }
        }

        private (Position gridPos, Vector3 screenPos) GetPositions()
        {
            var screenPos = Input.mousePosition;
            screenPos.z = -_mainCamera.transform.position.z;
            var worldPos = _mainCamera.ScreenToWorldPoint(screenPos);

            var gridPos = CoordinateConverter.WorldToGrid(
                worldPos, _bridge.CellSize, _bridge.BoardOrigin,
                _bridge.Width, _bridge.Height);

            return (gridPos, screenPos);
        }
    }
}
