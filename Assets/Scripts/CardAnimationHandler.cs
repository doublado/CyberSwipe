using UnityEngine;
using System.Collections;

namespace CyberSwipe
{
    /// <summary>
    /// Handles the animation of card swiping and returning to center position.
    /// Provides smooth transitions for card movements and rotations.
    /// </summary>
    public class CardAnimationHandler : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Duration of the swipe animation in seconds")]
        [SerializeField] private float swipeDuration = 0.5f;
        
        [Tooltip("Distance the card travels during a swipe")]
        [SerializeField] private float swipeDistance = 1000f;
        
        [Tooltip("Curve defining the acceleration and deceleration of the swipe")]
        [SerializeField] private AnimationCurve swipeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // Component references
        private RectTransform cardRectTransform;
        private Vector2 initialPosition;
        private Quaternion initialRotation;
        private Coroutine activeAnimation;

        private void Awake()
        {
            cardRectTransform = GetComponent<RectTransform>();
            initialPosition = cardRectTransform.anchoredPosition;
            initialRotation = cardRectTransform.rotation;
        }

        /// <summary>
        /// Initiates a swipe animation to the right.
        /// </summary>
        /// <param name="onComplete">Callback to execute when animation finishes</param>
        public void SwipeRight(System.Action onComplete)
        {
            StopActiveAnimation();
            activeAnimation = StartCoroutine(AnimateSwipe(isRightSwipe: true, onComplete));
        }

        /// <summary>
        /// Initiates a swipe animation to the left.
        /// </summary>
        /// <param name="onComplete">Callback to execute when animation finishes</param>
        public void SwipeLeft(System.Action onComplete)
        {
            StopActiveAnimation();
            activeAnimation = StartCoroutine(AnimateSwipe(isRightSwipe: false, onComplete));
        }

        /// <summary>
        /// Returns the card to its initial position with animation.
        /// </summary>
        public void ReturnToCenter()
        {
            StopActiveAnimation();
            activeAnimation = StartCoroutine(AnimateReturnToCenter());
        }

        /// <summary>
        /// Stops any currently running animation.
        /// </summary>
        private void StopActiveAnimation()
        {
            if (activeAnimation != null)
            {
                StopCoroutine(activeAnimation);
            }
        }

        /// <summary>
        /// Animates the card swipe in the specified direction.
        /// </summary>
        /// <param name="isRightSwipe">True for right swipe, false for left swipe</param>
        /// <param name="onComplete">Callback to execute when animation finishes</param>
        private IEnumerator AnimateSwipe(bool isRightSwipe, System.Action onComplete)
        {
            float elapsedTime = 0f;
            Vector2 startPosition = cardRectTransform.anchoredPosition;
            Quaternion startRotation = cardRectTransform.rotation;
            
            // Calculate target position and rotation based on swipe direction
            Vector2 targetPosition = new Vector2(
                isRightSwipe ? swipeDistance : -swipeDistance,
                startPosition.y
            );
            
            Quaternion targetRotation = Quaternion.Euler(0, 0, isRightSwipe ? -30f : 30f);

            while (elapsedTime < swipeDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = swipeCurve.Evaluate(elapsedTime / swipeDuration);

                cardRectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
                cardRectTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);

                yield return null;
            }

            // Ensure final position and rotation are exact
            cardRectTransform.anchoredPosition = targetPosition;
            cardRectTransform.rotation = targetRotation;

            onComplete?.Invoke();
        }

        /// <summary>
        /// Animates the card returning to its initial position.
        /// </summary>
        private IEnumerator AnimateReturnToCenter()
        {
            float elapsedTime = 0f;
            Vector2 startPosition = cardRectTransform.anchoredPosition;
            Quaternion startRotation = cardRectTransform.rotation;

            while (elapsedTime < swipeDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = swipeCurve.Evaluate(elapsedTime / swipeDuration);

                cardRectTransform.anchoredPosition = Vector2.Lerp(startPosition, initialPosition, progress);
                cardRectTransform.rotation = Quaternion.Lerp(startRotation, initialRotation, progress);

                yield return null;
            }

            // Ensure final position and rotation are exact
            cardRectTransform.anchoredPosition = initialPosition;
            cardRectTransform.rotation = initialRotation;
        }
    }
} 