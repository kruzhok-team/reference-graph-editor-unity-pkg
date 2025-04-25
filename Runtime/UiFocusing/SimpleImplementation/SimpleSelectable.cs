using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace UI.Focusing
{
    public class SimpleSelectable : MonoBehaviour, ISelectable, IPointerClickHandler
    {
        public UnityEvent Selected;
        public UnityEvent Deselected;

        [SerializeField] private SelectionBinding[] _bindings = new SelectionBinding[0];
        [SerializeField] private bool _isSingleSelection = true;
        [SerializeField] private GameObject _overrideSelectionObject;

        public bool IsSingleSelection => _isSingleSelection;
        public GameObject Object => _overrideSelectionObject ?? gameObject;


        private HashSet<SelectionBinding> _cashedBindings = new();

        private void Awake()
        {
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
                if (eventData.button == binding.MouseButton)
                {
                    UIFocusingSystem.Instance.Select(this);

                    binding.OnPerformed?.Invoke();
                    
                    break;
                }
            }
        }
    }

    [Serializable]
    class SelectionBinding
    {
        public PointerEventData.InputButton MouseButton;
        public UnityEvent OnPerformed;
    }
}
