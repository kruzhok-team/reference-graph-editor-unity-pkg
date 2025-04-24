using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UI.Focusing
{
    public class SimpleContextLevel : MonoBehaviour, IContextLayer
    {
        public UnityEvent Activated;
        public UnityEvent Deactivated;

        [Header("Hotkeys")]
        [SerializeField] private bool _blockOtherHotkeys;
        public bool BlockOtherHotkeys => _blockOtherHotkeys;

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

        [Header("Dimming")]
        [SerializeField] private GameObject[] _focusedElements = new GameObject[0];
        public IEnumerable<GameObject> FocusedElements => _focusedElements;

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
