using UnityEngine;
using UnityEngine.EventSystems;

namespace CyberSwipe
{
    public class EventHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        private CardManager cardManager;
        private bool isMouseDown = false;
        private Vector2 startPosition;

        private void Awake()
        {
            cardManager = GetComponent<CardManager>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isMouseDown = true;
            startPosition = eventData.position;
            cardManager.OnBeginDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isMouseDown = false;
            cardManager.OnEndDrag(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (isMouseDown)
            {
                cardManager.OnDrag(eventData);
            }
        }
    }
}
