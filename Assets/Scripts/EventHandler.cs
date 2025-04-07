using UnityEngine;
using UnityEngine.EventSystems;

namespace CyberSwipe
{
    /// <summary>
    /// Handles input events for card interactions, including pointer down, up, and move events.
    /// Acts as an intermediary between Unity's event system and the CardManager.
    /// </summary>
    public class EventHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        // Component references
        private CardManager cardManager;
        
        // Input state tracking
        private bool isMouseDown = false;
        private Vector2 startPosition;

        private void Awake()
        {
            cardManager = GetComponent<CardManager>();
        }

        /// <summary>
        /// Called when the pointer is pressed down on the card.
        /// </summary>
        /// <param name="eventData">Event data containing pointer information</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            isMouseDown = true;
            startPosition = eventData.position;
            cardManager.OnBeginDrag(eventData);
        }

        /// <summary>
        /// Called when the pointer is released from the card.
        /// </summary>
        /// <param name="eventData">Event data containing pointer information</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            isMouseDown = false;
            cardManager.OnEndDrag(eventData);
        }

        /// <summary>
        /// Called when the pointer moves while over the card.
        /// Only processes movement if the pointer is currently down.
        /// </summary>
        /// <param name="eventData">Event data containing pointer information</param>
        public void OnPointerMove(PointerEventData eventData)
        {
            if (isMouseDown)
            {
                cardManager.OnDrag(eventData);
            }
        }
    }
}
