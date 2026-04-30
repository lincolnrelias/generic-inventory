using System;
using InventorySystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem.UI
{
    public sealed class InventoryTooltipController : IDisposable
    {
        private readonly InventoryService _service;
        private readonly InventoryPresenter _presenter;
        private readonly VisualElement _tooltipPanel;
        private readonly Label _titleLabel;
        private readonly Label _descriptionLabel;
        private readonly Func<IInventoryItem, string> _tooltipBuilder;

        public InventoryTooltipController(
            InventoryService service,
            InventoryPresenter presenter,
            VisualElement tooltipPanel,
            Label titleLabel,
            Label descriptionLabel,
            Func<IInventoryItem, string> tooltipBuilder)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _tooltipPanel = tooltipPanel ?? throw new ArgumentNullException(nameof(tooltipPanel));
            _titleLabel = titleLabel ?? throw new ArgumentNullException(nameof(titleLabel));
            _descriptionLabel = descriptionLabel ?? throw new ArgumentNullException(nameof(descriptionLabel));
            _tooltipBuilder = tooltipBuilder ?? throw new ArgumentNullException(nameof(tooltipBuilder));

            foreach (var slot in _presenter.SlotElements)
            {
                slot.RegisterCallback<PointerEnterEvent>(OnPointerEnterSlot);
                slot.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveSlot);
            }
        }

        public void Dispose()
        {
            foreach (var slot in _presenter.SlotElements)
            {
                slot.UnregisterCallback<PointerEnterEvent>(OnPointerEnterSlot);
                slot.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveSlot);
            }
        }

        private void OnPointerEnterSlot(PointerEnterEvent evt)
        {
            var slot = evt.currentTarget as VisualElement;
            if (slot == null || slot.userData is not int slotIndex)
            {
                return;
            }

            var item = _service.GetItem(slotIndex);
            if (item == null)
            {
                HideTooltip();
                return;
            }

            _titleLabel.text = item.DisplayName;
            _descriptionLabel.text = _tooltipBuilder(item);
            _tooltipPanel.style.left = evt.position.x + 16f;
            _tooltipPanel.style.top = evt.position.y + 16f;
            _tooltipPanel.RemoveFromClassList("hidden");
        }

        private void OnPointerLeaveSlot(PointerLeaveEvent _)
        {
            HideTooltip();
        }

        private void HideTooltip()
        {
            _tooltipPanel.AddToClassList("hidden");
        }
    }
}
