using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace UI.Focusing
{
    public class HotkeyAction
    {
        private readonly Action _hotkeyPressed;
        private readonly Func<bool> _shouldProcess;
        public IReadOnlyList<InputActionReference> Hotkeys { get; }

        private readonly Dictionary<InputActionReference, Action<InputAction.CallbackContext>> _callbacks = new();

        public HotkeyAction(Action hotkeyPressed, Func<bool> shouldProcess, params InputActionReference[] hotkeys)
        {
            _hotkeyPressed = hotkeyPressed;
            _shouldProcess = shouldProcess ?? (() => true);
            Hotkeys = hotkeys;
        }

        public void SubscribeTo(InputActionReference inputRef)
        {
            if (inputRef?.action == null || _callbacks.ContainsKey(inputRef))
            {
                return;
            }

            inputRef.action.performed += Callback;
            _callbacks[inputRef] = Callback;

            inputRef.action.Enable();
        }

        private void Callback(InputAction.CallbackContext ctx)
        {
            if (_shouldProcess())
            {
                _hotkeyPressed?.Invoke();
            }
        }

        public void UnsubscribeFrom(InputActionReference inputRef)
        {
            if (inputRef?.action == null || !_callbacks.TryGetValue(inputRef, out Action<InputAction.CallbackContext> callback))
            {
                return;
            }

            inputRef.action.performed -= callback;
            _callbacks.Remove(inputRef);
        }
    }
}
