using System;
using InventorySystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem.UI
{
    public sealed class InventoryTooltipController : IDisposable
    {
        private readonly InventoryService _service;
        private readonly InventoryPresenter _presenter;
        private readonly RectTransform _tooltipPanel;
        private readonly TextMeshProUGUI _titleLabel;
        private readonly TextMeshProUGUI _descriptionLabel;
        private readonly Func<IInventoryItem, string> _tooltipBuilder;
        private readonly Vector2 _tooltipOffset;
        private readonly Canvas _canvas;

        public InventoryTooltipController(
            InventoryService service,
            InventoryPresenter presenter,
            Canvas canvas,
            RectTransform tooltipPanel,
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI descriptionLabel,
            Func<IInventoryItem, string> tooltipBuilder,
            Vector2 tooltipOffset)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _tooltipPanel = tooltipPanel ?? throw new ArgumentNullException(nameof(tooltipPanel));
            _titleLabel = titleLabel ?? throw new ArgumentNullException(nameof(titleLabel));
            _descriptionLabel = descriptionLabel ?? throw new ArgumentNullException(nameof(descriptionLabel));
            _tooltipBuilder = tooltipBuilder ?? throw new ArgumentNullException(nameof(tooltipBuilder));
            _tooltipOffset = tooltipOffset;

            foreach (var slot in _presenter.SlotElements)
            {
                slot.PointerEntered += OnPointerEnterSlot;
                slot.PointerExited += OnPointerLeaveSlot;
            }
        }

        public void Dispose()
        {
            foreach (var slot in _presenter.SlotElements)
            {
                slot.PointerEntered -= OnPointerEnterSlot;
                slot.PointerExited -= OnPointerLeaveSlot;
            }
        }

        private void OnPointerEnterSlot(int slotIndex, PointerEventData evt)
        {
            var item = _service.GetItem(slotIndex);
            if (item == null)
            {
                HideTooltip();
                return;
            }

            _titleLabel.text = item.DisplayName;
            _descriptionLabel.text = _tooltipBuilder(item);
            var screenPoint = evt.position + _tooltipOffset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPoint,
                _canvas.worldCamera,
                out var localPoint);
            _tooltipPanel.anchoredPosition = localPoint;
            _tooltipPanel.gameObject.SetActive(true);
        }

        private void OnPointerLeaveSlot(int _, PointerEventData __)
        {
            HideTooltip();
        }

        private void HideTooltip()
        {
            _tooltipPanel.gameObject.SetActive(false);
        }
    }
}
