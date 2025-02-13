namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Интерфейс, реализующий логику истории состояний
    /// </summary>
    public interface IUndoable
    {
        /// <summary>
        /// Возвращается к предыдущему состоянию 
        /// </summary>
        /// <param name="context">Контекст состояния</param>
        void Undo(string context);
        /// <summary>
        /// Повторяет предыдущее состояние
        /// </summary>
        /// <param name="context">Контекст состояния</param>
        void Redo(string context);
        /// <summary>
        /// Возвращает контекст предыдущего состояния
        /// </summary>
        /// <returns>Предыдущее состояние в виде строкового представления</returns>
        string GetUndoContext();
        /// <summary>
        /// Возвращает контекст текущего состояние
        /// </summary>
        /// <returns>Текущее состояние в виде строкового представления</returns>
        string GetCurrentContext();
    }
}
