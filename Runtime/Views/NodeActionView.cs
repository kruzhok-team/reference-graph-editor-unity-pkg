using System;
using System.Collections.Generic;
using Talent.GraphEditor.Core;
using TMPro;
using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Представление поведения для перехода в узле
    /// </summary>
    public class NodeActionView : MonoBehaviour, INodeActionView
    {
        [SerializeField] private TextMeshProUGUI _parameterTMP;
        [SerializeField] private GameObject _parameterContainer;
        [Header("Icons")]
        [SerializeField] private Transform _iconsContainer;
        [SerializeField] private Icon _singleIconPrefab;
        [SerializeField] private Icon _doubleIconPrefab;

        private string _actionID;
        private List<Tuple<string, string>> _parameterValue;
        private NodeEventView _eventView;
        private RuntimeGraphEditor _runtimeGraphEditor;
        private IconSpriteProviderAsset _iconProvider;
        private GameObject _currentIcon;

        /// <summary>
        /// Идентификатор поведения
        /// </summary>
        public string ActionID => _actionID;
        /// <summary>
        /// Список параметров поведения
        /// </summary>
        public List<Tuple<string, string>> ParameterValue => _parameterValue;

        /// <summary>
        /// Представление событие, в котором содержится данное поведение
        /// </summary>
        public NodeEventView EventView => _eventView;

        /// <summary>
        /// Инициализирует <see cref="NodeActionView"/>
        /// </summary>
        /// <param name="actionID">Идентификатор поведения</param>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        /// <param name="nodeEventView">Представление перехода в узле</param>
        /// <param name="iconProvider">Объект, предоставляющий доступ к спрайтам</param>
        public void Init(string actionID, RuntimeGraphEditor runtimeGraphEditor, NodeEventView nodeEventView, IconSpriteProviderAsset iconProvider)
        {
            _actionID = actionID;
            _runtimeGraphEditor = runtimeGraphEditor;
            _eventView = nodeEventView;
            _iconProvider = iconProvider;
            _parameterContainer.SetActive(false);

            UpdateIcons(actionID);
        }

        private void UpdateIcons(string id)
        {
            if (_currentIcon != null)
            {
                Destroy(_currentIcon);
            }

            _currentIcon = _iconProvider.GetIconInstance(id);
            _currentIcon.transform.SetParent(_iconsContainer, false);
        }

        /// <summary>
        /// Удаляет представление поведения для перехода в узле
        /// </summary>
        public void Delete()
        {
            _runtimeGraphEditor.GraphEditor.RemoveNodeAction(_eventView, this);
            EventView.RemoveActionView(this);
        }

        /// <summary>
        /// Устанавливает список параметров поведения
        /// </summary>
        /// <param name="parameters">Список параметров поведения</param>
        public void SetParameters(List<Tuple<string, string>> parameters)
        {
            _parameterValue = parameters;

            if (_parameterTMP != null && parameters.Count > 0)
            {
                string parameterValues = "";

                foreach (Tuple<string, string> parameter in parameters)
                {
                    if (!string.IsNullOrEmpty(parameterValues))
                    {
                        parameterValues += ", ";
                    }

                    parameterValues += parameter.Item2;
                }

                _parameterTMP.text = parameterValues;
                _parameterContainer.SetActive(true);
            }
        }
    }
}
