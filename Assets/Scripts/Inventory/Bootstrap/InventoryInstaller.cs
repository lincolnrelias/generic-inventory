using System;
using System.Collections.Generic;
using InventorySystem.Core;
using InventorySystem.Data;
using InventorySystem.UI;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InventorySystem.Bootstrap
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class InventoryInstaller : MonoBehaviour
    {
        [SerializeField] private InventoryConfig config;
        [SerializeField] private ItemDefinitionBase[] startupItems;

        private InventoryService _service;
        private InventoryPresenter _presenter;
        private InventoryDragController _dragController;
        private InventoryTooltipController _tooltipController;
        private InventoryInteractionPanelController _interactionPanelController;
        private int _runtimeItemCounter;
        private readonly Dictionary<string, Texture2D> _iconByItemId = new();

        public InventoryService Service => _service;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("InventoryConfig is required.");
                enabled = false;
                return;
            }

            var document = GetComponent<UIDocument>();
            var root = document.rootVisualElement;

            _service = new InventoryService(new InventoryGrid(config.Columns, config.Rows));
            CacheStartupItemIcons();
            _presenter = new InventoryPresenter(
                _service,
                root.Q<VisualElement>("inventory-grid"),
                config.Columns,
                config.SlotSize,
                BuildViewModel,
                ResolveIconTexture);

            _dragController = new InventoryDragController(
                _service,
                _presenter,
                root,
                root.Q<VisualElement>("drag-icon"),
                config.HoldThresholdSeconds);

            _tooltipController = new InventoryTooltipController(
                _service,
                _presenter,
                root.Q<VisualElement>("tooltip-panel"),
                root.Q<Label>("tooltip-title"),
                root.Q<Label>("tooltip-description"),
                BuildTooltip);

            _interactionPanelController = new InventoryInteractionPanelController(
                _service,
                root,
                CreateDebugMaterialItem,
                CreateDebugEquipmentItem);

            AddStartupItems();
        }

        private void Update()
        {
            _dragController?.Tick();

            if (IsHotkeyPressedAddMaterial())
            {
                TryAddMaterial();
            }

            if (IsHotkeyPressedAddEquipment())
            {
                TryAddEquipment();
            }

            if (IsHotkeyPressedRemoveLast())
            {
                RemoveLastItem();
            }
        }

        private void OnDestroy()
        {
            _interactionPanelController?.Dispose();
            _tooltipController?.Dispose();
            _dragController?.Dispose();
            _presenter?.Dispose();
        }

        private ItemViewModel BuildViewModel(IInventoryItem item)
        {
            return new ItemViewModel(item.DisplayName, item.Description, item.IconId);
        }

        private string BuildTooltip(IInventoryItem item)
        {
            return item.Kind switch
            {
                InventoryItemKind.Equipment => $"Type: Equipment\n{item.Description}",
                InventoryItemKind.Material => $"Type: Material\n{item.Description}",
                _ => item.Description
            };
        }

        private void AddStartupItems()
        {
            if (startupItems == null)
            {
                return;
            }

            for (var i = 0; i < startupItems.Length; i++)
            {
                var definition = startupItems[i];
                if (definition == null)
                {
                    continue;
                }

                var item = definition.CreateRuntimeItem();
                _service.TryAddItem(item, out _);
            }
        }

        private void CacheStartupItemIcons()
        {
            _iconByItemId.Clear();
            if (startupItems == null)
            {
                return;
            }

            for (var i = 0; i < startupItems.Length; i++)
            {
                var definition = startupItems[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                if (definition.Icon != null)
                {
                    _iconByItemId[definition.Id] = definition.Icon;
                }
            }
        }

        private Texture2D ResolveIconTexture(IInventoryItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                return null;
            }

            return _iconByItemId.TryGetValue(item.Id, out var texture) ? texture : null;
        }

        public bool TryAddMaterial()
        {
            return _service.TryAddItem(CreateDebugMaterialItem(), out _);
        }

        public bool TryAddEquipment()
        {
            return _service.TryAddItem(CreateDebugEquipmentItem(), out _);
        }

        public bool RemoveAt(int slotIndex)
        {
            return _service.RemoveItem(slotIndex);
        }

        public bool RemoveLastItem()
        {
            for (var i = _service.Grid.SlotCount - 1; i >= 0; i--)
            {
                if (_service.GetItem(i) != null)
                {
                    return _service.RemoveItem(i);
                }
            }

            return false;
        }

        public void ClearAllItems()
        {
            for (var i = _service.Grid.SlotCount - 1; i >= 0; i--)
            {
                _service.RemoveItem(i);
            }
        }

        private IInventoryItem CreateDebugMaterialItem()
        {
            _runtimeItemCounter++;
            return new MaterialItem(
                $"mat_runtime_{_runtimeItemCounter}",
                $"Material {_runtimeItemCounter}",
                "Generated material item",
                string.Empty,
                "runtime");
        }

        private IInventoryItem CreateDebugEquipmentItem()
        {
            _runtimeItemCounter++;
            return new EquipmentItem(
                $"eq_runtime_{_runtimeItemCounter}",
                $"Equipment {_runtimeItemCounter}",
                "Generated equipment item",
                string.Empty,
                UnityEngine.Random.Range(5, 31));
        }

        private static bool IsHotkeyPressedAddMaterial()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F1);
#endif
        }

        private static bool IsHotkeyPressedAddEquipment()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F2);
#endif
        }

        private static bool IsHotkeyPressedRemoveLast()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F3);
#endif
        }
    }

    internal sealed class InventoryInteractionPanelController : IDisposable
    {
        private readonly InventoryService _service;
        private readonly Func<IInventoryItem> _createMaterial;
        private readonly Func<IInventoryItem> _createEquipment;
        private readonly VisualElement _panelRoot;

        private readonly Button _addMaterialButton;
        private readonly Button _addEquipmentButton;
        private readonly Button _removeLastButton;
        private readonly Button _fillRandomButton;
        private readonly Button _clearAllButton;

        public InventoryInteractionPanelController(
            InventoryService service,
            VisualElement root,
            Func<IInventoryItem> createMaterial,
            Func<IInventoryItem> createEquipment)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _createMaterial = createMaterial ?? throw new ArgumentNullException(nameof(createMaterial));
            _createEquipment = createEquipment ?? throw new ArgumentNullException(nameof(createEquipment));
            _panelRoot = root.Q<VisualElement>("interaction-panel");

            _addMaterialButton = root.Q<Button>("tool-add-material");
            _addEquipmentButton = root.Q<Button>("tool-add-equipment");
            _removeLastButton = root.Q<Button>("tool-remove-last");
            _fillRandomButton = root.Q<Button>("tool-fill-random");
            _clearAllButton = root.Q<Button>("tool-clear-all");

            if (_panelRoot != null)
            {
                _panelRoot.pickingMode = PickingMode.Position;
                _panelRoot.BringToFront();
            }

            HookButton(_addMaterialButton, AddMaterial, "tool-add-material");
            HookButton(_addEquipmentButton, AddEquipment, "tool-add-equipment");
            HookButton(_removeLastButton, RemoveLast, "tool-remove-last");
            HookButton(_fillRandomButton, FillRandom, "tool-fill-random");
            HookButton(_clearAllButton, ClearAll, "tool-clear-all");
        }

        public void Dispose()
        {
            if (_addMaterialButton != null) _addMaterialButton.clicked -= AddMaterial;
            if (_addEquipmentButton != null) _addEquipmentButton.clicked -= AddEquipment;
            if (_removeLastButton != null) _removeLastButton.clicked -= RemoveLast;
            if (_fillRandomButton != null) _fillRandomButton.clicked -= FillRandom;
            if (_clearAllButton != null) _clearAllButton.clicked -= ClearAll;
        }

        private void AddMaterial()
        {
            if (!_service.TryAddItem(_createMaterial(), out _))
            {
                Debug.Log("Inventory tools: add material failed (inventory full or blocked by rules).");
            }
        }

        private void AddEquipment()
        {
            if (!_service.TryAddItem(_createEquipment(), out _))
            {
                Debug.Log("Inventory tools: add equipment failed (inventory full or blocked by rules).");
            }
        }

        private void RemoveLast()
        {
            for (var i = _service.Grid.SlotCount - 1; i >= 0; i--)
            {
                if (_service.GetItem(i) != null)
                {
                    _service.RemoveItem(i);
                    return;
                }
            }

            Debug.Log("Inventory tools: remove last failed (inventory is already empty).");
        }

        private void FillRandom()
        {
            for (var i = 0; i < _service.Grid.SlotCount; i++)
            {
                if (_service.GetItem(i) != null)
                {
                    continue;
                }

                var item = UnityEngine.Random.value > 0.5f ? _createMaterial() : _createEquipment();
                _service.TryAddItem(item, out _);
            }
        }

        private void ClearAll()
        {
            var removedCount = 0;
            for (var i = _service.Grid.SlotCount - 1; i >= 0; i--)
            {
                if (_service.GetItem(i) != null)
                {
                    _service.RemoveItem(i);
                    removedCount++;
                }
            }

            if (removedCount == 0)
            {
                Debug.Log("Inventory tools: clear all had no effect (inventory already empty).");
                return;
            }

        }

        private static void HookButton(Button button, Action handler, string buttonName)
        {
            if (button == null)
            {
                Debug.LogWarning($"Inventory tools: button '{buttonName}' was not found in the UI document.");
                return;
            }

            button.clicked += handler;
        }
    }
}
