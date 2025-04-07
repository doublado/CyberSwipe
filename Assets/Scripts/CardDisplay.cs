using UnityEngine;
using UnityEngine.UI;

namespace CyberSwipe
{
    /// <summary>
    /// Manages the visual display and appearance of cards in the game.
    /// Handles card image display, glow effects, and visual feedback during interactions.
    /// </summary>
    public class CardDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The Image component that displays the card's main image")]
        [SerializeField] private Image cardImage;
        
        [Tooltip("The Image component that displays the glow effect")]
        [SerializeField] private Image glowImage;

        [Header("Glow Settings")]
        [Tooltip("Height of the glow effect in pixels")]
        [SerializeField] private float glowHeight = 400f;
        
        [Tooltip("Horizontal offset of the glow effect from the card's edge")]
        [SerializeField] private float glowOffset = 0f;
        
        [Tooltip("Vertical offset of the glow effect from the card's center")]
        [SerializeField] private float glowVerticalOffset = 0f;

        // Component references
        private RectTransform rectTransform;
        private CardData currentCardData;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Initializes the card display with the given card data.
        /// </summary>
        /// <param name="cardData">The data to use for displaying the card</param>
        public void Initialize(CardData cardData)
        {
            currentCardData = cardData;
            UpdateDisplay();
        }

        /// <summary>
        /// Updates the visual elements of the card based on the current card data.
        /// </summary>
        private void UpdateDisplay()
        {
            if (currentCardData == null || currentCardData.CardImage == null) return;

            // Set the card dimensions
            rectTransform.sizeDelta = currentCardData.GetCardSize();

            // Set up the card image
            if (cardImage != null)
            {
                cardImage.sprite = currentCardData.CardImage;
                cardImage.color = Color.white;
                cardImage.preserveAspect = true;
                cardImage.type = Image.Type.Simple;
                
                // Ensure the image fills the card while maintaining aspect ratio
                RectTransform imageRect = cardImage.rectTransform;
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;
                
                // Apply the image scale
                imageRect.localScale = Vector3.one * currentCardData.imageScale;
            }

            // Set up the glow image
            if (glowImage != null)
            {
                // Start with transparent glow
                glowImage.color = new Color(1f, 1f, 1f, 0f);
                
                // Ensure the glow is behind the card image
                glowImage.rectTransform.SetAsFirstSibling();
                
                // Set the image type to sliced for proper scaling
                glowImage.type = Image.Type.Sliced;
            }
        }

        /// <summary>
        /// Sets the glow color and position based on the swipe direction.
        /// </summary>
        /// <param name="color">The color to apply to the glow effect</param>
        /// <param name="isRightSide">Whether the glow should appear on the right side of the card</param>
        public void SetGlowColor(Color color, bool isRightSide)
        {
            if (glowImage == null) return;

            glowImage.color = color;
            
            // Adjust the glow to only show on the side being swiped
            RectTransform glowRect = glowImage.rectTransform;
            
            if (isRightSide)
            {
                // Right side glow - triangle pointing right
                glowRect.anchorMin = new Vector2(1f, 0.5f);
                glowRect.anchorMax = new Vector2(1f, 0.5f);
                glowRect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                glowRect.sizeDelta = new Vector2(currentCardData.GetCardHeight(), glowHeight);
                glowRect.anchoredPosition = new Vector2(glowOffset, glowVerticalOffset);
                glowRect.localScale = new Vector3(1f, 1f, 1f);
                glowRect.localRotation = Quaternion.Euler(0f, 0f, -90f);
            }
            else
            {
                // Left side glow - triangle pointing left
                glowRect.anchorMin = new Vector2(0f, 0.5f);
                glowRect.anchorMax = new Vector2(0f, 0.5f);
                glowRect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                glowRect.sizeDelta = new Vector2(currentCardData.GetCardHeight(), glowHeight);
                glowRect.anchoredPosition = new Vector2(-glowOffset, glowVerticalOffset);
                glowRect.localScale = new Vector3(1f, 1f, 1f);
                glowRect.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        /// <summary>
        /// Gets the current card data.
        /// </summary>
        /// <returns>The CardData associated with this display</returns>
        public CardData GetCardData()
        {
            return currentCardData;
        }
    }
} 