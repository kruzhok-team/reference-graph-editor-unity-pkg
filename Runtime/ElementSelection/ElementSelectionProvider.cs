using System;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, предоставляющий доступ к выбору элемента
    /// </summary>
    public class ElementSelectionProvider : IElementSelectionProvider
    {
        /// <inheritdoc/>
        public event Action<IElementSelectable> Selected;
        /// <inheritdoc/>
        public event Action<IElementSelectable> Deselected;

        private IElementSelectable _currentSelection;
        /// <inheritdoc/>
        public IElementSelectable CurrentSelectedElement => _currentSelection;
        private readonly RuntimeGraphEditor _runtimeGraphEditor;

        /// <summary>
        /// Конструктор <see cref="ElementSelectionProvider"/>
        /// </summary>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        public ElementSelectionProvider(RuntimeGraphEditor runtimeGraphEditor)
        {
            _runtimeGraphEditor = runtimeGraphEditor;
        }

        /// <inheritdoc/>
        public void Select(IElementSelectable selectable)
        {
            if (_currentSelection == selectable)
            {
                return;
            }

            if (!ReferenceEquals(_runtimeGraphEditor.EditingEdge, selectable) && _runtimeGraphEditor.EditingEdge != null)
            {
                return;
            }

            _currentSelection?.Unselect();

            _currentSelection = selectable;

            Selected?.Invoke(selectable);
        }

        /// <inheritdoc/>
        public void Unselect(IElementSelectable selectable)
        {
            if (_currentSelection != selectable)
            {
                return;
            }

            Deselected?.Invoke(_currentSelection);

            _currentSelection = default;
        }

        /// <inheritdoc/>
        public void Select(string id)
        {
            if (_runtimeGraphEditor.TryGetEdgeViewById(id, out EdgeView edgeView))
            {
                edgeView.Select(false);
            }

            if (_runtimeGraphEditor.TryGetNodeViewById(id, out NodeView nodeView))
            {
                nodeView.Select(false);
            }
        }

        /// <summary>
        /// Отменяет выбор текущего элемента
        /// </summary>
        public void Unselect()
        {
            Deselected?.Invoke(_currentSelection);

            _currentSelection = default;
        }
    }
}
