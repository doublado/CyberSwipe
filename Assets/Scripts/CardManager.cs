using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace CyberSwipe
{
    public class CardManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Card Settings")]
        [SerializeField] private float swipeThreshold = 200f;
        [SerializeField] private float rotationMultiplier = 0.1f;
        [SerializeField] private float returnSpeed = 10f;

        [Header("Visual Feedback")]
        [SerializeField] private Color denyColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color acceptColor = new Color(0.2f, 1f, 0.2f, 0.5f);
        [SerializeField] private float maxGlowAlpha = 0.8f;

        private RectTransform rectTransform;
        private Vector2 startPosition;
        private bool isDragging = false;
        private CardDisplay cardDisplay;
        private GameManager gameManager;
        private CardAnimationHandler animationHandler;
        private Image cardImage;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            cardDisplay = GetComponent<CardDisplay>();
            gameManager = Object.FindFirstObjectByType<GameManager>();
            animationHandler = GetComponent<CardAnimationHandler>();
            cardImage = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            startPosition = rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Only use the horizontal component of the drag
            Vector2 currentPosition = rectTransform.anchoredPosition;
            currentPosition.x += eventData.delta.x;
            rectTransform.anchoredPosition = currentPosition;

            // Calculate rotation based on horizontal position
            float rotation = (currentPosition.x - startPosition.x) * rotationMultiplier;
            rectTransform.rotation = Quaternion.Euler(0, 0, rotation);

            // Update visual feedback based on drag position
            UpdateVisualFeedback(currentPosition.x);
        }

        private void UpdateVisualFeedback(float currentX)
        {
            if (cardImage == null) return;

            float distanceFromCenter = currentX - startPosition.x;
            float normalizedDistance = Mathf.Clamp01(Mathf.Abs(distanceFromCenter) / swipeThreshold);

            if (distanceFromCenter < 0)
            {
                // Dragging left (deny)
                Color color = denyColor;
                color.a = normalizedDistance * maxGlowAlpha;
                cardImage.color = color;
            }
            else
            {
                // Dragging right (accept)
                Color color = acceptColor;
                color.a = normalizedDistance * maxGlowAlpha;
                cardImage.color = color;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            Vector2 currentPosition = rectTransform.anchoredPosition;
            float swipeDistance = currentPosition.x - startPosition.x;

            // Reset card color
            if (cardImage != null)
            {
                cardImage.color = Color.white;
            }

            if (Mathf.Abs(swipeDistance) > swipeThreshold)
            {
                bool wasAccepted = swipeDistance > 0;
                HandleCardDecision(wasAccepted);
            }
            else
            {
                // Always recenter the card if it's not swiped far enough
                if (animationHandler != null)
                {
                    animationHandler.ReturnToCenter();
                }
                else
                {
                    // Fallback if no animation handler
                    StartCoroutine(ReturnToCenterCoroutine());
                }
            }
        }

        private void HandleCardDecision(bool wasAccepted)
        {
            CardData cardData = cardDisplay.GetCardData();
            if (cardData != null && gameManager != null)
            {
                if (animationHandler != null)
                {
                    // Use the animation handler to swipe the card away
                    System.Action onComplete = () => {
                        gameManager.HandleCardDecision(wasAccepted, cardData);
                        Destroy(gameObject); // Destroy the card after animation
                    };
                    
                    if (wasAccepted)
                    {
                        animationHandler.SwipeRight(onComplete);
                    }
                    else
                    {
                        animationHandler.SwipeLeft(onComplete);
                    }
                }
                else
                {
                    // If no animation handler, just process the decision immediately
                    gameManager.HandleCardDecision(wasAccepted, cardData);
                    Destroy(gameObject);
                }
            }
        }

        private IEnumerator ReturnToCenterCoroutine()
        {
            Vector2 currentPosition = rectTransform.anchoredPosition;
            Quaternion currentRotation = rectTransform.rotation;
            float elapsedTime = 0f;

            while (elapsedTime < 1f)
            {
                elapsedTime += Time.deltaTime * returnSpeed;
                rectTransform.anchoredPosition = Vector2.Lerp(currentPosition, startPosition, elapsedTime);
                rectTransform.rotation = Quaternion.Lerp(currentRotation, Quaternion.identity, elapsedTime);
                yield return null;
            }

            // Ensure we reach the exact center position and rotation
            rectTransform.anchoredPosition = startPosition;
            rectTransform.rotation = Quaternion.identity;
        }
    }
} 