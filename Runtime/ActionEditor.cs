using System.Collections.Generic;
using Talent.Graphs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, представляющий редактор поведения
    /// </summary>
    public class ActionEditor : MonoBehaviour
    {
        [SerializeField] private Icon _actionIcon;
        [SerializeField] private TextMeshProUGUI _parameterText;
        [SerializeField] private TMP_Dropdown _moduleDropdown;
        [SerializeField] private TMP_Dropdown _triggerDropdown;
        [SerializeField] private Button _deleteButton;
        [Header("Action Parameter")]
        [SerializeField] private RectTransform _parameterContainerRoot;
        [SerializeField] private ParameterContainer _parameterContainerPrefab;

        /// <summary>
        /// Иконка поведения
        /// </summary>
        public Icon ActionIcon { get { return _actionIcon; } }
        /// <summary>
        /// Текст параметра
        /// </summary>
        public TextMeshProUGUI ParameterText { get { return _parameterText; } }
        /// <summary>
        /// Раскрывающийся список модулей
        /// </summary>
        public TMP_Dropdown ModuleDropdown { get { return _moduleDropdown; } }
        /// <summary>
        /// Раскрывающийся список событий
        /// </summary>
        public TMP_Dropdown TriggerDropdown { get { return _triggerDropdown; } }
        /// <summary>
        /// Контейнер параметров
        /// </summary>
        public RectTransform ParameterContainer { get { return _parameterContainerRoot; } }
        /// <summary>
        /// Кнопка удаления
        /// </summary>
        public Button DeleteButton { get { return _deleteButton; } }

        private HashSet<ParameterContainer> _parameterContainers = new();
        /// <summary>
        /// Контейнеры параметров
        /// </summary>
        public IEnumerable<ParameterContainer> ParameterContainers => _parameterContainers;

        /// <summary>
        /// Создает параметр
        /// </summary>
        /// <param name="actionParameter">Параметр действия</param>
        /// <returns>Контейнер параметра</returns>
        public ParameterContainer CreateParameter(ActionParameter actionParameter)
        {
            _parameterContainerRoot.gameObject.SetActive(true);

            ParameterContainer parameter = Instantiate(_parameterContainerPrefab, _parameterContainerRoot);

            parameter.Init(actionParameter);

            _parameterContainers.Add(parameter);

            return parameter;
        }

        /// <summary>
        /// Пытается получить контейнер параметра по параметру действия
        /// </summary>
        /// <param name="actionParameter">Параметр действия</param>
        /// <param name="parameterContainer">Возвращает контейнер параметра, если он был найден, иначе null</param>
        /// <returns>true, если контейнер параметра был найден, иначе false</returns>
        public bool TryGetParameter(ActionParameter actionParameter, out ParameterContainer parameterContainer)
        {
            parameterContainer = null;

            foreach (ParameterContainer parameter in ParameterContainers)
            {
                if (parameter.ActionParameter == actionParameter)
                {
                    parameterContainer = parameter;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Сбрасывает вид у списка параметров действий
        /// </summary>
        /// <param name="actionParameters">Список параметров действий</param>
        public void ResetView(List<ActionParameter> actionParameters)
        {
            HashSet<ParameterContainer> parameterContainers = new();

            foreach (ParameterContainer parameter in _parameterContainers)
            {
                if (!actionParameters.Contains(parameter.ActionParameter))
                {
                    Destroy(parameter.gameObject);
                }
                else
                {
                    parameterContainers.Add(parameter);
                }
            }

            _parameterContainers = parameterContainers;
        }
    }
}
