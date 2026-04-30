using System;
using System.Collections.Generic;
using InventorySystem.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem.UI
{
    public sealed class InventoryPresenter : IDisposable
    {
        private readonly InventoryService _service;
        private readonly VisualElement _gridRoot;
        private readonly Func<IInventoryItem, ItemViewModel> _viewModelBuilder;
        private readonly Func<IInventoryItem, Texture2D> _iconResolver;
        private readonly float _slotSpacing;
        private readonly int _columns;
        private readonly List<VisualElement> _slotElements = new();

        public IReadOnlyList<VisualElement> SlotElements => _slotElements;

        public InventoryPresenter(
            InventoryService service,
            VisualElement gridRoot,
            int columns,
            float slotSize,
            float slotSpacing,
            Func<IInventoryItem, ItemViewModel> viewModelBuilder,
            Func<IInventoryItem, Texture2D> iconResolver = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _gridRoot = gridRoot ?? throw new ArgumentNullException(nameof(gridRoot));
            _viewModelBuilder = viewModelBuilder ?? throw new ArgumentNullException(nameof(viewModelBuilder));
            _iconResolver = iconResolver;
            _slotSpacing = Mathf.Max(0f, slotSpacing);
            _columns = Mathf.Max(1, columns);

            _gridRoot.style.width = (slotSize * columns) + ((columns - 1) * slotSpacing);
            BuildSlots();
            Subscribe();
            RefreshAll();
        }

        public void Dispose()
        {
            _service.InventoryChanged -= HandleInventoryChanged;
        }

        public void RefreshAll()
        {
            for (var i = 0; i < _slotElements.Count; i++)
            {
                RefreshSlot(i);
            }
        }

        private void BuildSlots()
        {
            _slotElements.Clear();
            _gridRoot.Clear();

            for (var i = 0; i < _service.Grid.SlotCount; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("inventory-slot");
                slot.userData = i;
                var x = i % _columns;
                var y = i / _columns;
                slot.style.marginRight = x < _columns - 1 ? _slotSpacing : 0f;
                slot.style.marginBottom = y < _service.Grid.Rows - 1 ? _slotSpacing : 0f;
                _slotElements.Add(slot);
                _gridRoot.Add(slot);
            }
        }

        private void RefreshSlot(int slotIndex)
        {
            var slot = _slotElements[slotIndex];
            slot.Clear();

            var item = _service.GetItem(slotIndex);
            if (item == null)
            {
                return;
            }

            var vm = _viewModelBuilder(item);
            var icon = new VisualElement();
            icon.AddToClassList("inventory-item-icon");
            // Let the slot receive pointer/mouse events directly for drag start.
            icon.pickingMode = PickingMode.Ignore;
            var hasIconTexture = false;
            var resolvedTexture = _iconResolver?.Invoke(item);
            if (resolvedTexture != null)
            {
                icon.style.backgroundImage = new StyleBackground(resolvedTexture);
                hasIconTexture = true;
            }
            else if (!string.IsNullOrWhiteSpace(vm.IconPath))
            {
                var texture = Resources.Load<Texture2D>(vm.IconPath);
                if (texture == null)
                {
                    texture = Resources.Load<Texture2D>(item.IconId);
                }

                if (texture != null)
                {
                    icon.style.backgroundImage = new StyleBackground(texture);
                    hasIconTexture = true;
                }
            }

            icon.userData = slotIndex;
            slot.Add(icon);

            if (!hasIconTexture)
            {
                var fallbackLabel = new Label(GetFallbackToken(item));
                fallbackLabel.AddToClassList("inventory-item-fallback");
                fallbackLabel.pickingMode = PickingMode.Ignore;
                icon.Add(fallbackLabel);
            }
        }

        private static string GetFallbackToken(IInventoryItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.DisplayName))
            {
                return item.DisplayName.Substring(0, 1).ToUpperInvariant();
            }

            return item.Kind == InventoryItemKind.Equipment ? "E" : "M";
        }

        private void Subscribe()
        {
            _service.InventoryChanged += HandleInventoryChanged;
        }

        private void HandleInventoryChanged()
        {
            RefreshAll();
        }
    }
}
