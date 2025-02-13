using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент, соответствующий интерактивной области
    /// </summary>
    public class InteractArea : MonoBehaviour,
        IScrollHandler,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IDragHandler,
        IBeginDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        public event Action<PointerEventData> Click;
        public event Action<PointerEventData> RightClick;
        public event Action<PointerEventData> DoubleClick;
        public event Action<PointerEventData> PointerDown;
        public event Action<PointerEventData> PointerUp;
        public event Action<PointerEventData> BeginDrag;
        public event Action<PointerEventData> Drag;
        public event Action<PointerEventData> EndDrag;
        public event Action<PointerEventData> Scroll;
        public event Action<PointerEventData> HoverEnter;
        public event Action<PointerEventData> HoverExit;

        private float _timeSinceLastClick;
        private const float DoubleClickTime = 0.5f;

        private void OnEnable()
        {
            _timeSinceLastClick = Time.realtimeSinceStartup;
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RightClick?.Invoke(eventData);
                return;
            }

            if (Time.realtimeSinceStartup - _timeSinceLastClick <= DoubleClickTime)
            {
                DoubleClick?.Invoke(eventData);
            }
            else
            {
                Click?.Invoke(eventData);
            }
        
            _timeSinceLastClick = Time.realtimeSinceStartup;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            Drag?.Invoke(eventData);
        }

        public virtual void OnScroll(PointerEventData eventData)
        {
            Scroll?.Invoke(eventData);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            PointerDown?.Invoke(eventData);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            PointerUp?.Invoke(eventData);
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            BeginDrag?.Invoke(eventData);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            EndDrag?.Invoke(eventData);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            HoverEnter?.Invoke(eventData);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            HoverExit?.Invoke(eventData);
        }
    }
}
