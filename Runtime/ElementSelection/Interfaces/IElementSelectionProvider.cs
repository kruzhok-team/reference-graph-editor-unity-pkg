using System;
namespace Talent.GraphEditor.Unity.Runtime
{
    public interface IElementSelectionProvider
    {
        event Action<IElementSelectable> Selected;
        event Action<IElementSelectable> Deselected;

        void Select(IElementSelectable selectable);
        void Unselect(IElementSelectable selectable);

        IElementSelectable CurrentSelectedElement { get; }
        void Select(string id);
    }
}
