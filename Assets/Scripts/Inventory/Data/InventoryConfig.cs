using UnityEngine;

namespace InventorySystem.Data
{
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "Inventory/Inventory Config")]
    public sealed class InventoryConfig : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField] private int columns = 6;
        [SerializeField] private int rows = 5;
        [SerializeField] private float slotSize = 64f;

        [Header("Drag")]
        [SerializeField] private float holdThresholdSeconds = 0.12f;

        public int Columns => Mathf.Max(1, columns);
        public int Rows => Mathf.Max(1, rows);
        public float SlotSize => Mathf.Max(1f, slotSize);
        public float HoldThresholdSeconds => Mathf.Max(0f, holdThresholdSeconds);
    }
}
