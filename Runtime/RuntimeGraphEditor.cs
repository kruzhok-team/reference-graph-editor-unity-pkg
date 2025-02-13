using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Talent.GraphEditor.Core;
using Talent.Graphs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Event = Talent.Graphs.Event;

namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Пример реализации визуального редактора графов
    /// </summary>
    public class RuntimeGraphEditor : MonoBehaviour, IGraphElementViewFactory, IUndoable
    {
        [SerializeField] private TMP_InputField _graphDocumentNameInput;
        [SerializeField] private EditingWindow _edgeEditorWindow;
        [SerializeField] private EditNodeNamePopUp _editNodeNamePopUp;
        [SerializeField] private IconSpriteProviderAsset _iconSpriteProviderAsset;
        [SerializeField] private PanZoom _zoomPan;
        [SerializeField] private UndoController _undoController;
    
        /// <summary>
        /// Список событий, поддерживаемых редактором
        /// </summary>
        public List<string> Triggers { get; private set; } = new List<string>();
        /// <summary>
        /// Список действий, поддерживаемых редактором
        /// </summary>
        public List<ActionData> Actions { get; private set; } = new List<ActionData>();
        /// <summary>
        /// Список действия, поддерживаемых редактором
        /// </summary>
        public List<string> Variables { get; private set; } = new List<string>();
        /// <summary>
        /// Текущий редактируемый документ
        /// </summary>
        public CyberiadaGraphDocument GraphDocument => GraphEditor.GraphDocument;
        /// <summary>
        /// Объект, дающий возможность выбирать активный элемент
        /// </summary>
        public IElementSelectionProvider ElementSelectionProvider { get; private set; }
        /// <summary>
        /// Объект, дающий возможность отменять предыдущее действие
        /// </summary>
        public UndoController UndoController => _undoController;
        /// <summary>
        /// Редактируемый узел
        /// </summary>
        public NodeView ParentingingNode { get; set; }
        /// <summary>
        /// Редактируемое ребро
        /// </summary>
        public EdgeView EditingEdge { get; set; }
        /// <summary>
        /// Ядро логики редактора графа
        /// </summary>
        public Core.GraphEditor GraphEditor { get; private set; }
    
        private CyberiadaGraphMLConverter _converter;
    
        private void Awake()
        {
            ElementSelectionProvider = new ElementSelectionProvider(this);
            _converter = new CyberiadaGraphMLConverter(Application.productName, Application.version);
        }

        void Start()
        {
            _edgeEditorWindow.gameObject.SetActive(false);
        }

        /// <summary>
        /// Устанавливает ассет для получения спрайтов
        /// </summary>
        /// <param name="iconSpriteProviderAsset">Ассет</param>
        public void SetIconProviderAsset(IconSpriteProviderAsset iconSpriteProviderAsset)
        {
            _iconSpriteProviderAsset = iconSpriteProviderAsset;
        }

        /// <summary>
        /// Открывает CyberiadaGraphML документ
        /// </summary>
        /// <param name="graphDocument">Документ</param>
        /// <param name="context">Источник контекста исполнения</param>
        public void OpenGraphDocument(CyberiadaGraphDocument graphDocument, IExecutionContextSource context)
        {
            GraphEditor ??= new Core.GraphEditor(this);
            GraphEditor.SetGraphDocument(graphDocument);
            Triggers = new List<string>() { "entry", "exit" };
            Triggers.AddRange(context.GetEvents().ToList());
            Actions = context.GetActions().ToList();
            Variables = context.GetVariables().ToList();

            _graphDocumentNameInput.text = graphDocument.Name;
        }

        /// <summary>
        /// Устанавливает CyberiadaGraphML документ как редактируемый
        /// </summary>
        /// <param name="graphDocument">Документ</param>
        public void SetGraphDocument(CyberiadaGraphDocument graphDocument)
        {
            GraphEditor.SetGraphDocument(graphDocument);

            LayoutRebuilder.ForceRebuildLayoutImmediate(GraphElementViewsContainer.transform as RectTransform);

            _graphDocumentNameInput.text = graphDocument.Name;
        }

        /// <summary>
        /// Устанавливает имя документу
        /// </summary>
        /// <param name="name">Новое имя</param>
        public void SetGraphDocumentName(string name)
        {
            GraphDocument.Name = name;
        }

        /// <summary>
        /// Создает новое представление узла в графе
        /// </summary>
        public void CreateNewNode()
        {
            RequestCreateUndoState();

            NodeView view = (NodeView)GraphEditor.CreateNewNode("New Node");
            if (EditingEdge == null)
            {
                view.Select(false);
            }

            OpenNodeNamePopUp(view.ID);
        }

        /// <summary>
        /// Добавляет дочерний узел в родительский узел
        /// </summary>
        /// <param name="parent">Родительский узел</param>
        public void AddChildNode(NodeView parent)
        {
            RequestCreateUndoState();

            INodeView child = GraphEditor.CreateNewNode("ChildNode");
            GraphEditor.SetParent(child, parent, true);
            OpenNodeNamePopUp(child.ID);
        }

        /// <summary>
        /// Делает узел не родительским
        /// </summary>
        /// <param name="nodeView">Представление узла</param>
        public void UnParentNode(NodeView nodeView)
        {
            RequestCreateUndoState();
            GraphEditor.SetParent(nodeView, null, false);

            if (nodeView.IsUniqueNodeName(nodeView.VisualData.Name))
            {
                Rebuild();
                ElementSelectionProvider.Select(nodeView.ID);
            }
            else
            {
                OpenNodeNamePopUp(nodeView.ID, nodeView.VisualData.Name, true);
            }
        }

        /// <summary>
        /// Открывает редактор переходов
        /// </summary>
        /// <param name="node">Представление узла</param>
        /// <param name="nodeEvent">Представление перехода</param>
        public void OpenEventEditor(NodeView node, NodeEventView nodeEvent = null)
        {
            _edgeEditorWindow.gameObject.SetActive(true);
            _edgeEditorWindow.Init(node, nodeEvent, _iconSpriteProviderAsset);

            _zoomPan.gameObject.SetActive(false);
        }

        /// <summary>
        /// Открывает редактор ребра
        /// </summary>
        /// <param name="edgeView">Представление ребра</param>
        public void OpenEdgeEditor(EdgeView edgeView)
        {
            _edgeEditorWindow.gameObject.SetActive(true);
            _edgeEditorWindow.Init(edgeView.SourceView, edgeView.TargetView, _iconSpriteProviderAsset, edgeView);

            _zoomPan.gameObject.SetActive(false);
        }

        /// <summary>
        /// Сохраняет переход в узле
        /// </summary>
        /// <param name="node">Представление узла</param>
        /// <param name="trigger">Событие</param>
        /// <param name="condition">Условие</param>
        /// <param name="actions">Список действий и параметров</param>
        /// <param name="nodeEvent">Представление перехода в узле</param>
        public void SaveNodeEvent(NodeView node, string trigger, string condition,
            List<Tuple<string, List<Tuple<string, string>>>> actions, NodeEventView nodeEvent = null)
        {
            RequestCreateUndoState();

            if (nodeEvent == null)
            {
                nodeEvent = GraphEditor.CreateNewNodeEvent(node, trigger) as NodeEventView;
            }

            GraphEditor.ChangeNodeEventCondition(node, nodeEvent, string.IsNullOrEmpty(condition) ? "" : condition);

            nodeEvent.ActionsContainer.gameObject.SetActive(false);
            foreach (NodeActionView actionView in nodeEvent.ActionsContainer.GetComponentsInChildren<NodeActionView>())
            {
                GraphEditor.RemoveNodeAction(nodeEvent, actionView);
            }

            foreach (var action in actions)
            {
                var actionView = GraphEditor.CreateNewNodeAction(nodeEvent, action.Item1);
                GraphEditor.ChangeNodeActionParameter(actionView, action.Item2);
            }

            GraphEditor.ChangeNodeEventTrigger(nodeEvent, nodeEvent.Event, trigger);

            LayoutRebuilder.ForceRebuildLayoutImmediate(node.transform as RectTransform);
        }

        /// <summary>
        /// Сохраняет представление ребра
        /// </summary>
        /// <param name="start">Начальный узел</param>
        /// <param name="end">Конечный узел</param>
        /// <param name="trigger">Событие</param>
        /// <param name="condition">Условие</param>
        /// <param name="actions">Список действий и параметров</param>
        /// <param name="edgeView">Представление ребра</param>
        public void SaveEdge(NodeView start, NodeView end, string trigger, string condition,
            List<Tuple<string, List<Tuple<string, string>>>> actions, EdgeView edgeView)
        {
            RequestCreateUndoState();

            if (edgeView.IsPreview)
            {
                if (!GraphEditor.TryApplyPreview(edgeView, start, end, trigger, edgeView.VisualData))
                {
                    return;
                }

                edgeView.VisualData.Position = edgeView.transform.localPosition;
                edgeView.Init(this, edgeView.VisualData, start,
                    end, trigger, condition,
                    _iconSpriteProviderAsset, _lineClickListener);
                EditingEdge = null;
            }

            GraphEditor.ChangeEdgeTrigger(edgeView, trigger);
            GraphEditor.ChangeEdgeCondition(edgeView, string.IsNullOrEmpty(condition) ? "" : condition);

            edgeView.ActionsContainer.gameObject.SetActive(false);
            edgeView.RefreshConditionAndActionsContainer();
            foreach (EdgeActionView actionView in edgeView.ActionsContainer.GetComponentsInChildren<EdgeActionView>())
            {
                GraphEditor.RemoveEdgeAction(edgeView, actionView);
            }

            foreach (var action in actions)
            {
                var actionView = GraphEditor.CreateNewEdgeAction(edgeView, action.Item1);
                GraphEditor.ChangeEdgeActionParameter(actionView, action.Item2);
            }
        }

        /// <summary>
        /// Закрывает редактор представление ребра
        /// </summary>
        public void CancelEdgeEditor()
        {
            _edgeEditorWindow.gameObject.SetActive(false);

            UndoController.DeleteAllUndo(_edgeEditorWindow);

            _zoomPan.gameObject.SetActive(true);
        }

        /// <summary>
        /// Открывает окно редактирования имени представления узла
        /// </summary>
        /// <param name="nodeViewId">Уникальный идентификатор представления узла</param>
        /// <param name="desiredInitialName">Желаемое начальное имя</param>
        /// <param name="needRebuild">Необходимо, ли перестраивать граф после закрытия окна</param>
        public void OpenNodeNamePopUp(string nodeViewId, string desiredInitialName = "", bool needRebuild = false)
        {
            if (!TryGetNodeViewById(nodeViewId, out NodeView nodeView))
            {
                return;
            }

            _editNodeNamePopUp.Init(nodeView, desiredInitialName, needRebuild);
            _editNodeNamePopUp.gameObject.SetActive(true);
        }

        /// <summary>
        /// Перестроение представления графа
        /// </summary>
        public void Rebuild()
        {
            GraphEditor.RebuildView();

            LayoutRebuilder.ForceRebuildLayoutImmediate(GraphElementViewsContainer.transform as RectTransform);
        }

        /// <summary>
        /// Пытается получить представление узла по уникальному идентификатору
        /// </summary>
        /// <param name="id">Уникальный идентификатор узла</param>
        /// <param name="nodeView">Возвращает представление узла, если представление с заданным идентификатором найдено, иначе null</param>
        /// <returns>true, если представление найдено, иначе false</returns>
        public bool TryGetNodeViewById(string id, out NodeView nodeView)
        {
            bool result = GraphEditor.TryGetNodeViewByID(id, out INodeView node);
            nodeView = node as NodeView;
            return result && nodeView != null;
        }

        /// <summary>
        /// Пытается получить представление ребра по уникальному идентификатору
        /// </summary>
        /// <param name="id">Уникальный идентификатор ребра</param>
        /// <param name="edgeView">Возвращает представление ребра, если представление с заданным идентификатором найдено, иначе null</param>
        /// <returns>true, если представление найдено, иначе false</returns>
        public bool TryGetEdgeViewById(string id, out EdgeView edgeView)
        {
            bool result = GraphEditor.TryGetEdgeViewByID(id, out IEdgeView edge);
            edgeView = edge as EdgeView;
            return result && edgeView != null;
        }

        /// <summary>
        /// Создает предварительное представление ребра
        /// </summary>
        /// <param name="sourceView">Начальный узел</param>
        /// <param name="position">Позиция появления ребра</param>
        public void CreateEdgePreview(NodeView sourceView, Vector3 position)
        {
            EditingEdge = (EdgeView)GraphEditor.CreatePreviewEdgeView(sourceView);
            EditingEdge.ConnectSourceView(sourceView);
            LayoutRebuilder.ForceRebuildLayoutImmediate(EditingEdge.transform as RectTransform);
            EditingEdge.transform.position = position;
            ElementSelectionProvider.Select(EditingEdge);
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии на представление узла
        /// </summary>
        /// <param name="nodeView">Представление узла, по которому произошло нажатие</param>
        public void OnClicked(NodeView nodeView)
        {
            if (nodeView.Vertex != NodeData.Vertex_Initial)
            {
                if (ParentingingNode != null && ParentingingNode != nodeView)
                {
                    nodeView.ConnectParent();

                    return;
                }

                if (EditingEdge == null)
                {
                    return;
                }

                if (EditingEdge.IsPreview && EditingEdge.SourceView != nodeView)
                {
                    EditingEdge.ConnectTargetView(nodeView);
                    nodeView.SetOutlineVisibility(false);
                    OpenEdgeEditor(EditingEdge);
                }
                else
                {
                    if (EditingEdge.TargetView == null && EditingEdge.SourceView.Vertex == NodeData.Vertex_Initial)
                    {
                        GraphEditor.ConnectToInitialNode(nodeView);
                        return;
                    }

                    EdgeView edgeView = null;
                    if (EditingEdge.SourceView == null && EditingEdge.TargetView != nodeView)
                    {
                        edgeView = (EdgeView)GraphEditor.CreateNewEdge(nodeView, EditingEdge.TargetView, EditingEdge.TriggerID);
                        nodeView.SetOutlineVisibility(false);
                    }
                    else if (EditingEdge.TargetView == null && EditingEdge.SourceView != nodeView)
                    {
                        edgeView = (EdgeView)GraphEditor.CreateNewEdge(EditingEdge.SourceView, nodeView, EditingEdge.TriggerID);
                        nodeView.SetOutlineVisibility(false);
                    }

                    if (edgeView != null)
                    {
                        edgeView.transform.localPosition = edgeView.VisualData.Position = EditingEdge.transform.localPosition;
                        GraphEditor.ChangeEdgeTrigger(edgeView, EditingEdge.TriggerID);
                        GraphEditor.ChangeEdgeCondition(edgeView, string.IsNullOrEmpty(EditingEdge.Condition) ? "" : EditingEdge.Condition);
                        foreach (EdgeActionView actionView in EditingEdge.ActionsContainer.GetComponentsInChildren<EdgeActionView>())
                        {
                            GraphEditor.ChangeEdgeActionParameter(GraphEditor.CreateNewEdgeAction(edgeView, actionView.ActionID), actionView.ParameterValue);
                        }

                        EditingEdge.Delete();
                    }
                }
            }
        }

        private void OnNodeParentingRequested(NodeView nodeView)
        {
            RequestCreateUndoState();

            if (ParentingingNode == null)
            {
                ParentingingNode = nodeView;
                return;
            }

            if (ParentingingNode == nodeView)
            {
                return;
            }

            GraphEditor.SetParent(ParentingingNode, nodeView, true);

            if (!ParentingingNode.IsUniqueNodeName(ParentingingNode.VisualData.Name))
            {
                OpenNodeNamePopUp(ParentingingNode.ID, ParentingingNode.VisualData.Name);
            }

            ParentingingNode = null;
        }

        private void OnNodeParentingCanceled(NodeView nodeView)
        {
            if (ParentingingNode == nodeView)
            {
                ParentingingNode = null;
                nodeView.Unselect();
            }
        }

    #region Undo
    
        /// <summary>
        /// Создает состояние для отмены действия
        /// </summary>
        public void RequestCreateUndoState()
        {
            UndoController.CreateUndoState(this);
        }

        /// <inheritdoc/>
        public void Undo(string context)
        {
            ApplyContext(context);
        }

        /// <inheritdoc/>
        public void Redo(string context)
        {
            ApplyContext(context);
        }

        private void ApplyContext(string context)
        {
            XDocument document = XDocument.Parse(context);
            CyberiadaGraphDocument graphDocument = _converter.Deserialize(document.Root);

            if (graphDocument != null)
            {
                SetGraphDocument(graphDocument);
            }
        }

        /// <inheritdoc/>
        public string GetUndoContext()
        {
            return _converter.Serialize(GraphDocument).ToString();
        }

        /// <inheritdoc/>
        public string GetCurrentContext()
        {
            return _converter.Serialize(GraphDocument).ToString();
        }

    #endregion

    #region Factory Realization

        [Header("Factory")]
        [SerializeField] private Vector2 _newNodeOffset;
        [SerializeField] private Vector2 _dublicateNodeOffset;
        [SerializeField] private float _newEdgeOffset;
        [Header("Prefabs")]
        [SerializeField] private GraphView _graphPrefab;
        [SerializeField] private NodeView _initialNodePrefab;
        [SerializeField] private NodeView _nodePrefab;
        [SerializeField] private NodeEventView _nodeEventPrefab;
        [SerializeField] private NodeActionView _nodeActionPrefab;
        [SerializeField] private EdgeView _edgePrefab;
        [SerializeField] private EdgeActionView _edgeActionPrefab;
        [Header("Containers")]
        [SerializeField] private LineClickListener _lineClickListener;
        [field:SerializeField] public GraphLayoutGroup GraphElementViewsContainer { get; private set; }

        /// <summary>
        /// Создает новое представление графа
        /// </summary>
        /// <param name="graphID">Уникальный идентификатор графа</param>
        /// <param name="parentNodeView">Представление родительской вершины</param>
        /// <returns>Представление графа</returns>
        public IGraphView CreateGraphView(string graphID, INodeView parentNodeView)
        {
            NodeView nodeView = parentNodeView as NodeView;
            GraphView graphView = Instantiate(_graphPrefab, nodeView.ChildsContainer);
            graphView.ParentNode = nodeView;
            graphView.GetComponent<GraphLayoutGroup>().moveTransform = nodeView.transform as RectTransform;

            return graphView;
        }

        /// <summary>
        /// Создает новое представление узла
        /// </summary>
        /// <param name="nodeVisualData">Данные для визуального представления узла</param>
        /// <param name="vertex">Имя вершины</param>
        /// <param name="layoutAutomatically"></param>
        /// <returns>Представление узла</returns>
        public INodeView CreateNodeView(NodeVisualData nodeVisualData, string vertex, bool layoutAutomatically)
        {
            NodeView view = Instantiate(vertex == NodeData.Vertex_Initial ? _initialNodePrefab : _nodePrefab, GraphElementViewsContainer.transform);

            if (layoutAutomatically)
            {
                GraphElementViewsContainer.GetGraphCorners(out float left, out float top, out float right, out float bottom);

                RectTransform nodeRectTransform = view.transform as RectTransform;

                LayoutRebuilder.ForceRebuildLayoutImmediate(nodeRectTransform);

                float xPos;
                float yPos = (top + bottom) / 2;

                if (vertex == NodeData.Vertex_Initial)
                {
                    xPos = left - nodeRectTransform.sizeDelta.x / 2 * nodeRectTransform.lossyScale.x;
                }
                else
                {
                    xPos = right + nodeRectTransform.sizeDelta.x / 2 * nodeRectTransform.lossyScale.x;
                }

                Vector3 scaledOffset = Vector2.Scale(_newNodeOffset, GraphElementViewsContainer.transform.lossyScale);

                view.transform.position = (Vector2)GraphElementViewsContainer.transform.position + new Vector2(xPos, yPos) + (vertex == NodeData.Vertex_Initial ? Vector2.zero : (Vector2)scaledOffset);
                nodeVisualData.Position = view.transform.localPosition;
            }

            view.gameObject.name = $"Node View ({nodeVisualData.Name})";
            view.Init(this, vertex, nodeVisualData);
        
            view.NodeParentRequested += OnNodeParentingRequested;
            view.NodeParentCanceled += OnNodeParentingCanceled;

            return view;
        }

        /// <summary>
        /// Дублирует представление узла
        /// </summary>
        /// <param name="nodeView">Оригинальное представление узла</param>
        /// <param name="layoutAutomatically"></param>
        public void DuplicateNodeView(NodeView nodeView, bool layoutAutomatically)
        {
            RequestCreateUndoState();

            if (!GraphEditor.TryDuplicateNode(nodeView, out INodeView temp))
            {
                return;
            }

            NodeView duplicatedNode = temp as NodeView;

            if (duplicatedNode == null)
            {
                return;
            }

            Vector2 offset = Vector2.zero;

            if (layoutAutomatically)
            {
                RectTransform nodeRectTransform = nodeView.transform as RectTransform;

                float xPos = nodeRectTransform.sizeDelta.x;

                offset = new Vector2(xPos, 0) + Vector2.Scale(_dublicateNodeOffset, duplicatedNode.transform.parent.lossyScale);
            }

            duplicatedNode.transform.localPosition += (Vector3)offset;
            duplicatedNode.VisualData.Position += offset;

            OpenNodeNamePopUp(duplicatedNode.ID, duplicatedNode.VisualData.Name, true);
        }

        /// <summary>
        /// Создает представление перехода в узле
        /// </summary>
        /// <param name="nodeView">Представление узла</param>
        /// <param name="triggerID">Событие</param>
        /// <param name="event">Переход</param>
        /// <returns>Представление перехода</returns>
        public INodeEventView CreateNodeEventView(INodeView nodeView, string triggerID, Event @event)
        {
            NodeView view = nodeView as NodeView;

            NodeEventView eventView = Instantiate(_nodeEventPrefab, view.TriggersContainer);
            eventView.Init(triggerID, @event, view, this, _iconSpriteProviderAsset);

            return eventView;
        }

        /// <summary>
        /// Создает представление поведения в переходе для узла 
        /// </summary>
        /// <param name="eventView">Представление перехода</param>
        /// <param name="actionID">Переход</param>
        /// <returns>Представление поведения</returns>
        public INodeActionView CreateNodeActionView(INodeEventView eventView, string actionID)
        {
            NodeEventView view = eventView as NodeEventView;

            view.ActionsContainer.gameObject.SetActive(true);
            NodeActionView actionInstance = Instantiate(_nodeActionPrefab, view.ActionsContainer);
            actionInstance.Init(actionID, this, view, _iconSpriteProviderAsset);

            return actionInstance;
        }

        /// <summary>
        /// Создает представление ребра
        /// </summary>
        /// <param name="sourceNode">Входящее представление узла</param>
        /// <param name="targetNode">Исходящее представление узла</param>
        /// <param name="edgeVisualData">Данные для представления ребра</param>
        /// <param name="triggerID">Событие</param>
        /// <param name="condition">Условие</param>
        /// <returns>Представление ребра</returns>
        public IEdgeView CreateEdgeView(INodeView sourceNode, INodeView targetNode, EdgeVisualData edgeVisualData,
            string triggerID, string condition)
        {
            NodeView sourceNodeView = (NodeView) sourceNode;
            NodeView targetNodeView = (NodeView)targetNode;
            EdgeView view = Instantiate(_edgePrefab);
            view.gameObject.name = $"Edge View ({sourceNodeView.VisualData.Name})";
            view.Init(this, edgeVisualData, sourceNodeView, targetNodeView, triggerID, condition,
                _iconSpriteProviderAsset, _lineClickListener);

            return view;
        }
    
        /// <summary>
        /// Создает предварительное представление ребра
        /// </summary>
        /// <param name="sourceView">Входящее представление узла</param>
        /// <returns>Предварительно созданное представление ребра</returns>
        public IEdgeView CreatePreviewEdgeView(INodeView sourceView)
        {
            NodeView sourceNodeView = (NodeView)sourceView;
            EdgeView view = Instantiate(_edgePrefab, GraphElementViewsContainer.transform);
            view.gameObject.name = $"Edge View ({sourceNodeView.VisualData.Name})";
            view.Init(this, _lineClickListener);
            return view;
        }

        /// <summary>
        /// Создает представление поведения в переходе для ребра
        /// </summary>
        /// <param name="edgeView">Представление ребра</param>
        /// <param name="actionID">Поведение</param>
        /// <returns>Представление поведения</returns>
        public IEdgeActionView CreateEdgeActionView(IEdgeView edgeView, string actionID)
        {
            EdgeView view = edgeView as EdgeView;

            view.ActionsContainer.gameObject.SetActive(true);
            view.RefreshConditionAndActionsContainer();

            EdgeActionView actionInstance = Instantiate(_edgeActionPrefab, view.ActionsContainer);
            actionInstance.Init(actionID, this, view, _iconSpriteProviderAsset);

            return actionInstance;
        }

        /// <summary>
        /// Разрушает представление элемента графа 
        /// </summary>
        /// <param name="view">Элемент графа</param>
        public void DestroyElementView(IGraphElementView view)
        {
            if (view is Component unityView)
            {
                if (view is NodeView unityNodeView)
                {
                    if (ParentingingNode == unityNodeView)
                    {
                        ParentingingNode = null;
                    }

                    unityNodeView.NodeParentRequested -= OnNodeParentingRequested;
                    unityNodeView.NodeParentCanceled -= OnNodeParentingCanceled;
                }

                Transform parent = unityView.transform.parent;

                Destroy(unityView.gameObject);

                LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
            }
        }

    #endregion
    }
}
