using UnityEngine;
using System.Collections;

namespace CyberSwipe
{
    public class CardAnimationHandler : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float swipeDuration = 0.5f;
        [SerializeField] private float swipeDistance = 1000f;
        [SerializeField] private AnimationCurve swipeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private RectTransform cardRectTransform;
        private Vector2 originalPosition;
        private Quaternion originalRotation;
        private Coroutine currentAnimation;

        private void Awake()
        {
            cardRectTransform = GetComponent<RectTransform>();
            originalPosition = cardRectTransform.anchoredPosition;
            originalRotation = cardRectTransform.rotation;
        }

        public void SwipeRight(System.Action onComplete)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = StartCoroutine(AnimateSwipe(true, onComplete));
        }

        public void SwipeLeft(System.Action onComplete)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = StartCoroutine(AnimateSwipe(false, onComplete));
        }

        public void ReturnToCenter()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = StartCoroutine(AnimateReturnToCenter());
        }

        private IEnumerator AnimateSwipe(bool swipeRight, System.Action onComplete)
        {
            float elapsedTime = 0f;
            Vector2 startPos = cardRectTransform.anchoredPosition;
            Quaternion startRot = cardRectTransform.rotation;
            
            Vector2 targetPos = new Vector2(
                swipeRight ? swipeDistance : -swipeDistance,
                startPos.y
            );
            
            Quaternion targetRot = Quaternion.Euler(0, 0, swipeRight ? -30f : 30f);

            while (elapsedTime < swipeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = swipeCurve.Evaluate(elapsedTime / swipeDuration);

                cardRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                cardRectTransform.rotation = Quaternion.Lerp(startRot, targetRot, t);

                yield return null;
            }

            // Ensure we reach the exact target position and rotation
            cardRectTransform.anchoredPosition = targetPos;
            cardRectTransform.rotation = targetRot;

            onComplete?.Invoke();
        }

        private IEnumerator AnimateReturnToCenter()
        {
            float elapsedTime = 0f;
            Vector2 startPos = cardRectTransform.anchoredPosition;
            Quaternion startRot = cardRectTransform.rotation;

            while (elapsedTime < swipeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = swipeCurve.Evaluate(elapsedTime / swipeDuration);

                cardRectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
                cardRectTransform.rotation = Quaternion.Lerp(startRot, originalRotation, t);

                yield return null;
            }

            // Ensure we reach the exact original position and rotation
            cardRectTransform.anchoredPosition = originalPosition;
            cardRectTransform.rotation = originalRotation;
        }
    }
} 