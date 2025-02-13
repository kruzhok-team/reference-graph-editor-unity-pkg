using UnityEngine;
using UnityEngine.EventSystems;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Интерфейс, описывающий объект, который можно выделить
    /// </summary>
    public interface IElementSelectable : IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// Игровой объект, который считается выбранным при выделении
        /// </summary>
        GameObject SelectedObject { get; }

        /// <summary>
        /// Источник контекста выбора элемента
        /// </summary>
        ISelectionContextSource SelectionContextSource { get; }
    
        /// <summary>
        /// Отменяет выбор элемента
        /// </summary>
        void Unselect();
    }
}
