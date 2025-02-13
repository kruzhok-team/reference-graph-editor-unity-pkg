using Talent.Graphs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, представляющий контейнер для параметров
    /// </summary>
    public class ParameterContainer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _parameterNameText;
        [SerializeField] private TMP_InputField _parameterValueInputField;
        [SerializeField] private TMP_Dropdown _parameterValueDropdown;
        [SerializeField] private Toggle _parameterToggle;
        [SerializeField] private TextMeshProUGUI _toggleNameText;

        /// <summary>
        /// Параметр поведения
        /// </summary>
        public ActionParameter ActionParameter { get; private set; }

        /// <summary>
        /// Текстовый компонент для имени параметра
        /// </summary>
        public TextMeshProUGUI ParameterNameText { get { return _parameterNameText; } }
        /// <summary>
        /// Поле ввода для значения параметра
        /// </summary>
        public TMP_InputField ParameterValueInputField { get { return _parameterValueInputField; } }
        /// <summary>
        /// Выпадающий список значений параметра
        /// </summary>
        public TMP_Dropdown ParameterValueDropdown { get { return _parameterValueDropdown; } }
        /// <summary>
        /// Переключатель параметра
        /// </summary>
        public Toggle ParameterToggle { get { return _parameterToggle; } }
        /// <summary>
        /// Текстовый компонент имени переключателя
        /// </summary>
        public TextMeshProUGUI ToggleNameText { get { return _toggleNameText; } }

        /// <summary>
        /// Инициализирует <see cref="ParameterContainer"/>
        /// </summary>
        /// <param name="actionParameter">Параметр поведения</param>
        public void Init(ActionParameter actionParameter)
        {
            ActionParameter = actionParameter;
        }
    }
}
