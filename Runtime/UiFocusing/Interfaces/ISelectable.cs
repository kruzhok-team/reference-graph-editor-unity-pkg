using UnityEngine;

namespace UI.Focusing
{
    public interface ISelectable
    {
        void Select();
        void Deselect();

        bool IsSingleSelection { get; }
        GameObject Object { get; }
    }
}
