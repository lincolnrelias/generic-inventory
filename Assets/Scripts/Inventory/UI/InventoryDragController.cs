using System;
using InventorySystem.Core;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InventorySystem.UI
{
    public sealed class InventoryDragController : IDisposable
    {
        private readonly InventoryService _service;
        private readonly InventoryPresenter _presenter;
        private readonly VisualElement _root;
        private readonly VisualElement _dragIcon;
        private readonly float _holdThreshold;

        private bool _isPressing;
        private bool _isDragging;
        private float _pressStartTime;
        private int _sourceSlotIndex = -1;
        private VisualElement _sourceIcon;

        public InventoryDragController(
            InventoryService service,
            InventoryPresenter presenter,
            VisualElement root,
            VisualElement dragIcon,
            float holdThreshold)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _dragIcon = dragIcon ?? throw new ArgumentNullException(nameof(dragIcon));
            _holdThreshold = Mathf.Max(0f, holdThreshold);
        }

        public void Dispose()
        {
            EndDrag();
        }

        public void Tick()
        {
            if (!TryReadPointer(out var pressedThisFrame, out var releasedThisFrame, out var pointerScreenPos))
            {
                return;
            }

            var pointerPanelPos = ToPanelPosition(pointerScreenPos);

            if (pressedThisFrame && !_isPressing)
            {
                BeginPress(pointerPanelPos);
            }

            if (_isPressing && !_isDragging && Time.unscaledTime - _pressStartTime >= _holdThreshold)
            {
                StartDrag(pointerPanelPos);
            }

            if (_isDragging)
            {
                MoveDragIcon(pointerPanelPos);
            }

            if (releasedThisFrame && _isPressing)
            {
                if (_isDragging)
                {
                    CompleteDrop(pointerPanelPos);
                }

                EndDrag();
            }
        }

        private void BeginPress(Vector2 pointerPanelPos)
        {
            if (!TryGetSlotAt(pointerPanelPos, out var slotIndex))
            {
                return;
            }

            if (_service.GetItem(slotIndex) == null)
            {
                return;
            }

            _isPressing = true;
            _pressStartTime = Time.unscaledTime;
            _sourceSlotIndex = slotIndex;
            _sourceIcon = _presenter.SlotElements[slotIndex].Q(className: "inventory-item-icon");
        }

        private void StartDrag(Vector2 pointerPanelPos)
        {
            if (_isDragging || _sourceSlotIndex < 0)
            {
                return;
            }

            _isDragging = true;
            _dragIcon.RemoveFromClassList("hidden");
            MoveDragIcon(pointerPanelPos);
            if (_sourceIcon != null)
            {
                _sourceIcon.AddToClassList("dragging");
                _dragIcon.style.backgroundImage = _sourceIcon.style.backgroundImage;
            }
        }

        private void MoveDragIcon(Vector2 pointerPanelPos)
        {
            var container = _dragIcon.parent;
            var local = container != null ? container.WorldToLocal(pointerPanelPos) : pointerPanelPos;
            _dragIcon.style.left = local.x - 26f;
            _dragIcon.style.top = local.y - 26f;
        }

        private void CompleteDrop(Vector2 pointerPanelPos)
        {
            if (_sourceSlotIndex < 0)
            {
                return;
            }

            if (!TryGetSlotAt(pointerPanelPos, out var targetSlotIndex) || targetSlotIndex == _sourceSlotIndex)
            {
                return;
            }

            var targetItem = _service.GetItem(targetSlotIndex);
            if (targetItem == null)
            {
                _service.TryMoveItem(_sourceSlotIndex, targetSlotIndex);
            }
            else
            {
                _service.TrySwapItems(_sourceSlotIndex, targetSlotIndex);
            }
        }

        private void EndDrag()
        {
            if (_sourceIcon != null)
            {
                _sourceIcon.RemoveFromClassList("dragging");
            }

            _dragIcon.AddToClassList("hidden");
            _dragIcon.style.backgroundImage = StyleKeyword.None;
            _sourceIcon = null;
            _sourceSlotIndex = -1;
            _isPressing = false;
            _isDragging = false;
        }

        private bool TryReadPointer(out bool pressedThisFrame, out bool releasedThisFrame, out Vector2 pointerScreenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                var left = Mouse.current.leftButton;
                pressedThisFrame = left.wasPressedThisFrame;
                releasedThisFrame = left.wasReleasedThisFrame;
                pointerScreenPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif
            pressedThisFrame = Input.GetMouseButtonDown(0);
            releasedThisFrame = Input.GetMouseButtonUp(0);
            pointerScreenPosition = Input.mousePosition;
            return true;
        }

        private Vector2 ToPanelPosition(Vector2 screenPos)
        {
            if (_root.panel == null)
            {
                return Vector2.zero;
            }

            var flipped = new Vector2(screenPos.x, Screen.height - screenPos.y);
            return RuntimePanelUtils.ScreenToPanel(_root.panel, flipped);
        }

        private bool TryGetSlotAt(Vector2 pointerPanelPos, out int slotIndex)
        {
            slotIndex = -1;
            for (var i = 0; i < _presenter.SlotElements.Count; i++)
            {
                if (_presenter.SlotElements[i].worldBound.Contains(pointerPanelPos))
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}
