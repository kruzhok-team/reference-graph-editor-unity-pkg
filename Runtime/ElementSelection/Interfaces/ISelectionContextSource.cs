using System;
using System.Collections.Generic;
using UnityEngine;

namespace Talent.GraphEditor.Unity.Runtime
{
    public interface ISelectionContextSource
    {
        IEnumerable<HotkeyAction> HotkeyActions { get; }
    }

    public class HotkeyAction
    {
        public KeyCode Hotkey { get; }
        private Action HotkeyPressed;

        public HotkeyAction(KeyCode hotkey, Action hotkeyPressed)
        {
            Hotkey = hotkey;
            HotkeyPressed = hotkeyPressed;
        }

        public void OnHotkeyPressed()
        {
            HotkeyPressed?.Invoke();
        }
    }
}