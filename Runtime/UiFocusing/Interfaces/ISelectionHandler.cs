using System;
using System.Collections.Generic;

namespace UI.Focusing
{
    public interface ISelectionHandler
    {
        event Action<ISelectable> Selected;
        event Action<ISelectable> Deselected;
        event Action<IReadOnlyCollection<ISelectable>> SelectionChanged;

        void Select(ISelectable selectable);
        void Deselect(ISelectable selectable);

        IReadOnlyCollection<ISelectable> CurrentSelection { get; }
    }
}
