using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LostPage
{
    public sealed class CardDragHandler :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private Func<bool> _canDrag;
        private Action<PointerEventData> _beginDrag;
        private Action<PointerEventData> _drag;
        private Action<PointerEventData> _endDrag;
        private ScrollRect _scrollRect;
        private RectTransform _dragBoundary;
        private bool _isCardDragging;
        private bool _isScrolling;

        public void Configure(
            Func<bool> canDrag,
            Action<PointerEventData> beginDrag,
            Action<PointerEventData> drag,
            Action<PointerEventData> endDrag,
            ScrollRect scrollRect,
            RectTransform dragBoundary)
        {
            _canDrag = canDrag;
            _beginDrag = beginDrag;
            _drag = drag;
            _endDrag = endDrag;
            _scrollRect = scrollRect;
            _dragBoundary = dragBoundary;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isCardDragging = false;
            _isScrolling = _scrollRect != null;
            if (_isScrolling)
            {
                _scrollRect.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isCardDragging)
            {
                _drag?.Invoke(eventData);
                return;
            }

            if (IsAboveCardRow(eventData) &&
                _canDrag != null &&
                _canDrag())
            {
                if (_isScrolling)
                {
                    _scrollRect.OnEndDrag(eventData);
                    _isScrolling = false;
                }

                _isCardDragging = true;
                _beginDrag?.Invoke(eventData);
                _drag?.Invoke(eventData);
                return;
            }

            if (_isScrolling)
            {
                _scrollRect.OnDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isCardDragging)
            {
                _endDrag?.Invoke(eventData);
            }
            else if (_isScrolling)
            {
                _scrollRect.OnEndDrag(eventData);
            }

            _isCardDragging = false;
            _isScrolling = false;
        }

        private bool IsAboveCardRow(PointerEventData eventData)
        {
            if (_dragBoundary == null)
            {
                return false;
            }

            var corners = new Vector3[4];
            _dragBoundary.GetWorldCorners(corners);
            var topLeft = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                corners[1]);
            return eventData.position.y > topLeft.y;
        }
    }

    public sealed class EnemyCardDropTarget : MonoBehaviour
    {
        public int EnemyIndex { get; private set; }

        public void Configure(int enemyIndex)
        {
            EnemyIndex = enemyIndex;
        }
    }

    public sealed class CardEffectDropTarget : MonoBehaviour
    {
    }
}
