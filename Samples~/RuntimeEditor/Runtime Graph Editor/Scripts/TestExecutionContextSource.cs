using System.Collections.Generic;
using Talent.Graphs;

namespace Talent.GraphEditor.Unity.Runtime.Demo
{
    /// <summary>
    /// Класс, реализующий <see cref="IExecutionContextSource"/>
    /// </summary>
    public class TestExecutionContextSource : IExecutionContextSource
    {
        /// <summary>
        /// Возвращает события, поддерживаемые интерпретатором
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetEvents()
        {
            return new List<string>() { "Сканер.ЦельНайдена", "Таймер.Выполнен" };
        }

        /// <summary>
        /// Возвращает действия, поддерживаемые интерпретатором
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ActionData> GetActions()
        {
            return new List<ActionData>()
            {
                new ActionData("Сканер.Поиск", new List<ActionParameter> { new ActionParameter("Направление поиска", "enum", "мин", "макс") }),
                new ActionData("Анализатор.СбросЦели", new List<ActionParameter>()),
                new ActionData("Движение.КЦели", new List<ActionParameter> { new ActionParameter("Скорость", "float") }),
                new ActionData("Движение.Стоп", new List<ActionParameter>()),
                new ActionData("Таймер.Запуск", new List<ActionParameter> { new ActionParameter("Время", "float") }),
                new ActionData("Таймер.Стоп", new List<ActionParameter>())
            };
        }

        /// <summary>
        /// Возвращает переменные, поддерживаемые интерпретатором
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetVariables()
        {
            return new List<string>() { "Анализатор.Дист", "Движение.Скорость", "Движение.Ускорение", "Анализатор.КолвоВрагов", "Анализатор.КолвоСоюзников" };
        }
    }
}
