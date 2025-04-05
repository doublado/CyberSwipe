using UnityEngine;
using UnityEngine.UI;

namespace CyberSwipe
{
    public class CardDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image glowImage;

        [Header("Glow Settings")]
        [SerializeField] private float glowHeight = 400f;
        [SerializeField] private float glowOffset = 0f; // Start at the card's edge
        [SerializeField] private float glowVerticalOffset = 0f;

        private RectTransform rectTransform;
        private CardData currentCardData;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(CardData cardData)
        {
            currentCardData = cardData;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (currentCardData == null || currentCardData.CardImage == null) return;

            // Set the card dimensions
            rectTransform.sizeDelta = new Vector2(currentCardData.cardWidth, currentCardData.cardHeight);

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

        public void SetGlowColor(Color color, bool isRightSide)
        {
            if (glowImage != null)
            {
                glowImage.color = color;
                
                // Adjust the glow to only show on the side being swiped
                RectTransform glowRect = glowImage.rectTransform;
                
                if (isRightSide)
                {
                    // Right side glow - triangle pointing right
                    glowRect.anchorMin = new Vector2(1f, 0.5f);
                    glowRect.anchorMax = new Vector2(1f, 0.5f);
                    glowRect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                    glowRect.sizeDelta = new Vector2(currentCardData.cardHeight, glowHeight);
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
                    glowRect.sizeDelta = new Vector2(currentCardData.cardHeight, glowHeight);
                    glowRect.anchoredPosition = new Vector2(-glowOffset, glowVerticalOffset);
                    glowRect.localScale = new Vector3(1f, 1f, 1f);
                    glowRect.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
        }

        public CardData GetCardData()
        {
            return currentCardData;
        }
    }
} 