using System.Collections.Generic;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Источник контекста выбора
    /// </summary>
    public class SelectionContextSource : ISelectionContextSource
    {
        private HashSet<HotkeyAction> _hotkeyActions = new();
        /// <inheritdoc/>
        public IEnumerable<HotkeyAction> HotkeyActions => _hotkeyActions;

        /// <summary>
        /// Конструктор <see cref="SelectionContextSource"/>
        /// </summary>
        /// <param name="hotkeyActions">Стартовые действия на горячие клавиши</param>
        public SelectionContextSource(IEnumerable<HotkeyAction> hotkeyActions = null)
        {
            if (hotkeyActions == null)
            {
                return;
            }

            foreach (HotkeyAction hotkeyAction in hotkeyActions)
            {
                _hotkeyActions.Add(hotkeyAction);
            }
        }

        /// <summary>
        /// Добавляет действие на горячую клавишу
        /// </summary>
        /// <param name="hotkeyAction">Действие на горячую клавишу</param>
        public void AddHotkeyAction(HotkeyAction hotkeyAction)
        {
            _hotkeyActions.Add(hotkeyAction);
        }
    }
}
