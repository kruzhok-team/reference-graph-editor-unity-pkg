using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UI.Focusing
{
    public class SimpleContextLayer : MonoBehaviour, IContextLayer
    {
        #region Hotkeys

        [Header("Hotkeys")]
        [SerializeField] private bool _blockOtherHotkeys;
        public bool BlockOtherHotkeys => _blockOtherHotkeys;
        [SerializeField] private bool _isUnblockable;
        public bool IsUnblockable => _isUnblockable;

        [SerializeField] private HotkeyBinding[] _bindings = new HotkeyBinding[0];

        private readonly HashSet<HotkeyAction> _hotkeyActions = new();

        public IEnumerable<HotkeyAction> HotkeysMapping
        {
            get
            {
                if (_hotkeyActions.Count != _bindings.Length)
                {
                    foreach (HotkeyBinding binding in _bindings)
                    {
                        if (binding == null || binding.ActionReference == null)
                        {
                            continue;
                        }

                        HotkeyAction action = new(() => binding.OnPerformed?.Invoke(), null, binding.ActionReference);
                        _hotkeyActions.Add(action);
                    }
                }

                return _hotkeyActions;
            }
        }

        #endregion

        #region Dimming

        [Header("Dimming")]
        [SerializeField] private GameObject[] _focusedElements = new GameObject[0];
        private readonly HashSet<GameObject> _cashedFocusedElements = new();
        private bool _isFocusedCached;

        #endregion

        #region Events

        public UnityEvent Activated;
        public UnityEvent Deactivated;

        #endregion

        public IEnumerable<GameObject> FocusedElements
        {
            get
            {
                if (!_isFocusedCached)
                {
                    foreach (GameObject focusedElement in _focusedElements)
                    {
                        _cashedFocusedElements.Add(focusedElement);
                    }

                    _isFocusedCached = true;
                }

                return _cashedFocusedElements;
            }
        }

        public void AddFocusedElements(params GameObject[] elements)
        {
            foreach (GameObject element in elements)
            {
                _cashedFocusedElements.Add(element);
            }
        }

        public void RemoveFocusedElements(params GameObject[] elements)
        {
            foreach (GameObject element in elements)
            {
                _cashedFocusedElements.Remove(element);
            }
        }

        public void PushLayer()
        {
            UIFocusingSystem.Instance.PushContextLayer(this);
        }

        public void RemoveLayer()
        {
            UIFocusingSystem.Instance.RemoveContextLayer(this);
        }

        public void Activate()
        {
            Activated?.Invoke();
        }

        public void Deactivate()
        {
            Deactivated?.Invoke();
        }
    }

    [Serializable]
    class HotkeyBinding
    {
        public InputActionReference ActionReference;
        public UnityEvent OnPerformed;
    }
}
