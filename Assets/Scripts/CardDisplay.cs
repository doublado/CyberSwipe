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
        [SerializeField] private float glowPadding = 10f;
        [SerializeField] private float glowWidth = 40f;

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
                    // Right side glow
                    glowRect.anchorMin = new Vector2(1f, 0f);
                    glowRect.anchorMax = new Vector2(1f, 1f);
                    glowRect.offsetMin = new Vector2(0f, -glowPadding);
                    glowRect.offsetMax = new Vector2(glowWidth, glowPadding);
                }
                else
                {
                    // Left side glow
                    glowRect.anchorMin = new Vector2(0f, 0f);
                    glowRect.anchorMax = new Vector2(0f, 1f);
                    glowRect.offsetMin = new Vector2(-glowWidth, -glowPadding);
                    glowRect.offsetMax = new Vector2(0f, glowPadding);
                }
            }
        }

        public CardData GetCardData()
        {
            return currentCardData;
        }
    }
} 