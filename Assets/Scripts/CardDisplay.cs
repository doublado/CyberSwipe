using UnityEngine;
using UnityEngine.UI;

namespace CyberSwipe
{
    public class CardDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cardImage;

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
        }

        public CardData GetCardData()
        {
            return currentCardData;
        }
    }
} 