using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace UI.Focusing
{
    public class SimpleSelectable : MonoBehaviour, ISelectable, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField] private SelectionBinding[] _bindings = new SelectionBinding[0];
        [SerializeField] private bool _isSingleSelection = true;
        [SerializeField] private GameObject _overrideSelectionObject;

        public UnityEvent Selected;
        public UnityEvent Deselected;

        public bool IsSingleSelection => _isSingleSelection;
        public GameObject Object => _overrideSelectionObject ?? gameObject;

        private HashSet<SelectionBinding> _cashedBindings;

        private void Awake()
        {
            _cashedBindings = new();

            foreach (SelectionBinding binding in _bindings)
            {
                _cashedBindings.Add(binding);
            }
        }

        private void OnDestroy()
        {
            UIFocusingSystem.Instance.Deselect(this);
        }

        public void Select()
        {
            Selected?.Invoke();
        }

        public void Deselect()
        {
            Deselected?.Invoke();
        }

        public void ResetSelection()
        {
            UIFocusingSystem.Instance.Deselect(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging || eventData.delta.magnitude > 0)
            {
                return;
            }

            foreach (SelectionBinding binding in _cashedBindings)
            {
                if (eventData.button == binding.MouseButton && binding.Mode == SelectionMode.PointerClick)
                {
                    InvokeBinding(binding);

                    break;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            foreach (SelectionBinding binding in _cashedBindings)
            {
                if (eventData.button == binding.MouseButton && binding.Mode == SelectionMode.PointerDown)
                {
                    InvokeBinding(binding);

                    break;
                }
            }
        }

        private void InvokeBinding(SelectionBinding binding)
        {
            UIFocusingSystem.Instance.Select(this);

            binding.OnPerformed?.Invoke();
        }
    }

    [Serializable]
    class SelectionBinding
    {
        public PointerEventData.InputButton MouseButton;
        public SelectionMode Mode;
        public UnityEvent OnPerformed;
    }

    public enum SelectionMode
    {
        PointerClick,
        PointerDown
    }
}
