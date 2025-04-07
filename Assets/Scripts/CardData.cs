using UnityEngine;

namespace CyberSwipe
{
    /// <summary>
    /// Represents a card in the game, containing all necessary information for display and interaction.
    /// This is a ScriptableObject that can be created and configured in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardData", menuName = "CyberSwipe/Card Data")]
    public class CardData : ScriptableObject
    {
        /// <summary>
        /// Represents the different categories of cybersecurity scenarios that cards can belong to.
        /// </summary>
        public enum Category
        {
            Phishing,
            MaliciousFiles,
            SocialEngineering,
            NetworkSecurity,
            PhysicalSecurity,
            // Add new categories here
        }

        [Header("Card Information")]
        [Tooltip("Unique identifier for this card")]
        public string CardId;
        
        [Tooltip("The category this card belongs to")]
        public Category cardCategory;
        
        [Tooltip("Whether the action depicted on the card is safe to perform")]
        public bool IsSafe;

        [Header("Card Dimensions")]
        [Tooltip("Width of the card in pixels")]
        [SerializeField] private float cardWidth = 600f;
        
        [Tooltip("Height of the card in pixels")]
        [SerializeField] private float cardHeight = 900f;

        [Header("Visual Elements")]
        [Tooltip("The image to display on the card")]
        public Sprite CardImage;
        
        [Tooltip("Scale factor for the card image (0.5 to 2)")]
        [Range(0.5f, 2f)] 
        public float imageScale = 1f;

        [Header("Outcome Information")]
        [Tooltip("Description of what happens if the card is accepted")]
        [TextArea(2, 4)] 
        public string acceptOutcome;
        
        [Tooltip("Description of what happens if the card is denied")]
        [TextArea(2, 4)] 
        public string denyOutcome;
        
        [Tooltip("Impact on company revenue if the card is accepted")]
        public int acceptRevenueImpact;
        
        [Tooltip("Impact on company revenue if the card is denied")]
        public int denyRevenueImpact;

        /// <summary>
        /// Gets the width of the card.
        /// </summary>
        public float GetCardWidth() => cardWidth;

        /// <summary>
        /// Gets the height of the card.
        /// </summary>
        public float GetCardHeight() => cardHeight;

        /// <summary>
        /// Gets the size of the card as a Vector2.
        /// </summary>
        public Vector2 GetCardSize() => new Vector2(cardWidth, cardHeight);
    }
} 