using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

namespace CyberSwipe
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private List<CardData> cardDeck = new List<CardData>();
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform cardSpawnPoint;
        [SerializeField] private int cardsPerCategory = 5;

        [Header("UI References")]
        [SerializeField] private GameObject endScreen;
        [SerializeField] private TextMeshProUGUI endScreenTitle;
        [SerializeField] private TextMeshProUGUI endScreenOutcome;
        [SerializeField] private TextMeshProUGUI endScreenRevenue;

        private List<CardData.Category> completedCategories = new List<CardData.Category>();
        private List<CardData> currentCategoryCards = new List<CardData>();
        private List<string> decisionOutcomes = new List<string>();
        private int totalRevenueImpact = 0;
        private CardData.Category currentCategory;
        private bool gameStarted = false;

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

        public void StartGame()
        {
            if (gameStarted) return;
            gameStarted = true;

            ShuffleDeck();
            StartNewCategory();
        }

        private void StartNewCategory()
        {
            decisionOutcomes.Clear();
            totalRevenueImpact = 0;
            
            if (!SelectRandomCategory())
            {
                Debug.LogError("No valid categories with cards available!");
                return;
            }
            
            LoadCategoryCards();
        }

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
            
            // Mark category as completed
            completedCategories.Add(currentCategory);
        }

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

        public void ContinueToNextCategory()
        {
            endScreen.SetActive(false);
            StartNewCategory();
        }

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
    }
} 