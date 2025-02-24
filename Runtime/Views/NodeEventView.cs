using System.Collections.Generic;
using Talent.GraphEditor.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Представление перехода в узле
    /// </summary>
    public class NodeEventView : MonoBehaviour, INodeEventView, IElementSelectable
    {
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

        private string _triggerID;
        private NodeView _nodeView;
        private RuntimeGraphEditor _runtimeGraphEditor;
        private IconSpriteProviderAsset _iconProvider;
        private GameObject _currentIcon;
        private List<GameObject> _currentIcons = new();
        private int _actionCount;

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
        /// Контейнер поведений
        /// </summary>
        public Transform ActionsContainer => _actionsContainer;

        /// <inheritdoc/>
        public GameObject SelectedObject => gameObject;

        private SelectionContextSource _selectionContextSource;
        /// <inheritdoc/>
        public ISelectionContextSource SelectionContextSource => _selectionContextSource;

        private void Awake()
        {
            _selectionContextSource = new SelectionContextSource();
            _selectionContextSource.AddHotkeyAction(new(_deleteKeyCode, () => Delete()));
        }

        private void OnEnable()
        {
            _bodyArea.RightClick += OnPointerUp;
            _bodyArea.DoubleClick += OnDoubleClick;

            SetSelection(false, false);
        }

        private void OnDisable()
        {
            _bodyArea.RightClick -= OnPointerUp;
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
        /// Увеличивает счетчик количества поведений
        /// </summary>
        public void IncrementActionCount()
        {
            _actionCount++;
            _gridLayoutGroup.constraintCount = Mathf.Min(_actionCount, _maxColumnsInActionContainer);
        }

        /// <summary>
        /// Уменьшает счетчик количества поведений
        /// </summary>
        public void DecrementActionCount()
        {
            _actionCount--;
            _gridLayoutGroup.constraintCount = Mathf.Min(_actionCount, _maxColumnsInActionContainer);
        }

        private void OnDoubleClick(PointerEventData eventData)
        {
            OpenEventEditor();
        }

        private void UpdateIcons(string id)
        {
            if (_currentIcon != null)
            {
                GameObject.Destroy(_currentIcon);
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

        private void UpdateConditionIcons(params string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _currentIcons.Count; i++)
            {
                GameObject.Destroy(_currentIcons[i]);
            }

            _currentIcons.Clear();

            for (int i = 0; i < ids.Length; i++)
            {
                GameObject currentIcon = _iconProvider.GetIconInstance(ids[i], _singleIconPrefab, _doubleIconPrefab);
                currentIcon.transform.SetParent(_conditionIconsContainer, false);

                _currentIcons.Add(currentIcon);
            }
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии указателя на элемент  
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnPointerDown(PointerEventData eventData) { }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при отпускании указателя с элемента
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.dragging || eventData.delta.magnitude > 0)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Select(false);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                transform.SetAsLastSibling();
                Select(true);
            }
        }

        /// <summary>
        /// Выбирает представление перехода
        /// </summary>
        /// <param name="isContextSelection">Выбран ли переход с помощью контекстное меню</param>
        public void Select(bool isContextSelection)
        {
            _runtimeGraphEditor.ElementSelectionProvider.Select(isContextSelection ? null : this);

            SetSelection(true, isContextSelection);
        }

        /// <summary>
        /// Отменяет выбор представление перехода
        /// </summary>
        public void Unselect()
        {
            _runtimeGraphEditor.ElementSelectionProvider.Unselect(this);

            SetSelection(false, false);
        }

        private void SetSelection(bool isSelected, bool isContextSelection)
        {
            if (_bodyArea != null)
            {
                _bodyArea.gameObject.SetActive(isSelected);
            }

            if (isSelected && isContextSelection)
            {
                _nodeView.OpenEventContextMenu(this);
            }
        }
    }
}
