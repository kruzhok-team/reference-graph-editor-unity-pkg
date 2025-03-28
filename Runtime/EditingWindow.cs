using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Talent.Graphs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Окно редактирования элементов графа
    /// </summary>
    public class EditingWindow : MonoBehaviour, IElementSelectable, IUndoable, IPanZoomIgnorer
    {
        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;
        [SerializeField] private Button _saveButton;
        [Header("Event")]
        [SerializeField] private Icon _eventIcon;
        [SerializeField] private TMP_Dropdown _moduleDropdown;
        [SerializeField] private TMP_Dropdown _triggerDropdown;
        [Header("Condition")]
        [SerializeField] private GameObject _conditionContainer;
        [SerializeField] private Toggle _conditionSettingsToggle;
        [SerializeField] private Animator _conditionSettingsAnimator;
        [SerializeField] private Icon _firstVarIcon;
        [SerializeField] private TMP_Dropdown _firstVariableModuleDropdown;
        [SerializeField] private TMP_Dropdown _firstVariableDropdown;
        [SerializeField] private Icon _secondVarIcon;
        [SerializeField] private TMP_Dropdown _conditionSymbolsDropdown;
        [SerializeField] private TMP_Dropdown _secondVariableModuleDropdown;
        [SerializeField] private TMP_Dropdown _secondVariableDropdown;
        [SerializeField] private TMP_InputField _secondVariableInputField;
        [Header("Actions")]
        [SerializeField] private GameObject _actionDropdownHeaders;
        [SerializeField] private Transform _actionsParent;
        [SerializeField] private ActionEditor _actionEditorPrefab;
        [Header("Hotkeys")]
        [SerializeField] private KeyCode _closeKeyCode = KeyCode.Escape;

        private IconSpriteProviderAsset _iconProvider;
        private string[] _conditionSymbols = new string[6] { "!=", ">=", "<=", ">", "<", "==" };
        private NodeView _startNodeView;
        private NodeView _endNodeView;
        private EdgeView _edgeView;
        private List<ActionEditor> _actions = new List<ActionEditor>();
        private Dictionary<string, List<string>> _triggerModules = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _actionModules = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _variableModules = new Dictionary<string, List<string>>();
        private Dictionary<string, List<ActionParameter>> _actionParameters = new Dictionary<string, List<ActionParameter>>();

        private NodeView _nodeView;
        private NodeEventView _nodeEvent;
        private NodeEventView _nodeEventToSave;

        private EditTarget _editTarget;

        /// <summary>
        /// Цель редактирования
        /// </summary>
        public enum EditTarget { Edge, NodeEvent }

        /// <summary>
        /// Название числового условия
        /// </summary>
        public const string NumberConditionName = "Число";

        /// <inheritdoc/>
        public GameObject SelectedObject => gameObject;

        private SelectionContextSource _selectionContextSource;
    
        /// <inheritdoc/>
        public ISelectionContextSource SelectionContextSource => _selectionContextSource;

        private void Awake()
        {
            _selectionContextSource = new SelectionContextSource();
            _selectionContextSource.AddHotkeyAction(new HotkeyAction(Cancel, _closeKeyCode));
        }

        /// <inheritdoc/>
        public void Unselect()
        {
            if (_runtimeGraphEditor.ElementSelectionProvider == null)
            {
                return;
            }

            _runtimeGraphEditor.ElementSelectionProvider.Unselect(this);
        }

        private void OnEnable()
        {
            if (_runtimeGraphEditor.ElementSelectionProvider == null)
            {
                return;
            }

            _runtimeGraphEditor.ElementSelectionProvider.Select(this);
            _runtimeGraphEditor.UndoController.LockUndoable(this);
            _runtimeGraphEditor.LineClickListener.enabled = false;

            SubscribeListeners();
        }

        private void OnDisable()
        {
            _currentUndoState = null;

            _runtimeGraphEditor.UndoController.LockUndoable(null);
            _runtimeGraphEditor.LineClickListener.enabled = true;

            UnsubscribeListeners();
        }

        private void OnConditionInputsChanged(string text)
        {
            RequestCreateUndoState();

            SaveCurrentUndoState();

            if (_secondVariableModuleDropdown.captionText.text == NumberConditionName)
            {
                _secondVarIcon.SetText(_secondVariableInputField.text);
            }
        }

        private void OnConditionToggleChanged(bool isOn)
        {
            _conditionSettingsAnimator.SetBool("IsOn", isOn);

            RequestCreateUndoState();

            SaveCurrentUndoState();
        }

        private void OnConditionSymbolChanged(int value)
        {
            RequestCreateUndoState();

            if (_currentUndoState != null)
            {
                SaveCurrentUndoState();
            }
        }

        /// <summary>
        /// Инициализирует <see cref="EditingWindow"/>
        /// </summary>
        /// <param name="node">Узел</param>
        /// <param name="nodeEvent">Переход в узле</param>
        /// <param name="iconProvider">Объект, предоставляющий доступ к получению спрайтов</param>
        public void Init(NodeView node, NodeEventView nodeEvent, IconSpriteProviderAsset iconProvider)
        {
            _editTarget = EditTarget.NodeEvent;

            _nodeView = node;
            _nodeEventToSave = nodeEvent;
            _nodeEvent = nodeEvent;
            _iconProvider = iconProvider;

            ResetAllSettings(_nodeEvent == null);
            _saveButton.interactable = _nodeEvent != null;

            if (nodeEvent != null)
            {
                GetModuleAndID(nodeEvent.TriggerID, out string moduleKey, out string actionKey);

                _moduleDropdown.value = GetDropdownOptionIndex(_moduleDropdown, moduleKey);
                _triggerDropdown.value = GetDropdownOptionIndex(_triggerDropdown, actionKey);

                UpdateCondition(nodeEvent.Condition);

                foreach (NodeActionView action in nodeEvent.GetComponentsInChildren<NodeActionView>())
                {
                    AddAction(action.ActionID, action.ParameterValue);
                }
            }
            else
            {
                _moduleDropdown.value = 0;

                _eventIcon.firstImage.gameObject.SetActive(false);
                _eventIcon.secondImage.gameObject.SetActive(false);
            }

            OnTriggerChanged(_triggerDropdown.value);
            OnFirstVariableTriggerChanged(_firstVariableDropdown.value);
            OnSecondVariableTriggerChanged(_secondVariableDropdown.value);

            SaveCurrentUndoState();
        }

        /// <summary>
        /// Инициализирует <see cref="EditingWindow"/>
        /// </summary>
        /// <param name="start">Начальный узел</param>
        /// <param name="end">Конечный узел</param>
        /// <param name="iconProvider">Объект, предоставляющий доступ к получению спрайтов</param>
        /// <param name="edge">Ребро</param>
        public void Init(NodeView start, NodeView end, IconSpriteProviderAsset iconProvider, EdgeView edge)
        {
            _editTarget = EditTarget.Edge;

            _startNodeView = start;
            _endNodeView = end;
            _iconProvider = iconProvider;
            _edgeView = edge;

            ResetAllSettings(_edgeView == null);

            if (_edgeView != null)
            {
                _moduleDropdown.options.RemoveAt(0);
                _moduleDropdown.value = 1;
                _moduleDropdown.value = 0;

                GetModuleAndID(_edgeView.TriggerID, out string moduleKey, out string actionKey);

                _moduleDropdown.value = GetDropdownOptionIndex(_moduleDropdown, moduleKey);
                _triggerDropdown.value = GetDropdownOptionIndex(_triggerDropdown, actionKey);


                UpdateCondition(_edgeView.Condition);

                foreach (EdgeActionView action in _edgeView.GetComponentsInChildren<EdgeActionView>())
                {
                    AddAction(action.ActionID, action.ParameterValue);
                }
            }
            else
            {
                _moduleDropdown.options.RemoveAt(1);

                _moduleDropdown.value = 0;

                _eventIcon.firstImage.gameObject.SetActive(false);
                _eventIcon.secondImage.gameObject.SetActive(false);
            }

            OnTriggerChanged(_triggerDropdown.value);
            OnFirstVariableTriggerChanged(_firstVariableDropdown.value);
            OnSecondVariableTriggerChanged(_secondVariableDropdown.value);

            SaveCurrentUndoState();
        }

        /// <summary>
        /// Добавляет действие
        /// </summary>
        public void AddAction()
        {
            SaveCurrentUndoState();
            RequestCreateUndoState();

            AddAction(null);

            SaveCurrentUndoState();
        }

        /// <summary>
        /// Добавляет действие
        /// </summary>
        /// <param name="actionID">Идентификатор действия</param>
        /// <param name="parameters">Список параметров</param>
        public void AddAction(string actionID, List<Tuple<string, string>> parameters = null)
        {
            //создать префаб
            ActionEditor actionEditor = GameObject.Instantiate(_actionEditorPrefab, _actionsParent, false);

            //добавить экшен в список _actions
            _actions.Add(actionEditor);

            //заполнить дропдаун модуля
            var actionModuleOptions = new List<TMP_Dropdown.OptionData>();

            foreach (string actionModule in _actionModules.Keys)
            {
                _iconProvider.TryGetIcon(actionModule, out var sprite);

                var data = new TMP_Dropdown.OptionData();
                data.text = actionModule;
                data.image = sprite;

                actionModuleOptions.Add(data);
            }

            actionEditor.ModuleDropdown.ClearOptions();
            actionEditor.ModuleDropdown.AddOptions(actionModuleOptions);

            GetModuleAndID(actionID, out string moduleKey, out string actionKey);

            //выбрать модуль исходя из actionID
            actionEditor.ModuleDropdown.value = GetDropdownOptionIndex(actionEditor.ModuleDropdown, moduleKey);

            //заполнить дропдаун триггера исходя из выбранного модуля
            var actionTriggerOptions = new List<TMP_Dropdown.OptionData>();

            foreach (string actionTrigger in _actionModules[actionEditor.ModuleDropdown.captionText.text])
            {
                _iconProvider.TryGetIcon(actionTrigger, out var sprite);

                var data = new TMP_Dropdown.OptionData();
                data.text = actionTrigger;
                data.image = sprite;

                actionTriggerOptions.Add(data);
            }

            actionEditor.TriggerDropdown.ClearOptions();
            actionEditor.TriggerDropdown.AddOptions(actionTriggerOptions);

            //выбрать триггер исходя из actionID
            actionEditor.TriggerDropdown.value = GetDropdownOptionIndex(actionEditor.TriggerDropdown, actionKey);

            //заполнить параметр
            if (_actionParameters != null && (parameters == null || !_actionParameters.TryGetValue(actionID, out var configParams) || configParams != null && configParams.Count != parameters.Count))
            {
                OnActionTriggerChanged(actionEditor, null);
            }
            else
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    _currentUndoState = null;

                    Tuple<int, string> item = new Tuple<int, string>(i, parameters[i].Item2);
                    OnActionTriggerChanged(actionEditor, item);
                }
            }

            actionEditor.ActionIcon.UpdateIcons(_iconProvider, $"{moduleKey}.{actionKey}");

            //подписаться на изменение модуля
            actionEditor.ModuleDropdown.onValueChanged.AddListener((x) => OnActionModuleChanged(actionEditor));

            //подписаться на изменения экшена
            actionEditor.TriggerDropdown.onValueChanged.AddListener((x) => OnActionTriggerChanged(actionEditor));

            //подписать удаление экшена на кнопку
            actionEditor.DeleteButton.onClick.AddListener(() => RemoveAction(actionEditor));

            if (_actions.Count > 0)
            {
                _actionDropdownHeaders.gameObject.SetActive(true);
            }
        }

        private void OnActionModuleChanged(ActionEditor actionEditor)
        {
            var actionTriggerOptions = new List<TMP_Dropdown.OptionData>();

            foreach (string actionTrigger in _actionModules[actionEditor.ModuleDropdown.captionText.text])
            {
                _iconProvider.TryGetIcon(actionTrigger, out var sprite);

                var data = new TMP_Dropdown.OptionData();
                data.text = actionTrigger;
                data.image = sprite;

                actionTriggerOptions.Add(data);
            }

            actionEditor.TriggerDropdown.ClearOptions();
            actionEditor.TriggerDropdown.AddOptions(actionTriggerOptions);
            actionEditor.TriggerDropdown.value = GetDropdownOptionIndex(actionEditor.TriggerDropdown, _actionModules[actionEditor.ModuleDropdown.captionText.text][0]);

            OnActionTriggerChanged(actionEditor);
        }

        private void OnActionTriggerChanged(ActionEditor actionEditor, Tuple<int, string> defaultValue = null)
        {
            RequestCreateUndoState();

            actionEditor.ParameterContainer.gameObject.SetActive(false);

            string moduleID = actionEditor.ModuleDropdown.captionText.text;
            string actionID = actionEditor.TriggerDropdown.captionText.text;

            if (_actionModules.TryGetValue(moduleID, out _) && _actionParameters.TryGetValue($"{moduleID}.{actionID}", out List<ActionParameter> actionParameters) && actionParameters != null)
            {
                actionEditor.ResetView(actionParameters);

                if (defaultValue != null)
                {
                    ActionParameter actionParameter = actionParameters[defaultValue.Item1];

                    SetupParameterContainer(actionEditor, actionParameter, defaultValue);
                }
                else
                {
                    foreach (ActionParameter actionParameter in actionParameters)
                    {
                        SetupParameterContainer(actionEditor, actionParameter);
                    }
                }
            }

            actionEditor.ActionIcon.UpdateIcons(_iconProvider, $"{moduleID}.{actionID}");

            _currentUndoState = null;

            OnActionParameterChanged(actionEditor);

            SaveCurrentUndoState();
        }

        private void SetupParameterContainer(ActionEditor actionEditor, ActionParameter actionParameter, Tuple<int, string> defaultValue = null)
        {
            if (!actionEditor.TryGetParameter(actionParameter, out ParameterContainer parameterContainer))
            {
                parameterContainer = actionEditor.CreateParameter(actionParameter);

                parameterContainer.ParameterValueInputField.onValueChanged.AddListener((x) => OnActionParameterChanged(actionEditor));
                parameterContainer.ParameterValueDropdown.onValueChanged.AddListener((x) => OnActionParameterChanged(actionEditor));
                parameterContainer.ParameterToggle.onValueChanged.AddListener((x) => OnActionParameterChanged(actionEditor));
            }

            actionEditor.ParameterContainer.gameObject.SetActive(true);
            parameterContainer.ParameterNameText.text = actionParameter.Name;

            parameterContainer.ParameterValueDropdown.gameObject.SetActive(false);
            parameterContainer.ParameterValueInputField.gameObject.SetActive(false);
            parameterContainer.ParameterToggle.gameObject.SetActive(false);

            switch (actionParameter.Type)
            {
                case "bool":
                    parameterContainer.ParameterToggle.gameObject.SetActive(true);
                    parameterContainer.ToggleNameText.text = actionParameter.Name;
                    parameterContainer.ParameterToggle.isOn = defaultValue == null || !bool.TryParse(defaultValue.Item2, out bool result) ? true : result;
                    break;
                case "string":
                    parameterContainer.ParameterValueInputField.gameObject.SetActive(true);
                    parameterContainer.ParameterValueInputField.contentType = TMP_InputField.ContentType.Standard;
                    parameterContainer.ParameterValueInputField.text = defaultValue == null ? "" : defaultValue.Item2;
                    break;
                case "int":
                    parameterContainer.ParameterValueInputField.gameObject.SetActive(true);
                    parameterContainer.ParameterValueInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                    parameterContainer.ParameterValueInputField.text = defaultValue == null ? "" : defaultValue.Item2;
                    break;
                case "float":
                    parameterContainer.ParameterValueInputField.gameObject.SetActive(true);
                    parameterContainer.ParameterValueInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                    parameterContainer.ParameterValueInputField.text = defaultValue == null ? "" : defaultValue.Item2;
                    break;
                case "enum":
                    if (actionParameter.Values != null)
                    {
                        parameterContainer.ParameterValueDropdown.gameObject.SetActive(true);
                        parameterContainer.ParameterValueDropdown.ClearOptions();
                        parameterContainer.ParameterValueDropdown.AddOptions(actionParameter.Values.ToList());
                        parameterContainer.ParameterValueDropdown.value = defaultValue != null ? GetDropdownOptionIndex(parameterContainer.ParameterValueDropdown, defaultValue.Item2) : 0;
                    }
                    break;
            }
        }

        private void OnActionParameterChanged(ActionEditor actionEditor)
        {
            RequestCreateUndoState();

            string moduleID = actionEditor.ModuleDropdown.captionText.text;
            string actionID = actionEditor.TriggerDropdown.captionText.text;

            string parameter = null;

            if (_actionModules.TryGetValue(moduleID, out _) && _actionParameters.TryGetValue($"{moduleID}.{actionID}", out List<ActionParameter> actionParameters) && actionParameters != null)
            {
                actionEditor.ResetView(actionParameters);

                foreach (ActionParameter actionParameter in actionParameters)
                {
                    if (!actionEditor.TryGetParameter(actionParameter, out ParameterContainer parameterContainer))
                    {
                        parameterContainer = actionEditor.CreateParameter(actionParameter);

                        parameterContainer.ParameterValueInputField.onValueChanged.AddListener((x) => OnActionParameterChanged(actionEditor));
                        parameterContainer.ParameterValueDropdown.onValueChanged.AddListener((x) => OnActionParameterChanged(actionEditor));
                    }

                    actionEditor.ParameterContainer.gameObject.SetActive(true);

                    parameterContainer.ParameterNameText.text = actionParameter.Name;

                    switch (actionParameter.Type)
                    {
                        case "bool":
                            parameter = parameterContainer.ParameterToggle.isOn.ToString();
                            break;
                        case "string":
                            parameter = parameterContainer.ParameterValueInputField.text;
                            break;
                        case "int":
                            parameter = parameterContainer.ParameterValueInputField.text;
                            break;
                        case "float":
                            parameter = parameterContainer.ParameterValueInputField.text;
                            break;
                        case "enum":
                            parameter = parameterContainer.ParameterValueDropdown.captionText.text;
                            break;
                    }
                }
            }

            actionEditor.ParameterText.text = parameter;

            if (_currentUndoState != null)
            {
                SaveCurrentUndoState();
            }
        }

        private void RemoveAction(ActionEditor actionEditor)
        {
            SaveCurrentUndoState();
            RequestCreateUndoState();

            Destroy(actionEditor.gameObject);

            _actions.Remove(actionEditor);

            if (_actions.Count == 0)
            {
                _actionDropdownHeaders.gameObject.SetActive(false);
            }

            SaveCurrentUndoState();
        }

        /// <summary>
        /// Сохраняет настройки редактируемого окна
        /// </summary>
        public void Save()
        {
            string currentTrigger = _triggerDropdown.captionText.text;

            if (_moduleDropdown.captionText.text != "System")
            {
                currentTrigger = $"{_moduleDropdown.captionText.text}.{currentTrigger}";
            }

            List<Tuple<string, List<Tuple<string, string>>>> currentActions = new();

            var childActions = _actionsParent.GetComponentsInChildren<ActionEditor>();

            foreach (ActionEditor action in childActions)
            {
                string moduleID = action.ModuleDropdown.captionText.text;
                string actionID = action.TriggerDropdown.captionText.text;
                string parameter = null;

                List<Tuple<string, string>> parameters = new();

                if (_actionModules.TryGetValue(moduleID, out _) && _actionParameters.TryGetValue($"{moduleID}.{actionID}", out List<ActionParameter> actionParameters) && actionParameters != null)
                {
                    foreach (ParameterContainer parameterContainer in action.ParameterContainers)
                    {
                        switch (parameterContainer.ActionParameter.Type)
                        {
                            case "bool":
                                parameter = parameterContainer.ParameterToggle.isOn.ToString();
                                break;
                            case "string":
                                parameter = parameterContainer.ParameterValueInputField.text;
                                break;
                            case "int":
                                parameter = string.IsNullOrEmpty(parameterContainer.ParameterValueInputField.text) ? "0" : parameterContainer.ParameterValueInputField.text;
                                break;
                            case "float":
                                parameter = string.IsNullOrEmpty(parameterContainer.ParameterValueInputField.text) ? "0" : parameterContainer.ParameterValueInputField.text;
                                break;
                            case "enum":
                                parameter = parameterContainer.ParameterValueDropdown.captionText.text;
                                break;
                        }

                        parameters.Add(new Tuple<string, string>(parameterContainer.ActionParameter.Name, parameter));
                    }

                    currentActions.Add(new Tuple<string, List<Tuple<string, string>>>($"{moduleID}.{actionID}", parameters));
                }
            }

            string firstVar = _firstVariableDropdown.captionText.text;

            if (_firstVariableModuleDropdown.captionText.text != "System")
            {
                firstVar = $"{_firstVariableModuleDropdown.captionText.text}.{firstVar}";
            }

            string secondVar = _secondVariableDropdown.captionText.text;

            if (_secondVariableModuleDropdown.captionText.text != "System")
            {
                if (_secondVariableModuleDropdown.captionText.text == NumberConditionName)
                {
                    if (string.IsNullOrEmpty(_secondVariableInputField.text))
                    {
                        _secondVariableInputField.text = "0";
                    }

                    secondVar = $"{_secondVariableInputField.text}";
                }
                else
                {
                    secondVar = $"{_secondVariableModuleDropdown.captionText.text}.{secondVar}";
                }
            }

            string currentCondition = $"{firstVar} {_conditionSymbolsDropdown.captionText.text} {secondVar}";

            _runtimeGraphEditor.CancelEditingWindow();
            switch (_editTarget)
            {
                case EditTarget.Edge:
                    _runtimeGraphEditor.SaveEdge(_startNodeView, _endNodeView, currentTrigger, _conditionSettingsToggle.isOn && _conditionContainer.activeSelf ? currentCondition : null, currentActions, _edgeView);
                    break;
                case EditTarget.NodeEvent:
                    if (_nodeEventToSave == null)
                    {
                        _nodeEventToSave = _nodeEvent;
                    }
                    _runtimeGraphEditor.SaveNodeEvent(_nodeView, currentTrigger, _conditionSettingsToggle.isOn && _conditionContainer.activeSelf ? currentCondition : null, currentActions, _nodeEventToSave);

                    break;
            }
        }

        /// <summary>
        /// Закрывает редактируемое окно
        /// </summary>
        public void Cancel()
        {
            if (_runtimeGraphEditor.EditingEdge != null && _runtimeGraphEditor.EditingEdge.IsPreview)
            {
                _runtimeGraphEditor.EditingEdge.Delete();
                _runtimeGraphEditor.EditingEdge = null;
            }
            
            _runtimeGraphEditor.CancelEditingWindow();
        }

        /// <summary>
        /// Удаляет выбранный элемент в окне редактирования
        /// </summary>
        public void Delete()
        {
            switch (_editTarget)
            {
                case EditTarget.Edge:

                    if (_edgeView != null)
                    {
                        _edgeView.Delete();
                    }
                    break;
                case EditTarget.NodeEvent:
                    if (_nodeEventToSave == null)
                    {
                        _nodeEventToSave = _nodeEvent;
                    }

                    if (_nodeEventToSave != null)
                    {
                        _nodeEventToSave.Delete();
                    }
                    break;
            }

            _runtimeGraphEditor.CancelEditingWindow();
        }

        private void OnModuleChanged(int moduleIndex)
        {
            var triggerOptions = new List<TMP_Dropdown.OptionData>();

            _saveButton.interactable = !string.IsNullOrEmpty(_moduleDropdown.captionText.text);

            if (_triggerModules.ContainsKey(_moduleDropdown.captionText.text))
            {
                foreach (string trigger in _triggerModules[_moduleDropdown.captionText.text])
                {
                    _iconProvider.TryGetIcon(trigger, out var sprite);

                    var data = new TMP_Dropdown.OptionData();
                    data.text = trigger;
                    data.image = sprite;

                    triggerOptions.Add(data);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(_moduleDropdown.captionText.text))
                {
                    _eventIcon.firstImage.gameObject.SetActive(false);
                    _eventIcon.secondImage.gameObject.SetActive(false);
                }

                _triggerDropdown.ClearOptions();
                return;
            }

            _triggerDropdown.ClearOptions();
            _triggerDropdown.AddOptions(triggerOptions);
            _triggerDropdown.value = GetDropdownOptionIndex(_triggerDropdown, _triggerModules[_moduleDropdown.captionText.text][0]);
            OnTriggerChanged(_triggerDropdown.value);
        }

        private void OnTriggerChanged(int triggerIndex)
        {
            RequestCreateUndoState();

            string currentTrigger = _triggerDropdown.captionText.text;

            if (_moduleDropdown.captionText.text != "System")
            {
                currentTrigger = $"{_moduleDropdown.captionText.text}.{currentTrigger}";
            }

            _conditionContainer.SetActive(_moduleDropdown.captionText.text != "System");
            _conditionSettingsToggle.onValueChanged?.Invoke(_conditionSettingsToggle.isOn);

            _eventIcon.UpdateIcons(_iconProvider, currentTrigger);

            if (_currentUndoState != null)
            {
                SaveCurrentUndoState();
            }
        }

        private void OnFirstVariableModuleChanged(int moduleIndex)
        {
            var variableOptions = new List<TMP_Dropdown.OptionData>();

            foreach (string variable in _variableModules[_firstVariableModuleDropdown.captionText.text])
            {
                _iconProvider.TryGetIcon(variable, out var sprite);

                var data = new TMP_Dropdown.OptionData();
                data.text = variable;
                data.image = sprite;

                variableOptions.Add(data);
            }

            if (_firstVariableModuleDropdown.captionText.text == NumberConditionName)
            {
                _firstVariableDropdown.gameObject.SetActive(false);
                OnFirstVariableTriggerChanged(_firstVariableDropdown.value);

                return;
            }

            _firstVariableDropdown.gameObject.SetActive(true);

            _firstVariableDropdown.ClearOptions();
            _firstVariableDropdown.AddOptions(variableOptions);
            _firstVariableDropdown.value = GetDropdownOptionIndex(_firstVariableDropdown, _variableModules[_firstVariableModuleDropdown.captionText.text][0]);
            OnFirstVariableTriggerChanged(_firstVariableDropdown.value);
        }

        private void OnFirstVariableTriggerChanged(int moduleIndex)
        {
            RequestCreateUndoState();

            string currentTrigger = _firstVariableDropdown.captionText.text;

            if (_firstVariableModuleDropdown.captionText.text != "System")
            {
                if (_firstVariableModuleDropdown.captionText.text == NumberConditionName)
                {
                    currentTrigger = NumberConditionName;
                }
                else
                {
                    currentTrigger = $"{_firstVariableModuleDropdown.captionText.text}.{currentTrigger}";
                }
            }

            _firstVarIcon.UpdateIcons(_iconProvider, currentTrigger);

            if (_currentUndoState != null)
            {
                SaveCurrentUndoState();
            }
        }

        private void OnSecondVariableModuleChanged(int moduleIndex)
        {
            var variableOptions = new List<TMP_Dropdown.OptionData>();

            foreach (string variable in _variableModules[_secondVariableModuleDropdown.captionText.text])
            {
                _iconProvider.TryGetIcon(variable, out var sprite);

                var data = new TMP_Dropdown.OptionData();
                data.text = variable;
                data.image = sprite;

                variableOptions.Add(data);
            }

            if (_secondVariableModuleDropdown.captionText.text == NumberConditionName)
            {
                _secondVariableInputField.gameObject.SetActive(true);
                _secondVariableDropdown.gameObject.SetActive(false);
                OnSecondVariableTriggerChanged(_secondVariableDropdown.value);

                return;
            }

            _secondVariableInputField.gameObject.SetActive(false);
            _secondVariableDropdown.gameObject.SetActive(true);

            _secondVariableDropdown.ClearOptions();
            _secondVariableDropdown.AddOptions(variableOptions);
            _secondVariableDropdown.value = GetDropdownOptionIndex(_secondVariableDropdown, _variableModules[_secondVariableModuleDropdown.captionText.text][0]);
            OnSecondVariableTriggerChanged(_secondVariableDropdown.value);
        }

        private void OnSecondVariableTriggerChanged(int moduleIndex)
        {
            RequestCreateUndoState();

            string currentTrigger = _secondVariableDropdown.captionText.text;

            if (_secondVariableModuleDropdown.captionText.text != "System")
            {
                if (_secondVariableModuleDropdown.captionText.text == NumberConditionName)
                {
                    currentTrigger = NumberConditionName;
                }
                else
                {
                    currentTrigger = $"{_secondVariableModuleDropdown.captionText.text}.{currentTrigger}";
                }
            }

            if (currentTrigger == NumberConditionName)
            {
                _secondVarIcon.SetText(_secondVariableInputField.text);
            }
            else
            {
                _secondVarIcon.UpdateIcons(_iconProvider, currentTrigger);
            }

            if (_currentUndoState != null)
            {
                SaveCurrentUndoState();
            }
        }

        private void UpdateCondition(string newCondition)
        {
            _conditionSettingsToggle.isOn = !string.IsNullOrEmpty(newCondition);
            _conditionSettingsAnimator.SetBool("IsOn", _conditionSettingsToggle.isOn);

            if (_conditionSettingsToggle.isOn)
            {
                foreach (string conditionSymbol in _conditionSymbols)
                {
                    string[] splittedCondition = newCondition.Split(conditionSymbol, System.StringSplitOptions.RemoveEmptyEntries);

                    if (splittedCondition.Length > 1)
                    {
                        GetModuleAndID(splittedCondition[0].Trim(), out string firstVarModuleKey, out string firstVarActionKey);

                        _firstVariableModuleDropdown.value = GetDropdownOptionIndex(_firstVariableModuleDropdown, firstVarModuleKey);
                        _firstVariableDropdown.value = GetDropdownOptionIndex(_firstVariableDropdown, firstVarActionKey);

                        string rightHand = splittedCondition[1].Trim();

                        if (float.TryParse(rightHand, out _))
                        {
                            _secondVariableModuleDropdown.value = GetDropdownOptionIndex(_secondVariableModuleDropdown, NumberConditionName);
                            _secondVariableInputField.text = rightHand;
                        }
                        else
                        {
                            GetModuleAndID(rightHand, out string secondVarModuleKey, out string secondVarActionKey);

                            _secondVariableModuleDropdown.value = GetDropdownOptionIndex(_secondVariableModuleDropdown, secondVarModuleKey);
                            _secondVariableDropdown.value = GetDropdownOptionIndex(_secondVariableDropdown, secondVarActionKey);
                        }

                        _conditionSymbolsDropdown.value = GetDropdownOptionIndex(_conditionSymbolsDropdown, conditionSymbol);

                        break;
                    }
                }
            }
        }

        private void SubscribeListeners()
        {
            _triggerDropdown.onValueChanged.AddListener(OnTriggerChanged);

            _firstVariableModuleDropdown.onValueChanged.AddListener(OnFirstVariableModuleChanged);
            _firstVariableDropdown.onValueChanged.AddListener(OnFirstVariableTriggerChanged);

            _secondVariableModuleDropdown.onValueChanged.AddListener(OnSecondVariableModuleChanged);
            _secondVariableDropdown.onValueChanged.AddListener(OnSecondVariableTriggerChanged);

            _conditionSymbolsDropdown.onValueChanged.AddListener(OnConditionSymbolChanged);
            _conditionSettingsToggle.onValueChanged.AddListener(OnConditionToggleChanged);
            _secondVariableInputField.onValueChanged.AddListener(OnConditionInputsChanged);
        }

        private void UnsubscribeListeners()
        {
            _triggerDropdown.onValueChanged.RemoveListener(OnTriggerChanged);

            _firstVariableModuleDropdown.onValueChanged.RemoveListener(OnFirstVariableModuleChanged);
            _firstVariableDropdown.onValueChanged.RemoveListener(OnFirstVariableTriggerChanged);

            _secondVariableModuleDropdown.onValueChanged.RemoveListener(OnSecondVariableModuleChanged);
            _secondVariableDropdown.onValueChanged.RemoveListener(OnSecondVariableTriggerChanged);

            _conditionSymbolsDropdown.onValueChanged.RemoveListener(OnConditionSymbolChanged);
            _conditionSettingsToggle.onValueChanged.RemoveListener(OnConditionToggleChanged);
            _secondVariableInputField.onValueChanged.RemoveListener(OnConditionInputsChanged);
        }

        private void ResetAllSettings(bool includeEmptyModule = false)
        {
            //Triggers
            _triggerModules.Clear();
            _moduleDropdown.onValueChanged.RemoveListener(OnModuleChanged);
            UnsubscribeListeners();

            _secondVariableInputField.text = "0";

            foreach (string trigger in _runtimeGraphEditor.Triggers)
            {
                GetModuleAndID(trigger, out string moduleKey, out string triggerKey);

                if (!_triggerModules.ContainsKey(moduleKey))
                {
                    _triggerModules.Add(moduleKey, new List<string>());
                }

                _triggerModules[moduleKey].Add(triggerKey);
            }

            string firstTriggerModule = _triggerModules.Keys.First();

            var moduleOptions = new List<TMP_Dropdown.OptionData>();

            if (includeEmptyModule)
            {
                moduleOptions.Add(new TMP_Dropdown.OptionData());
            }

            foreach (string module in _triggerModules.Keys)
            {
                _iconProvider.TryGetIcon(module, out var sprite);

                var optionData = new TMP_Dropdown.OptionData();
                optionData.text = module;
                optionData.image = sprite;

                moduleOptions.Add(optionData);
            }

            _moduleDropdown.ClearOptions();
            _moduleDropdown.AddOptions(moduleOptions);
            _moduleDropdown.value = GetDropdownOptionIndex(_moduleDropdown, firstTriggerModule);
            _moduleDropdown.onValueChanged.AddListener(OnModuleChanged);

            var triggerOptions = new List<TMP_Dropdown.OptionData>();

            foreach (string trigger in _triggerModules[firstTriggerModule])
            {
                _iconProvider.TryGetIcon(trigger, out var sprite);

                var optionData = new TMP_Dropdown.OptionData();
                optionData.text = trigger;
                optionData.image = sprite;

                triggerOptions.Add(optionData);
            }

            _triggerDropdown.ClearOptions();
            _triggerDropdown.AddOptions(triggerOptions);
            _triggerDropdown.value = GetDropdownOptionIndex(_triggerDropdown, _triggerModules[firstTriggerModule][0]);

            //Variables
            _variableModules.Clear();
            _firstVariableModuleDropdown.onValueChanged.RemoveListener(OnFirstVariableModuleChanged);
            _firstVariableDropdown.onValueChanged.RemoveListener(OnFirstVariableTriggerChanged);
            _secondVariableModuleDropdown.onValueChanged.RemoveListener(OnSecondVariableModuleChanged);
            _secondVariableDropdown.onValueChanged.RemoveListener(OnSecondVariableTriggerChanged);

            foreach (string trigger in _runtimeGraphEditor.Variables)
            {
                GetModuleAndID(trigger, out string moduleKey, out string triggerKey);

                if (!_variableModules.ContainsKey(moduleKey))
                {
                    _variableModules.Add(moduleKey, new List<string>());
                }

                _variableModules[moduleKey].Add(triggerKey);
            }

            _variableModules.Add(NumberConditionName, new List<string>());

            string firstVariableModule = _variableModules.Keys.First();

            //first var

            var variableModuleOptions1 = new List<TMP_Dropdown.OptionData>();

            _iconProvider.TryGetIcon(NumberConditionName, out var numberSprite);

            var data = new TMP_Dropdown.OptionData();
            data.text = NumberConditionName;
            data.image = numberSprite;

            var variableModuleOptions2 = new List<TMP_Dropdown.OptionData>() { data };

            foreach (string variableModule in _variableModules.Keys)
            {
                if (variableModule == NumberConditionName)
                    continue;

                _iconProvider.TryGetIcon(variableModule, out var sprite);

                data = new TMP_Dropdown.OptionData();
                data.text = variableModule;
                data.image = sprite;

                variableModuleOptions1.Add(data);
                variableModuleOptions2.Add(data);
            }

            var variableOptions = new List<TMP_Dropdown.OptionData>();

            foreach (string variable in _variableModules[firstVariableModule])
            {
                _iconProvider.TryGetIcon(variable, out var sprite);

                data = new TMP_Dropdown.OptionData();
                data.text = variable;
                data.image = numberSprite;

                variableOptions.Add(data);
            }

            _firstVariableModuleDropdown.ClearOptions();
            _firstVariableModuleDropdown.AddOptions(variableModuleOptions1);
            _firstVariableModuleDropdown.value = GetDropdownOptionIndex(_firstVariableModuleDropdown, firstVariableModule);

            _firstVariableDropdown.ClearOptions();
            _firstVariableDropdown.AddOptions(variableOptions);
            _firstVariableDropdown.value = GetDropdownOptionIndex(_firstVariableDropdown, _variableModules[firstVariableModule][0]);

            if (_firstVariableModuleDropdown.captionText.text == NumberConditionName)
            {
                _firstVariableDropdown.gameObject.SetActive(false);
            }
            else
            {
                _firstVariableDropdown.gameObject.SetActive(true);
            }

            //second var
            _secondVariableModuleDropdown.ClearOptions();
            _secondVariableModuleDropdown.AddOptions(variableModuleOptions2);
            _secondVariableModuleDropdown.value = GetDropdownOptionIndex(_secondVariableModuleDropdown, NumberConditionName);

            _secondVariableDropdown.ClearOptions();
            _secondVariableDropdown.AddOptions(variableOptions);
            _secondVariableDropdown.value = 0;

            if (_secondVariableModuleDropdown.captionText.text == NumberConditionName)
            {
                _secondVariableInputField.gameObject.SetActive(true);
                _secondVariableDropdown.gameObject.SetActive(false);
            }
            else
            {
                _secondVariableInputField.gameObject.SetActive(false);
                _secondVariableDropdown.gameObject.SetActive(true);
            }

            //condition
            _conditionSettingsToggle.isOn = false;
            _conditionSettingsAnimator.SetBool("IsOn", _conditionSettingsToggle.isOn);

            _conditionSymbolsDropdown.ClearOptions();
            _conditionSymbolsDropdown.AddOptions(_conditionSymbols.ToList());

            //Actions
            _actionModules.Clear();
            _actionParameters.Clear();

            foreach (ActionData actionData in _runtimeGraphEditor.Actions)
            {
                GetModuleAndID(actionData.ID, out string moduleKey, out string actionKey);

                if (!_actionModules.ContainsKey(moduleKey))
                {
                    _actionModules.Add(moduleKey, new List<string>());
                }

                _actionModules[moduleKey].Add(actionKey);

                _actionParameters.Add(actionData.ID, actionData.Parameters);
            }

            foreach (ActionEditor action in _actions)
            {
                GameObject.Destroy(action.gameObject);
            }

            _actions.Clear();

            _actionDropdownHeaders.gameObject.SetActive(false);

            SubscribeListeners();
        }

        private void GetModuleAndID(string stringToSplit, out string moduleKey, out string actionKey)
        {
            if (string.IsNullOrEmpty(stringToSplit))
            {
                moduleKey = "";
                actionKey = "";
            }
            else
            {
                if (stringToSplit == NumberConditionName)
                {
                    moduleKey = NumberConditionName;
                    actionKey = NumberConditionName;

                    return;
                }

                string[] splitted = stringToSplit.Split('.');
                moduleKey = splitted.Length < 2 ? "System" : splitted[0];
                actionKey = splitted.Length < 2 ? splitted[0] : splitted[1];
            }
        }

        private int GetDropdownOptionIndex(TMP_Dropdown dropdown, string option)
        {
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                if (dropdown.options[i].text == option)
                {
                    return i;
                }
            }

            return -1;
        }

    #region Undo

        private UndoState _currentUndoState;

        private void SaveCurrentUndoState()
        {
            _currentUndoState = GetCurrentState();
        }

        private void RequestCreateUndoState()
        {
            if (_currentUndoState == null)
            {
                return;
            }

            _runtimeGraphEditor.UndoController.CreateUndoState(this);
        }

        /// <inheritdoc/>
        public void Undo(string context)
        {
            ApplyContext(context);
        }

        /// <inheritdoc/>
        public void Redo(string context)
        {
            UnsubscribeListeners();

            Undo(context);

            SubscribeListeners();
        }
    
        /// <inheritdoc/>
        public string GetUndoContext()
        {
            return JsonConvert.SerializeObject(_currentUndoState);
        }

        /// <inheritdoc/>
        public string GetCurrentContext()
        {
            return JsonConvert.SerializeObject(GetCurrentState());
        }

        private void ApplyContext(string context)
        {
            _currentUndoState = null;

            UndoState undoState = JsonConvert.DeserializeObject<UndoState>(context);

            _moduleDropdown.value = undoState.moduleDropdown;
            _triggerDropdown.value = undoState.triggerDropdown;

            _firstVariableModuleDropdown.value = undoState.firstVariableModuleDropdown;
            _firstVariableDropdown.value = undoState.firstVariableDropdown;

            _secondVariableModuleDropdown.value = undoState.secondVariableModuleDropdown;
            _secondVariableDropdown.value = undoState.secondVariableDropdown;
            _secondVariableInputField.text = undoState.secondVariableInputField;

            _conditionSymbolsDropdown.value = undoState.conditionSymbolsDropdown;
            _conditionSettingsToggle.isOn = undoState.conditionSettingsToggle;
            _conditionSettingsAnimator.SetBool("IsOn", _conditionSettingsToggle.isOn);

            foreach (ActionEditor action in _actions)
            {
                Destroy(action.gameObject);
            }

            _actions.Clear();

            foreach (Tuple<string, List<Tuple<string, string>>> action in undoState.currentActions)
            {
                AddAction(action.Item1, action.Item2);
            }

            SaveCurrentUndoState();
        }

        private UndoState GetCurrentState()
        {
            UndoState state = new();

            state.moduleDropdown = _moduleDropdown.value;
            state.triggerDropdown = _triggerDropdown.value;

            state.firstVariableModuleDropdown = _firstVariableModuleDropdown.value;
            state.firstVariableDropdown = _firstVariableDropdown.value;

            state.secondVariableModuleDropdown = _secondVariableModuleDropdown.value;
            state.secondVariableDropdown = _secondVariableDropdown.value;
            state.secondVariableInputField = _secondVariableInputField.text;

            state.conditionSymbolsDropdown = _conditionSymbolsDropdown.value;
            state.conditionSettingsToggle = _conditionSettingsToggle.isOn;

            List<Tuple<string, List<Tuple<string, string>>>> currentActions = new();

            foreach (ActionEditor action in _actions)
            {
                string moduleID = action.ModuleDropdown.captionText.text;
                string actionID = action.TriggerDropdown.captionText.text;
                string parameter = null;

                List<Tuple<string, string>> parameters = new();

                if (_actionModules.TryGetValue(moduleID, out _))
                {
                    foreach (ParameterContainer parameterContainer in action.ParameterContainers)
                    {
                        switch (parameterContainer.ActionParameter.Type)
                        {
                            case "bool":
                                parameter = parameterContainer.ParameterToggle.isOn.ToString();
                                break;
                            case "string":
                                parameter = parameterContainer.ParameterValueInputField.text;
                                break;
                            case "int":
                                parameter = string.IsNullOrEmpty(parameterContainer.ParameterValueInputField.text) ? "0" : parameterContainer.ParameterValueInputField.text;
                                break;
                            case "float":
                                parameter = string.IsNullOrEmpty(parameterContainer.ParameterValueInputField.text) ? "0" : parameterContainer.ParameterValueInputField.text;
                                break;
                            case "enum":
                                parameter = parameterContainer.ParameterValueDropdown.captionText.text;
                                break;
                        }

                        parameters.Add(new Tuple<string, string>(parameterContainer.ActionParameter.Name, parameter));
                    }

                    currentActions.Add(new Tuple<string, List<Tuple<string, string>>>($"{moduleID}.{actionID}", parameters));
                }
            }

            state.currentActions = currentActions;

            return state;
        }

        /// <summary>
        /// Состояние редактируемого окна, используется для восстановления истории состояний 
        /// </summary>
        class UndoState
        {
            /// <summary>
            /// Индекс модулю в выпадающем списке
            /// </summary>
            public int moduleDropdown { get; set; }
            /// <summary>
            /// Индекс события в выпадающем списке
            /// </summary>
            public int triggerDropdown { get; set; }
            /// <summary>
            /// Индекс первой переменной модуля в выпадающем списке
            /// </summary>
            public int firstVariableModuleDropdown { get; set; }
            /// <summary>
            /// Индекс первой переменной в выпадающем списке
            /// </summary>
            public int firstVariableDropdown { get; set; }
            /// <summary>
            /// Значение поля ввода первой переменной
            /// </summary>
            public string firstVariableInputField { get; set; }
            /// <summary>
            /// Индекс второй переменной модуля в выпадающем списке
            /// </summary>
            public int secondVariableModuleDropdown { get; set; }
            /// <summary>
            /// Индекс второй переменной в выпадающем списке
            /// </summary>
            public int secondVariableDropdown { get; set; }
            /// <summary>
            /// Значение поля ввода второй переменной
            /// </summary>
            public string secondVariableInputField { get; set; }
            /// <summary>
            /// Индекс символа условия в выпадающем списке
            /// </summary>
            public int conditionSymbolsDropdown { get; set; }
            /// <summary>
            /// Переключатель настроек условия
            /// </summary>
            public bool conditionSettingsToggle { get; set; }
            /// <summary>
            /// Список текущих действия с параметрами
            /// </summary>
            public List<Tuple<string, List<Tuple<string, string>>>> currentActions { get; set; }
        }

    #endregion
    }
}
