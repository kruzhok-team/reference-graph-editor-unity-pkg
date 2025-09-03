using System;
using System.Collections.Generic;
using System.Linq;
using Talent.GraphEditor.Core;
using Talent.GraphEditor.Unity.Runtime.ContextMenu;
using Talent.Graphs;
using TMPro;
using UI.Focusing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Представление узла
    /// </summary>
    public class NodeView : MonoBehaviour, INodeView
    {
        [SerializeField] private SimpleSelectable _selectable;

        [Space]

        [SerializeField] private TMP_InputField _nameTMP;
        [SerializeField] private InteractArea _bodyArea;
        [SerializeField] private RectTransform _childsContainer;
        [SerializeField] private Transform _triggersContainer;
        [SerializeField] private GameObject _nameEditButton;
        [SerializeField] private EdgeCreationButton[] _connectionButtons;

        [Header("ContextMenu")]
        [SerializeField] private NodeViewContextMenu _nodeViewContextMenu;
        [SerializeField] private ActionContextMenu _actionContextMenu;

        private RuntimeGraphEditor _runtimeGraphEditor;
        private Transform _defaultParent;
        private List<EdgeView> _edgeViews = new();

        /// <summary>
        /// Список переходов из узла
        /// </summary>
        public IEnumerable<EdgeView> EdgeViews => _edgeViews;
        /// <summary>
        /// Контейнер дочерних элементов
        /// </summary>
        public RectTransform ChildsContainer => _childsContainer;
        /// <summary>
        /// Контейнер событий
        /// </summary>
        public Transform TriggersContainer => _triggersContainer;
        /// <summary>
        /// Вершина
        /// </summary>
        public string Vertex { get; private set; }
        /// <summary>
        /// Визуальные данные узла
        /// </summary>
        public NodeVisualData VisualData { get; private set; }

        /// <summary>
        /// Уникальный идентификатор узла
        /// </summary>
        public string ID { get; set; }
    
        /// <summary>
        /// Событие на запрос того, чтобы сделать узел родительским
        /// </summary>
        public event Action<NodeView> NodeParentRequested;
        /// <summary>
        /// Событие на отмену запрос того, чтобы сделать узел родительским
        /// </summary>
        public event Action<NodeView> NodeParentCanceled;

        /// <summary>
        /// Родительский граф
        /// </summary>
        public GraphView ParentGraph { get; private set; }
        /// <summary>
        /// Имеет ли узел родителя
        /// </summary>
        public bool HasParent { get; private set; }

        private void OnEnable()
        {
            _defaultParent = transform.parent;

            _bodyArea.BeginDrag += OnBeginDragElement;
            _bodyArea.Drag += OnDragElement;
        }

        private void OnDisable()
        {
            _bodyArea.BeginDrag -= OnBeginDragElement;
            _bodyArea.Drag -= OnDragElement;
        }

        /// <summary>
        /// Инициализирует <see cref="NodeView"/>
        /// </summary>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        /// <param name="vertex">Вершина</param>
        /// <param name="visualData">Визуальные данные узла</param>
        public void Init(RuntimeGraphEditor runtimeGraphEditor, string vertex, NodeVisualData visualData)
        {
            _runtimeGraphEditor = runtimeGraphEditor;
            Vertex = vertex;
            VisualData = visualData;

            if (_nameTMP != null)
            {
                _nameTMP.text = VisualData.Name;
            }

            transform.localPosition = VisualData.Position;

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);

            foreach (EdgeCreationButton connectionButton in _connectionButtons)
            {
                connectionButton.Init(runtimeGraphEditor);
            }
        }

        /// <summary>
        /// Дублирует узел
        /// </summary>
        public void Duplicate()
        {
            _runtimeGraphEditor.DuplicateNodeView(this, true);
        }

        /// <summary>
        /// Устанавливает родительский граф для узла
        /// </summary>
        /// <param name="parent">Родительский граф</param>
        /// <param name="layoutAutomatically"></param>
        public void SetParent(IGraphView parent, bool layoutAutomatically)
        {
            HasParent = parent != null;

            if (parent == null)
            {
                transform.SetParent(_defaultParent);

                foreach (EdgeView edge in _edgeViews)
                {
                    edge.UpdateSourceParent();
                }

                return;
            }

            GraphView parentGraphView = parent as GraphView;

            transform.SetParent(parentGraphView.transform, false);
            ParentGraph = parentGraphView;

            if (layoutAutomatically)
            {
                GraphLayoutGroup graphLayoutGroup = parentGraphView.GetComponent<GraphLayoutGroup>();

                if (graphLayoutGroup.GetRectChildrenCount() >= 1)
                {
                    graphLayoutGroup.GetGraphCorners(out float left, out float top, out float right, out float bottom);

                    RectTransform nodeRectTransform = transform as RectTransform;

                    LayoutRebuilder.ForceRebuildLayoutImmediate(nodeRectTransform);

                    float xPos = right + nodeRectTransform.sizeDelta.x / 2;
                    float yPos = (top + bottom) / 2;

                    transform.position = (Vector2)graphLayoutGroup.transform.position + new Vector2(xPos, yPos);
                }
                else
                {
                    transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                transform.localPosition = VisualData.Position;
            }

            VisualData.Position = transform.localPosition;

            foreach (EdgeView edge in _edgeViews)
            {
                edge.UpdateSourceParent();
            }
        }

        /// <summary>
        /// Открывает контекстное меню редактора перехода
        /// </summary>
        /// <param name="eventView">Представление перехода в узле</param>
        public void OpenEventContextMenu(NodeEventView eventView)
        {
            _actionContextMenu.Init(eventView);
        }

        /// <summary>
        /// Открывает контекстное меню редактора ноды
        /// </summary>
        public void OpenNodeContextMenu()
        {
            _runtimeGraphEditor.CancelEditingEdge(this);

            _nodeViewContextMenu.Init();
        }

        /// <summary>
        /// Устанавливает имя узла
        /// </summary>
        /// <param name="newName">Новое имя</param>
        public void SetName(string newName)
        {
            if (newName == VisualData.Name)
            {
                return;
            }

            VisualData.Name = newName;
            _nameTMP.text = newName;
        }

        /// <summary>
        /// Добавляет ребро
        /// </summary>
        /// <param name="edgeView">Представление ребра</param>
        public void AddEdge(EdgeView edgeView)
        {
            _edgeViews.Add(edgeView);
        }

        /// <summary>
        /// Удаляет ребро
        /// </summary>
        /// <param name="edgeView">Представление ребра</param>
        public void RemoveEdge(EdgeView edgeView)
        {
            _nodeViewContextMenu.RemoveEdge(edgeView);

            _edgeViews.Remove(edgeView);
        }

        /// <summary>
        /// Добавляет переход
        /// </summary>
        public void AddEvent()
        {
            _runtimeGraphEditor.OpenEventEditor(this);
        }

        /// <summary>
        /// Добавляет дочерний узел
        /// </summary>
        public void AddChildNode()
        {
            _runtimeGraphEditor.AddChildNode(this);
        }

        /// <summary>
        /// Делает узел не родительским
        /// </summary>
        public void Unparent()
        {
            _runtimeGraphEditor.UnParentNode(this);
        }

        /// <summary>
        /// Удаляет представление узла
        /// </summary>
        public void Delete(bool createUndo = true)
        {
            if (createUndo)
            {
                _runtimeGraphEditor.RequestCreateUndoState();
            }

            _runtimeGraphEditor?.GraphEditor.RemoveNode(this);
        }

        public void Select()
        {
            UIFocusingSystem.Instance.Select(_selectable);
        }

        private void OnCancelHotkeyPressed()
        {
            NodeParentCanceled?.Invoke(this);
        }

        /// <summary>
        /// Делает представление узла родительским
        /// </summary>
        public void ConnectParent()
        {
            NodeParentRequested?.Invoke(this);
        }

        private void OnBeginDragElement(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _runtimeGraphEditor.RequestCreateUndoState();
        }

        private void OnDragElement(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            transform.position += (Vector3)eventData.delta;
            VisualData.Position = transform.localPosition;

            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.transform as RectTransform);
        }

        /// <summary>
        /// Открывает редактор имени узла
        /// </summary>
        public void StartEditName()
        {
            _runtimeGraphEditor.OpenNodeNamePopUp(ID, true, VisualData.Name);
        }

        /// <summary>
        /// Является ли этот потомком другого узла
        /// </summary>
        /// <param name="otherNodeView">Другой узел</param>
        /// <returns>true, если является потомком, иначе false</returns>
        public bool IsDescendant(NodeView otherNodeView)
        {
            if (this == otherNodeView)
            {
                return false;
            }

            NodeView nodeView = ParentGraph?.ParentNode;
            while (nodeView != null)
            {
                if (nodeView == otherNodeView)
                {
                    return true;
                }

                nodeView = nodeView.ParentGraph?.ParentNode;
            }

            return false;
        }

        /// <summary>
        /// Устанавливает состояние видимости обводки узла
        /// </summary>
        /// <param name="isVisible">Нужно ли включить обводку</param>
        public void SetOutlineVisibility(bool isVisible)
        {

        }

        /// <summary>
        /// Является ли имя для узла уникальным
        /// </summary>
        /// <param name="desiredName">Желаемое имя</param>
        /// <returns>true, если имя является уникальным, иначе false</returns>
        public bool IsUniqueNodeName(string desiredName)
        {
            List<NodeView> childNodes = new List<NodeView>();
            Transform nodeParent = transform.parent;

            foreach (Transform childNode in nodeParent)
            {
                if (childNode.TryGetComponent(out NodeView childNodeView))
                {
                    childNodes.Add(childNodeView);
                }
            }

            return childNodes.All(nodeView => nodeView.VisualData.Name != desiredName || nodeView == this);
        }

        public void OnClicked()
        {
            _runtimeGraphEditor.OnClicked(this);
        }
    }
}
