using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem.UI.Themes
{
    [CreateAssetMenu(fileName = "InventoryTheme", menuName = "Inventory/Themes/Inventory Theme")]
    public sealed class InventoryThemeDefinition : ScriptableObject
    {
        [SerializeField] private InventoryThemeType themeType = InventoryThemeType.PixelArt;
        [SerializeField] private StyleSheet[] styleSheets;
        [Header("Frame Visual")]
        [SerializeField] private Texture2D frameTexture;
        [SerializeField] private ScaleMode frameScaleMode = ScaleMode.StretchToFill;
        [SerializeField] private Color frameTint = Color.white;
        [SerializeField] private float slotSizeOverride = -1f;
        [SerializeField] private float slotSpacingOverride = -1f;
        [SerializeField] private Vector2 tooltipOffset = new(16f, 16f);

        public InventoryThemeType ThemeType => themeType;
        public StyleSheet[] StyleSheets => styleSheets;
        public Texture2D FrameTexture => frameTexture;
        public ScaleMode FrameScaleMode => frameScaleMode;
        public Color FrameTint => frameTint;
        public float SlotSizeOverride => slotSizeOverride;
        public float SlotSpacingOverride => slotSpacingOverride;
        public Vector2 TooltipOffset => tooltipOffset;
    }
}
