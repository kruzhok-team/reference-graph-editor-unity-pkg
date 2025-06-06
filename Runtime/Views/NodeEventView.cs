using System;
using System.Collections.Generic;
using Talent.GraphEditor.Core;
using TMPro;
using UI.Focusing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Представление перехода в узле
    /// </summary>
    public class NodeEventView : MonoBehaviour, INodeEventView
    {
        [SerializeField] private SimpleContextLayer _context;

        [SerializeField] private TextMeshProUGUI _triggerIDTMP;
        [SerializeField] private GridLayoutGroup _gridLayoutGroup;
        [SerializeField] private Transform _actionsContainer;
        [SerializeField] private TextMeshProUGUI _conditionTMP;
        [SerializeField] private Image _outlineImage;
        [SerializeField] private GameObject _conditionContainer;
        [SerializeField] private InteractArea _bodyArea;
        [Header("Icons")]
        [SerializeField] private Transform _iconsContainer;
        [SerializeField] private Transform _conditionIconsContainer;
        [SerializeField] private Icon _singleIconPrefab;
        [SerializeField] private Icon _doubleIconPrefab;
        [Header("Hotkeys")]
        [SerializeField] private KeyCode _deleteKeyCode = KeyCode.Delete;
        [Header("Settings")]
        [SerializeField] private int _maxColumnsInActionContainer = 3;
        
        private readonly List<GameObject> _currentIcons = new List<GameObject>();
        private readonly List<NodeActionView> _actionViews = new List<NodeActionView>();
        
        private string _triggerID;
        private NodeView _nodeView;
        private RuntimeGraphEditor _runtimeGraphEditor;
        private IconSpriteProviderAsset _iconProvider;
        private GameObject _currentIcon;
        
        /// <summary>
        /// Родительский узел
        /// </summary>
        public NodeView NodeView => _nodeView;
        /// <summary>
        /// Идентификатор события
        /// </summary>
        public string TriggerID => _triggerID;
        /// <summary>
        /// Переход
        /// </summary>
        public Talent.Graphs.Event Event { get; private set; }
        /// <summary>
        /// Условие
        /// </summary>
        public string Condition { get; private set; }
        
        /// <summary>
        /// Перечисление представлений поведений
        /// </summary>
        public IEnumerable<NodeActionView> ActionViews => _actionViews;

        private void OnEnable()
        {
            _bodyArea.DoubleClick += OnDoubleClick;
        }

        private void OnDisable()
        {
            _bodyArea.DoubleClick -= OnDoubleClick;
        }

        /// <summary>
        /// Инициализирует <see cref="NodeEventView"/>
        /// </summary>
        /// <param name="triggerID">Идентификатор события</param>
        /// <param name="event">Переход</param>
        /// <param name="nodeView">Представление узла</param>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        /// <param name="iconProvider">Объект, предоставляющий доступ к спрайтам</param>
        public void Init(string triggerID, Talent.Graphs.Event @event, NodeView nodeView,
            RuntimeGraphEditor runtimeGraphEditor, IconSpriteProviderAsset iconProvider)
        {
            Event = @event;
            _triggerID = triggerID;
            _nodeView = nodeView;
            _runtimeGraphEditor = runtimeGraphEditor;
            _iconProvider = iconProvider;

            if (_triggerIDTMP != null)
            {
                _triggerIDTMP.text = triggerID;
            }

            UpdateIcons(triggerID);
        }
        
        /// <summary>
        /// Добавляет представление поведения в перехоже
        /// </summary>
        /// <param name="actionView">Представление поведения</param>
        public void AddActionView(NodeActionView actionView)
        {
            actionView.transform.SetParent(_actionsContainer, false);
            _actionViews.Add(actionView);
            _gridLayoutGroup.constraintCount = Mathf.Clamp(_actionViews.Count, 0, _maxColumnsInActionContainer);
            _actionsContainer.gameObject.SetActive(true);
        }

        /// <summary>
        /// Удаляет представление поведения из перехода
        /// </summary>
        /// <param name="actionView">Представление поведения</param>
        public void RemoveActionView(NodeActionView actionView)
        {
            _actionViews.Remove(actionView);
            _gridLayoutGroup.constraintCount = Mathf.Clamp(_actionViews.Count, 0, _maxColumnsInActionContainer);

            if (_actionViews.Count == 0)
            {
                _actionsContainer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Очищает список поведений
        /// </summary>
        public void ClearActionViews()
        {
            _actionViews.Clear();
            _gridLayoutGroup.constraintCount = 0;
            _actionsContainer.gameObject.SetActive(false);
        }

        private void OnDoubleClick(PointerEventData eventData)
        {
            OpenEventEditor();
        }

        private void UpdateIcons(string id)
        {
            if (_currentIcon != null)
            {
                Destroy(_currentIcon);
            }

            _currentIcon = _iconProvider.GetIconInstance(id, changeSeparatorColor: true);
            _currentIcon.transform.SetParent(_iconsContainer, false);

            if (_iconProvider.TryGetColor(id, out Color color))
            {
                _outlineImage.color = color;
            }
        }

        /// <summary>
        /// Открывает редактор перехода
        /// </summary>
        public void OpenEventEditor()
        {
            _runtimeGraphEditor.OpenEventEditor(_nodeView, this);
        }

        /// <summary>
        /// Удаляет представление перехода в узле
        /// </summary>
        public void Delete()
        {
            _runtimeGraphEditor.RequestCreateUndoState();
            _runtimeGraphEditor.GraphEditor.RemoveNodeEvent(_nodeView, this);
        }

        /// <summary>
        /// Удаляет условие перехода
        /// </summary>
        public void DeleteCondition()
        {
            _runtimeGraphEditor.GraphEditor.ChangeNodeEventCondition(_nodeView, this, "");
        }

        /// <summary>
        /// Устанавливает событие для перехода
        /// </summary>
        /// <param name="triggerID">Событие</param>
        public void SetTrigger(string triggerID)
        {
            _triggerID = triggerID;

            if (!string.IsNullOrEmpty(triggerID))
            {
                UpdateIcons(triggerID);
            }
        }

        /// <summary>
        /// Устанавливает условие для перехода
        /// </summary>
        /// <param name="condition">Условие</param>
        public void SetCondition(string condition)
        {
            Condition = condition;

            if (_conditionTMP != null)
            {
                _conditionTMP.text = condition;
            }

            _conditionContainer.SetActive(!string.IsNullOrEmpty(condition));

            if (!string.IsNullOrEmpty(condition))
            {
                UpdateConditionIcons(condition.Split(" "));
            }
        }

        public void OpenContextMenu()
        {
            _nodeView.OpenEventContextMenu(this);
        }

        private void UpdateConditionIcons(params string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _currentIcons.Count; i++)
            {
                Destroy(_currentIcons[i]);
            }

            _currentIcons.Clear();

            for (int i = 0; i < ids.Length; i++)
            {
                GameObject currentIcon = _iconProvider.GetIconInstance(ids[i], _singleIconPrefab, _doubleIconPrefab);
                currentIcon.transform.SetParent(_conditionIconsContainer, false);

                _currentIcons.Add(currentIcon);
            }
        }
    }
}
