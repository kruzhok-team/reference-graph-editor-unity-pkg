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
        private readonly Action HotkeyPressed;

        public IReadOnlyList<KeyCode> Hotkeys { get; }
        
        public HotkeyAction(Action hotkeyPressed, params KeyCode[] hotkeys)
        {
            HotkeyPressed = hotkeyPressed;
            Hotkeys = hotkeys;
        }

        public void OnHotkeyPressed()
        {
            HotkeyPressed?.Invoke();
        }
    }
}
