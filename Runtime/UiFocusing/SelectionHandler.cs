using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Focusing
{
    public class SelectionHandler : ISelectionHandler
    {
        private readonly HashSet<ISelectable> _selection = new();

        public event Action<ISelectable> Selected;
        public event Action<ISelectable> Deselected;
        public event Action<IReadOnlyCollection<ISelectable>> SelectionChanged;

        public IReadOnlyCollection<ISelectable> CurrentSelection => _selection;

        public void Select(ISelectable selectable)
        {
            if (_selection.Contains(selectable))
            {
                return;
            }

            if (selectable.IsSingleSelection)
            {
                ClearSelection();
            }

            selectable.Select();
            Selected?.Invoke(selectable);
            SelectionChanged?.Invoke(_selection);
        }

        public void Deselect(ISelectable selectable)
        {
            if (_selection.Remove(selectable))
            {
                selectable.Deselect();
                Deselected?.Invoke(selectable);
                SelectionChanged?.Invoke(_selection);
            }
        }

        private void ClearSelection()
        {
            foreach (var item in _selection)
            {
                item.Deselect();
                Deselected?.Invoke(item);
            }

            _selection.Clear();
            SelectionChanged?.Invoke(_selection);
        }
    }
}
