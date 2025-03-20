using System.Collections.Generic;
using Talent.GraphEditor.Core;
using Talent.Graphs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    public class EdgeView : MonoBehaviour, IEdgeView, IElementSelectable, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// Начальное представление узла
        /// </summary>
        public NodeView SourceView { get; private set; }
        /// <summary>
        /// Конечное представление узла
        /// </summary>
        public NodeView TargetView { get; private set; }
        /// <summary>
        /// Возвращает true, если представление узла является предварительным, иначе false
        /// </summary>
        public bool IsPreview { get; private set; }
        /// <summary>
        /// Возвращает true, если представление ребра находится в режиме перетаскивания, иначе false
        /// </summary>
        public bool IsDraggableMode { get; set; }

        [SerializeField] private EdgeEditingButton _changeSourceConnection;
        [SerializeField] private EdgeEditingButton _changeTargetConnection;
        [SerializeField] private GameObject _content;
        [SerializeField] private CanvasGroup _centerBlockCanvasGroup;
        [SerializeField] private TextMeshProUGUI _triggerTMP;
        [SerializeField] private TextMeshProUGUI _conditionTMP;
        [SerializeField] private GameObject _conditionsAndActionsContainer;
        [SerializeField] private GameObject _conditionContainer;
        [SerializeField] private Transform _actionsContainer;
        [SerializeField] private InteractArea _bodyArea;
        [SerializeField] private EdgeLine _line;
        [SerializeField] private RectTransform _outLine;
        [SerializeField] private GameObject _contextSelection;

        [Header("Colors")]
        [SerializeField] private Color _edgeUnselectedColor = Color.white;
        [SerializeField] private Color _edgeSelectedColor;

        [Header("Icons")] [SerializeField] private Transform _iconsContainer;

        [SerializeField] private Transform _conditionIconsContainer;
        [SerializeField] private Icon _singleIconPrefab;
        [SerializeField] private Icon _doubleIconPrefab;


        [Header("Hotkeys")] [SerializeField] private KeyCode _cancelKeyCode = KeyCode.Escape;

        [SerializeField] private KeyCode _deleteKeyCode = KeyCode.Delete;

        private RuntimeGraphEditor _runtimeGraphEditor;

        private IconSpriteProviderAsset _iconProvider;
    
        private GameObject _currentTriggerIcon;
    
        private NodeView _otherNode;


        private List<GameObject> _currentIcons = new();
        /// <summary>
        /// Возвращает true, если ребро является видимым, иначе false
        /// </summary>
        public bool IsCenterBlockVisible => _centerBlockCanvasGroup.alpha != 0;
    
        /// <summary>
        /// Визуальные данные представления ребра
        /// </summary>
        public EdgeVisualData VisualData { get; private set; } = new();
        /// <summary>
        /// Уникальный идентификатор представления узла
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// Уникальный идентификатор события для перехода
        /// </summary>
        public string TriggerID { get; private set; }
        /// <summary>
        /// Уникальный идентификатор условия для перехода
        /// </summary>
        public string Condition { get; private set; }

        /// <summary>
        /// Контейнер действий представления ребра
        /// </summary>
        public Transform ActionsContainer => _actionsContainer;
        
        /// <summary>
        /// Линия, проходящая через ребро
        /// </summary>
        public EdgeLine Line => _line;

        /// <inheritdoc/>
        public GameObject SelectedObject => _bodyArea.gameObject;

        private SelectionContextSource _selectionContextSource;

        private LineClickListener _lineClickListener;
        /// <inheritdoc/>
        public ISelectionContextSource SelectionContextSource => _selectionContextSource;
        
        private void Awake()
        {
            _selectionContextSource = new SelectionContextSource();
            _selectionContextSource.AddHotkeyAction(new HotkeyAction(OnCancelHotkeyPressed, _cancelKeyCode));
            _selectionContextSource.AddHotkeyAction(new HotkeyAction(Delete, _deleteKeyCode));
            _selectionContextSource.AddHotkeyAction(new HotkeyAction(Duplicate,  KeyCode.LeftControl, KeyCode.D));
        }

        private void OnEnable()
        {
            _bodyArea.RightClick += OnPointerUp;
            _bodyArea.BeginDrag += OnBeginDragElement;
            _bodyArea.Drag += OnDrag;
            _bodyArea.DoubleClick += OnDoubleClick;

            SetSelection(false, false);
        }

        private void OnDisable()
        {
            _bodyArea.RightClick -= OnPointerUp;
            _bodyArea.BeginDrag -= OnBeginDragElement;
            _bodyArea.Drag -= OnDrag;
            _bodyArea.DoubleClick -= OnDoubleClick;
        }

        private void OnDoubleClick(PointerEventData eventData)
        {
            if (IsPreview)
            {
                return;
            }

            if (_runtimeGraphEditor.ElementSelectionProvider.CurrentSelectedElement != null && SelectedObject ==
                _runtimeGraphEditor.ElementSelectionProvider.CurrentSelectedElement.SelectedObject)
            {
                OpenEdgeEditor();
            }
        }

        private void OnBeginDragElement(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _runtimeGraphEditor.RequestCreateUndoState();
        }

        /// <summary>
        /// Инициализирует предварительно созданный <see cref="EdgeView"/>  
        /// </summary>
        /// <param name="runtimeGraphEditor">Редактор графоа</param>
        /// <param name="lineClickListener">Слушатель кликов на линии</param>
        public void Init(RuntimeGraphEditor runtimeGraphEditor, LineClickListener lineClickListener)
        {
            _runtimeGraphEditor = runtimeGraphEditor;
            _line.Init(lineClickListener.transform);
            SetSelection(true, false);
            IsPreview = true;
            _changeSourceConnection.Init(_runtimeGraphEditor);
            _changeTargetConnection.Init(_runtimeGraphEditor);
            _changeSourceConnection.Deactivate();
            _changeTargetConnection.Deactivate();
        }

        /// <summary>
        /// Инициализирует <see cref="EdgeView"/>
        /// </summary>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        /// <param name="visualData">Визуальные данные представления ребра</param>
        /// <param name="sourceView">Стартовое представление узла</param>
        /// <param name="targetView">Конечное представление узла</param>
        /// <param name="triggerID">Уникальный идентификатор события для перехода</param>
        /// <param name="condition">Уникальный идентификатор условия для перехода</param>
        /// <param name="iconProvider">Объект, предоставляющий доступ к иконкам</param>
        /// <param name="lineClickListener">Слушатель кликов на линии</param>
        public void Init(RuntimeGraphEditor runtimeGraphEditor, EdgeVisualData visualData,
            NodeView sourceView, NodeView targetView, string triggerID, string condition,
            IconSpriteProviderAsset iconProvider, LineClickListener lineClickListener)
        {
            _runtimeGraphEditor = runtimeGraphEditor;
            VisualData = visualData;
            TriggerID = triggerID;
            Condition = condition;
            _iconProvider = iconProvider;
            _lineClickListener = lineClickListener;
            transform.localPosition = VisualData.Position;
            SourceView = sourceView;
            TargetView = targetView;
            _line.Init(lineClickListener.transform);
            _lineClickListener.AddLine(_line, this);
            SourceView.AddEdge(this);
            IsPreview = false;
            _changeSourceConnection.Init(_runtimeGraphEditor);
            _changeTargetConnection.Init(_runtimeGraphEditor);

            if (sourceView.Vertex == NodeData.Vertex_Initial)
            {
                _content.SetActive(false);
                _outLine.localScale = Vector3.zero;

                if (!TryGetComponent(out LayoutElement element))
                {
                    element = gameObject.AddComponent<LayoutElement>();

                    element.ignoreLayout = true;
                }
            }
            else
            {
                if (_triggerTMP != null)
                {
                    _triggerTMP.text = triggerID;
                }

                if (_conditionTMP != null)
                {
                    _conditionTMP.text = condition;
                }

                UpdateTriggerIcon(triggerID);

                _conditionContainer.SetActive(!string.IsNullOrEmpty(condition));

                if (!string.IsNullOrEmpty(condition))
                {
                    UpdateConditionIcons(condition.Split(" "));
                }

                RefreshConditionAndActionsContainer();
            }
            
            RecalculateParent();
        }
    
        private void Update()
        {
            if (SourceView == null || TargetView == null)
            {
                _otherNode?.SetOutlineVisibility(false);
                NodeView otherNode = FindOtherNode();
                if (otherNode != null)
                {
                    if (SourceView == null)
                    {
                        _line.Draw(otherNode, _outLine, TargetView);
                        Transform parent = GetCommonDeepestParent(otherNode, TargetView);
                        transform.SetParent(parent);
                    }
                    else
                    {
                        _line.Draw(SourceView, _outLine, otherNode);
                        Transform parent = GetCommonDeepestParent(SourceView, otherNode);
                        transform.SetParent(parent);
                    }
                
                    otherNode.SetOutlineVisibility(true);
                    _otherNode = otherNode;
                }
                else
                {
                    if (TargetView == null)
                    {
                        _line.Draw(SourceView, _outLine, Input.mousePosition);
                    }
                    else if (SourceView == null)
                    {
                        _line.Draw(Input.mousePosition, _outLine, TargetView);
                    }

                    transform.SetParent(_runtimeGraphEditor.GraphElementViewsContainer.transform);
                }
            }
            else
            {
                _line.Draw(SourceView, _outLine, TargetView);
            }

            _changeSourceConnection.transform.position =
                _line.transform.TransformPoint(_line.StartPoint);
            _changeTargetConnection.transform.position = _line.transform.TransformPoint(_line.EndPoint);
        }

        /// <summary>
        /// Соединяет ребро с начальным представлением узла
        /// </summary>
        /// <param name="sourceView">Начальное представление узла</param>
        public void ConnectSourceView(NodeView sourceView)
        {
            SourceView = sourceView;
            SourceView.AddEdge(this);
        }

        /// <summary>
        /// Соединяет ребро с конечным представлением узла
        /// </summary>
        /// <param name="targetView">Конечное преставление узла</param>
        public void ConnectTargetView(NodeView targetView)
        {
            TargetView = targetView;
            targetView.SetOutlineVisibility(false);
        }

        /// <summary>
        /// Ищет представление узла отличное от начального и конечного
        /// </summary>
        /// <returns>Возвращает найденное представление узла, иначе null</returns>
        public NodeView FindOtherNode()
        {
            NodeView currentNode = SourceView == null ? TargetView : SourceView;
            Vector3 mousePosition = Input.mousePosition;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                NodeView tempNode = result.gameObject.GetComponentInParent<NodeView>();

                if (tempNode != null && tempNode.Vertex != NodeData.Vertex_Initial && tempNode != currentNode)
                {
                    return tempNode;
                }
            }

            return null;
        }

        public void RecalculateParent()
        {
            transform.SetParent(GetCommonDeepestParent(SourceView, TargetView), false);
        }
    
        private Transform GetCommonDeepestParent(NodeView sourceView, NodeView targetView)
        {
            if (sourceView.IsDescendant(targetView))
            {
                return targetView.ChildsContainer.GetComponentInChildren<GraphView>().transform;
            }

            if (targetView.IsDescendant(sourceView))
            {
                return sourceView.ChildsContainer.GetComponentInChildren<GraphView>().transform;
            }

            GraphView source = sourceView.ParentGraph;
            GraphView target = targetView.ParentGraph;

            Transform commonParent = _runtimeGraphEditor.GraphElementViewsContainer.transform;

            if (source == null || target == null)
            {
                return commonParent;
            }

            bool isFound = false;
            while (source != null)
            {
                while (target != null)
                {
                    if (source == target)
                    {
                        commonParent = source.transform;
                        isFound = true;
                        break;
                    }

                    target = target.ParentNode?.ParentGraph;
                }

                if (isFound)
                {
                    break;
                }

                source = source.ParentNode?.ParentGraph;
                target = targetView.ParentGraph;
            }

            return commonParent;
        }

        /// <summary>
        /// Обновляет контейнер условия и действий
        /// </summary>
        public void RefreshConditionAndActionsContainer()
        {
            _conditionsAndActionsContainer.SetActive(false);
            _conditionsAndActionsContainer.SetActive(_conditionContainer.activeSelf ||
                _actionsContainer.gameObject.activeSelf);
        }

        /// <summary>
        /// Удаляет преставление условия
        /// </summary>
        public void DeleteCondition()
        {
            _runtimeGraphEditor.GraphEditor.ChangeEdgeCondition(this, "");
        }

        /// <summary>
        /// Удаляет представление ребра
        /// </summary>
        public void Delete()
        {
            _runtimeGraphEditor.RequestCreateUndoState();

            if (IsPreview)
            {
                _runtimeGraphEditor.DestroyElementView(this);
                return;
            }

            _runtimeGraphEditor.GraphEditor.RemoveEdge(this);
        }

        /// <summary>
        /// Устанавливает событие для перехода
        /// </summary>
        /// <param name="newTriggerID">Событие</param>
        public void SetTrigger(string newTriggerID)
        {
            TriggerID = newTriggerID;

            if (_triggerTMP != null)
            {
                _triggerTMP.text = newTriggerID;
            }

            UpdateTriggerIcon(newTriggerID);
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

            RefreshConditionAndActionsContainer();
        }

        /// <summary>
        /// Изменяет начальный узел представления ребра
        /// </summary>
        public void ChangeEdgeSourceNode()
        {
            SourceView.RemoveEdge(this);
            SourceView = null;
            _changeTargetConnection.Deactivate();
            _changeSourceConnection.Deactivate();
        

            _runtimeGraphEditor.ElementSelectionProvider.Select(this);
            _runtimeGraphEditor.EditingEdge = this;
            _runtimeGraphEditor.RequestCreateUndoState();
        }

        /// <summary>
        /// Изменяет конечный узел представления ребра
        /// </summary>
        public void ChangeEdgeTargetNode()
        {
            TargetView = null;
            _changeTargetConnection.Deactivate();
            _changeSourceConnection.Deactivate();

            _runtimeGraphEditor.ElementSelectionProvider.Select(this);
            _runtimeGraphEditor.EditingEdge = this;
            _runtimeGraphEditor.RequestCreateUndoState();
        }

        private void OnCancelHotkeyPressed()
        {
            if (_runtimeGraphEditor.EditingEdge != this)
            {
                return;
            }

            if (IsPreview)
            {
                if (!_runtimeGraphEditor.UndoController.IsLocked)
                {
                    _runtimeGraphEditor.DestroyElementView(this);
                }
            }
            else
            {
                _runtimeGraphEditor.UndoController.Undo();
            }
        }

        private void UpdateTriggerIcon(string id)
        {
            if (_currentTriggerIcon != null)
            {
                GameObject.Destroy(_currentTriggerIcon);
            }

            _currentTriggerIcon = _iconProvider.GetIconInstance(id);
            _currentTriggerIcon.transform.SetParent(_iconsContainer, false);
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
        /// Обновляет родителя начального узла
        /// </summary>
        public void UpdateSourceParent()
        {
            transform.SetParent(SourceView.transform.parent);
        }

        /// <summary>
        /// Открывает редактор узла
        /// </summary>
        public void OpenEdgeEditor()
        {
            _runtimeGraphEditor.OpenEdgeEditor(this);
        }
        
        public void Duplicate()
        {
            _runtimeGraphEditor.DuplicateEdgeView(this);
        }

        private void OnDrag(PointerEventData eventData)
        {
            if (IsPreview)
            {
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            transform.position += (Vector3)eventData.delta;
            VisualData.Position = transform.localPosition;

            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.transform as RectTransform);
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии указателя на элемент  
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnPointerDown(PointerEventData eventData)
        {
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при отпускании указателя с элемента
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsCenterBlockVisible)
            {
                return;
            }

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
                Select(true);
            }
        }

        /// <summary>
        /// Выбирает представление ребра
        /// </summary>
        /// <param name="isContextSelection">Выбрано ли ребро с помощью контекстное меню</param>
        public void Select(bool isContextSelection)
        {
            if (_runtimeGraphEditor.EditingEdge != null)
            {
                return;
            }

            transform.SetAsLastSibling();
            _runtimeGraphEditor.ElementSelectionProvider.Select(isContextSelection ? null : this);
            SetSelection(true, isContextSelection);
        }

        /// <summary>
        /// Отменяет выбор представления ребра
        /// </summary>
        public void Unselect()
        {
            _runtimeGraphEditor.ElementSelectionProvider.Unselect(this);

            if (this == null)
            {
                return;
            }

            SetSelection(false, false);
        }

        private void SetSelection(bool isSelected, bool isContextSelection)
        {
            _bodyArea.enabled = isSelected && !isContextSelection;
            _bodyArea.gameObject.SetActive(isSelected);

            _contextSelection.SetActive(isSelected && isContextSelection);

            if (SourceView?.Vertex != NodeData.Vertex_Initial)
            {
                if (isSelected)
                {
                    _changeSourceConnection.Activate();
                }
                else
                {
                    _changeSourceConnection.Deactivate();
                }
            }

            if (isSelected)
            {
                _changeTargetConnection.Activate();
            }
            else
            {
                _changeTargetConnection.Deactivate();
            }
        
            _line.SetColor(isSelected ? _edgeSelectedColor : _edgeUnselectedColor);
        }

        private void OnDestroy()
        {
            if (!IsPreview)
            {
                _lineClickListener.RemoveLine(_line);
            }

            Destroy(_line.gameObject);
        }
    }
}
