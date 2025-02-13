using System;
using System.Collections.Generic;
using Talent.GraphEditor.Core;
using TMPro;
using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, представляющий преставление перехода в узле
    /// </summary>
    public class EdgeActionView : MonoBehaviour, IEdgeActionView
    {
        [SerializeField] private TextMeshProUGUI _actionIDTMP;
        [SerializeField] private TextMeshProUGUI _parameterTMP;
        [Header("Icons")]
        [SerializeField] private Transform _iconsContainer;
        [SerializeField] private Icon _singleIconPrefab;
        [SerializeField] private Icon _doubleIconPrefab;

        private string _actionID;
        private List<Tuple<string, string>> _parameterValue;
        private EdgeView _edgeView;
        private RuntimeGraphEditor _runtimeGraphEditor;
        private IconSpriteProviderAsset _iconProvider;
        private GameObject _currentIcon;

        /// <summary>
        /// Идентификатор поведения
        /// </summary>
        public string ActionID => _actionID;
        /// <summary>
        /// Список параметров
        /// </summary>
        public List<Tuple<string, string>> ParameterValue => _parameterValue;

        /// <summary>
        /// Инициализация <see cref="EdgeActionView"/>
        /// </summary>
        /// <param name="actionID">Идентификатор поведения</param>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        /// <param name="edgeView">Представление ребра</param>
        /// <param name="iconProvider">Ассет, предоставляющий доступ к спрайтам</param>
        public void Init(string actionID, RuntimeGraphEditor runtimeGraphEditor, EdgeView edgeView, IconSpriteProviderAsset iconProvider)
        {
            _actionID = actionID;
            _runtimeGraphEditor = runtimeGraphEditor;
            _edgeView = edgeView;
            _iconProvider = iconProvider;

            if (_actionIDTMP != null)
            {
                _actionIDTMP.text = _actionID;
            }

            UpdateIcons(actionID);
        }

        private void UpdateIcons(string id)
        {
            if (_currentIcon != null)
            {
                GameObject.Destroy(_currentIcon);
            }

            if (id.Split('.').Length > 2)
            {
                return;
            }

            _currentIcon = _iconProvider.GetIconInstance(id);
            _currentIcon.transform.SetParent(_iconsContainer, false);
        }

        /// <summary>
        /// Удаляет представление перехода в ребре
        /// </summary>
        public void Delete()
        {
            _runtimeGraphEditor.GraphEditor.RemoveEdgeAction(_edgeView, this);
        }

        /// <summary>
        /// Устанавливает список параметров поведения для переходов в узле
        /// </summary>
        /// <param name="parameters">Список параметров</param>
        public void SetParameters(List<Tuple<string, string>> parameters)
        {
            _parameterValue = parameters;

            if (_parameterTMP != null)
            {
                string parametersName = "";

                foreach (Tuple<string, string> parameter in parameters)
                {
                    if (!string.IsNullOrEmpty(parametersName))
                    {
                        parametersName += ", ";
                    }

                    parametersName += parameter.Item1;
                }

                _parameterTMP.text = parametersName;
            }
        }
    }
}
