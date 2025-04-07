using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

namespace CyberSwipe
{
    /// <summary>
    /// Manages the core game logic, including card deck management, category progression, and game state.
    /// Handles the initialization of new categories, card spawning, and game completion.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [Tooltip("List of all available cards in the game")]
        [SerializeField] private List<CardData> cardDeck = new List<CardData>();
        
        [Tooltip("Prefab used for instantiating new cards")]
        [SerializeField] private GameObject cardPrefab;
        
        [Tooltip("Transform where new cards will be spawned")]
        [SerializeField] private Transform cardSpawnPoint;
        
        [Tooltip("Number of cards to show per category")]
        [SerializeField] private int cardsPerCategory = 5;

        [Header("UI References")]
        [Tooltip("GameObject containing the end screen UI")]
        [SerializeField] private GameObject endScreen;
        
        [Tooltip("Text component for the end screen title")]
        [SerializeField] private TextMeshProUGUI endScreenTitle;
        
        [Tooltip("Text component for displaying card outcomes")]
        [SerializeField] private TextMeshProUGUI endScreenOutcome;
        
        [Tooltip("Text component for displaying revenue impact")]
        [SerializeField] private TextMeshProUGUI endScreenRevenue;

        // Game state tracking
        private List<CardData.Category> completedCategories = new List<CardData.Category>();
        private List<CardData> currentCategoryCards = new List<CardData>();
        private List<string> decisionOutcomes = new List<string>();
        private int totalRevenueImpact = 0;
        private CardData.Category currentCategory;
        private bool gameStarted = false;
        private bool sessionEnded = false;

        private void Start()
        {
            // Make sure end screen is hidden at start
            if (endScreen != null)
            {
                endScreen.SetActive(false);
            }
            
            // Check analytics consent before starting
            if (AnalyticsConsentPopup.HasConsent())
            {
                StartGame();
            }
        }

        /// <summary>
        /// Starts the game if it hasn't already been started.
        /// </summary>
        public void StartGame()
        {
            if (gameStarted) return;
            gameStarted = true;

            ShuffleDeck();
            StartNewCategory();
        }

        /// <summary>
        /// Initializes a new category of cards and prepares for gameplay.
        /// </summary>
        private void StartNewCategory()
        {
            decisionOutcomes.Clear();
            totalRevenueImpact = 0;
            
            if (!SelectRandomCategory())
            {
                Debug.LogError("No valid categories with cards available!");
                return;
            }
            
            // Initialize category stats if analytics is enabled
            if (AnalyticsService.Instance != null && AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                string categoryName = currentCategory.ToString();
                if (!AnalyticsService.Instance.GetCategoryStats().ContainsKey(categoryName))
                {
                    AnalyticsService.Instance.GetCategoryStats()[categoryName] = new AnalyticsService.CategoryStats 
                    { 
                        startTime = Time.time 
                    };
                    Debug.Log($"[GameManager] Initialized stats for category: {categoryName}");
                }
            }
            
            LoadCategoryCards();
        }

        /// <summary>
        /// Selects a random category that hasn't been completed yet.
        /// </summary>
        /// <returns>True if a valid category was selected, false otherwise</returns>
        private bool SelectRandomCategory()
        {
            // Get all categories that have cards in the deck
            var categoriesWithCards = cardDeck
                .Select(card => card.cardCategory)
                .Distinct()
                .Where(category => !completedCategories.Contains(category))
                .ToList();

            if (categoriesWithCards.Count == 0)
            {
                // If all categories are completed, reset and try again
                completedCategories.Clear();
                categoriesWithCards = cardDeck
                    .Select(card => card.cardCategory)
                    .Distinct()
                    .ToList();
            }

            if (categoriesWithCards.Count == 0)
            {
                Debug.LogError("No cards found in the deck!");
                return false;
            }

            currentCategory = categoriesWithCards[Random.Range(0, categoriesWithCards.Count)];
            return true;
        }

        /// <summary>
        /// Loads and shuffles cards for the current category.
        /// </summary>
        private void LoadCategoryCards()
        {
            currentCategoryCards.Clear();
            
            // Filter and shuffle cards for current category
            var categoryCards = cardDeck
                .Where(card => card.cardCategory == currentCategory)
                .OrderBy(x => Random.value)
                .ToList();

            if (categoryCards.Count == 0)
            {
                Debug.LogError($"No cards found for category {currentCategory}");
                completedCategories.Add(currentCategory);
                if (SelectRandomCategory())
                {
                    LoadCategoryCards();
                }
                return;
            }

            // Take only the number of cards we need
            currentCategoryCards = categoryCards.Count > cardsPerCategory 
                ? categoryCards.GetRange(0, cardsPerCategory) 
                : categoryCards;

            SpawnNextCard();
        }

        /// <summary>
        /// Spawns the next card in the current category.
        /// </summary>
        public void SpawnNextCard()
        {
            if (currentCategoryCards.Count == 0)
            {
                ShowEndScreen();
                return;
            }

            GameObject newCard = Instantiate(cardPrefab, cardSpawnPoint.position, Quaternion.identity, cardSpawnPoint);
            CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.Initialize(currentCategoryCards[0]);
            }
        }

        /// <summary>
        /// Handles the decision made on a card (accept or deny).
        /// </summary>
        /// <param name="wasAccepted">Whether the card was accepted or denied</param>
        /// <param name="cardData">The card data that was decided upon</param>
        public void HandleCardDecision(bool wasAccepted, CardData cardData)
        {
            string outcome = wasAccepted ? cardData.acceptOutcome : cardData.denyOutcome;
            int revenueImpact = wasAccepted ? cardData.acceptRevenueImpact : cardData.denyRevenueImpact;
            
            decisionOutcomes.Add(outcome);
            totalRevenueImpact += revenueImpact;

            // Remove the current card and spawn the next one
            currentCategoryCards.RemoveAt(0);
            SpawnNextCard();
        }

        /// <summary>
        /// Displays the end screen with category completion information.
        /// </summary>
        private void ShowEndScreen()
        {
            endScreen.SetActive(true);
            
            // Set title with category name
            string categoryName = GetCategoryNameInDanish(currentCategory);
            endScreenTitle.text = $"Kategori gennemført: {categoryName}";
            
            // Set outcomes text
            string outcomesText = "Beslutninger og resultater:\n\n";
            foreach (string outcome in decisionOutcomes)
            {
                outcomesText += outcome + "\n\n";
            }
            endScreenOutcome.text = outcomesText;
            
            // Set revenue text
            string revenueText = $"Samlet indvirkning på omsætning: {totalRevenueImpact:N0} kr.\n";
            if (totalRevenueImpact < 0)
            {
                revenueText += "Virksomheden led økonomiske tab.";
            }
            else if (totalRevenueImpact > 0)
            {
                revenueText += "Virksomheden opnåede økonomiske gevinster.";
            }
            else
            {
                revenueText += "Virksomhedens økonomiske status forblev uændret.";
            }
            endScreenRevenue.text = revenueText;
            
            // Mark category as completed and track analytics
            completedCategories.Add(currentCategory);
            OnCategoryCompleted(currentCategory.ToString());
            Debug.Log($"[GameManager] Category completed: {currentCategory}");
        }

        /// <summary>
        /// Gets the Danish name for a given category.
        /// </summary>
        /// <param name="category">The category to get the name for</param>
        /// <returns>The Danish name of the category</returns>
        private string GetCategoryNameInDanish(CardData.Category category)
        {
            switch (category)
            {
                case CardData.Category.Phishing:
                    return "Phishing";
                case CardData.Category.MaliciousFiles:
                    return "Skadelige Filer";
                case CardData.Category.SocialEngineering:
                    return "Social Engineering";
                case CardData.Category.NetworkSecurity:
                    return "Netværkssikkerhed";
                case CardData.Category.PhysicalSecurity:
                    return "Fysisk Sikkerhed";
                default:
                    return category.ToString();
            }
        }

        /// <summary>
        /// Continues to the next category after completing the current one.
        /// </summary>
        public void ContinueToNextCategory()
        {
            endScreen.SetActive(false);
            StartNewCategory();
        }

        /// <summary>
        /// Shuffles the card deck using the Fisher-Yates algorithm.
        /// </summary>
        private void ShuffleDeck()
        {
            // Fisher-Yates shuffle algorithm
            for (int i = cardDeck.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                CardData temp = cardDeck[i];
                cardDeck[i] = cardDeck[randomIndex];
                cardDeck[randomIndex] = temp;
            }
        }

        /// <summary>
        /// Called when a category is completed to track analytics.
        /// </summary>
        /// <param name="categoryName">Name of the completed category</param>
        public void OnCategoryCompleted(string categoryName)
        {
            if (AnalyticsService.Instance != null && AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                AnalyticsService.Instance.TrackCategoryCompletion(categoryName);
            }
        }

        private void OnApplicationQuit()
        {
            if (!sessionEnded)
            {
                Debug.Log("[GameManager] Application quitting, ending analytics session");
                if (AnalyticsConsentPopup.IsAnalyticsEnabled())
                {
                    AnalyticsService.Instance.EndSession();
                    sessionEnded = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (!sessionEnded)
            {
                Debug.Log("[GameManager] GameManager destroyed, ending analytics session");
                if (AnalyticsConsentPopup.IsAnalyticsEnabled())
                {
                    AnalyticsService.Instance.EndSession();
                    sessionEnded = true;
                }
            }
        }
    }
} 