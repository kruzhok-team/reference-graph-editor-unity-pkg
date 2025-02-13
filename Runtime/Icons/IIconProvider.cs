namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Интерфейс, предоставляющий доступ к иконкам
    /// </summary>
    /// <typeparam name="TIconType">Тип иконки</typeparam>
    public interface IIconProvider<TIconType>
    {
        /// <summary>
        /// Пытается найти иконку с определенным идентификатором
        /// </summary>
        /// <param name="key">Уникальный идентификатор иконки</param>
        /// <param name="icon">Возвращает иконку, если иконка с соответсвующий идентификатором существует, иначе null</param>
        /// <returns>true если иконка найдена, иначе false</returns>
        bool TryGetIcon(string key, out TIconType icon);
    }
}
