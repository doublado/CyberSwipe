using UnityEngine;

namespace CyberSwipe
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "CyberSwipe/Card Data")]
    public class CardData : ScriptableObject
    {
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
        public string CardId;
        public Category cardCategory;
        public bool IsSafe; // True if the action is safe to perform

        [Header("Card Dimensions")]
        [Tooltip("Width of the card in pixels")]
        public float cardWidth = 600f;
        [Tooltip("Height of the card in pixels")]
        public float cardHeight = 900f;

        [Header("Visual Elements")]
        public Sprite CardImage;
        [Range(0.5f, 2f)] public float imageScale = 1f; // Scale factor for the image

        [Header("Outcome Information")]
        [TextArea(2, 4)] public string acceptOutcome;
        [TextArea(2, 4)] public string denyOutcome;
        public int acceptRevenueImpact; // Revenue impact if accepted
        public int denyRevenueImpact;   // Revenue impact if denied
    }
} 