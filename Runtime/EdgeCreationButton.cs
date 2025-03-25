using UnityEngine;
using UnityEngine.EventSystems;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, представляющий кнопку создания узла
    /// </summary>
    public class EdgeCreationButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private NodeView _sourceNode;
        [SerializeField] private Vector2 _edgeSpawnOffset;

        private RuntimeGraphEditor _runtimeGraphEditor;
        private NodeView _otherNode;

        /// <summary>
        /// Инициализация <see cref="EdgeCreationButton"/>
        /// </summary>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        public void Init(RuntimeGraphEditor runtimeGraphEditor)
        {
            _runtimeGraphEditor = runtimeGraphEditor;
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии указателя на кнопку  
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnPointerDown(PointerEventData eventData)
        {
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая в начале процесса перетаскивания кнопки
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_canvasGroup.interactable)
            {
                return;
            }
        
            if (_runtimeGraphEditor.EditingEdge != null)
            {
                return;
            }

            RectTransform sourceNodeRectTransform = (RectTransform)_sourceNode.transform;
            Vector3 position = sourceNodeRectTransform.position + 
                _runtimeGraphEditor.GraphElementViewsContainer.transform.TransformVector(Vector2.Scale(sourceNodeRectTransform.sizeDelta / 2, _edgeSpawnOffset.normalized) + _edgeSpawnOffset);
            _runtimeGraphEditor.CreateEdgePreview(_sourceNode, position);
            _runtimeGraphEditor.EditingEdge.IsDraggableMode = true;
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая в процессе перетаскивания кнопки
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnDrag(PointerEventData eventData)
        {
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая в конце перетаскивания кнопки
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_runtimeGraphEditor.EditingEdge == null || !_runtimeGraphEditor.EditingEdge.IsPreview)
            {
                return;
            }

            NodeView otherNode = _runtimeGraphEditor.EditingEdge.FindOtherNode();

            if (otherNode != null)
            {
                _runtimeGraphEditor.OnClicked(otherNode);
                _runtimeGraphEditor.EditingEdge.IsDraggableMode = false;
            }
            else
            {
                _runtimeGraphEditor.DestroyElementView(_runtimeGraphEditor.EditingEdge);
                _runtimeGraphEditor.EditingEdge = null;
            }
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при отпускании указателя с кнопки
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_canvasGroup.interactable)
            {
                return;
            }
        
            if (_runtimeGraphEditor.EditingEdge != null && _runtimeGraphEditor.EditingEdge.IsDraggableMode)
            {
                return;
            }

            RectTransform sourceNodeRectTransform = (RectTransform)_sourceNode.transform;
            Vector3 position = sourceNodeRectTransform.position + 
                _runtimeGraphEditor.GraphElementViewsContainer.transform.TransformVector(Vector2.Scale(sourceNodeRectTransform.sizeDelta / 2, _edgeSpawnOffset.normalized) + _edgeSpawnOffset);
            _runtimeGraphEditor.CreateEdgePreview(_sourceNode, position);
        }
    }
}
