using System.Collections.Generic;
using UnityEngine;

namespace UI.Focusing
{
    public interface IContextLayer
    {
        void Activate();
        void Deactivate();

        IEnumerable<HotkeyAction> HotkeysMapping { get; }
        bool BlockOtherHotkeys { get; }

        IEnumerable<GameObject> FocusedElements { get; }
    }
}
