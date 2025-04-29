using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Focusing
{
    public class SelectionHandler : ISelectionHandler
    {
        private readonly HashSet<ISelectable> _selection = new();

        public event Action<ISelectable> ElementSelected;
        public event Action<ISelectable> ElementDeselected;
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

            if (_selection.Add(selectable))
            {
                selectable.Select();
                ElementSelected?.Invoke(selectable);
                SelectionChanged?.Invoke(_selection);
            }
        }

        public void Deselect(ISelectable selectable)
        {
            if (_selection.Remove(selectable))
            {
                selectable.Deselect();
                ElementDeselected?.Invoke(selectable);
                SelectionChanged?.Invoke(_selection);
            }
        }

        private void ClearSelection()
        {
            foreach (var item in _selection)
            {
                item.Deselect();
                ElementDeselected?.Invoke(item);
            }

            _selection.Clear();
            SelectionChanged?.Invoke(_selection);
        }
    }
}
