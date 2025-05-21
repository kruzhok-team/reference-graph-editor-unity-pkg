using UnityEngine;
using UnityEngine.EventSystems;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, представляющий кнопку редактирования линии узла
    /// </summary>
    public class EdgeEditingButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private EdgeView _edgeView;
        [SerializeField] private bool _isSource;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RuntimeGraphEditor _runtimeGraphEditor;

        /// <summary>
        /// Инициализация <see cref="EdgeEditingButton"/>
        /// </summary>
        /// <param name="runtimeGraphEditor">Редактор графа</param>
        public void Init(RuntimeGraphEditor runtimeGraphEditor)
        {
            _runtimeGraphEditor = runtimeGraphEditor;
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая в начале процесса перетаскивания кнопки
        /// </summary>
        /// <param name="eventData">Полезная нагрузка события связанного с указателем</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_canvasGroup.interactable || _runtimeGraphEditor.EditingEdge != null)
            {
                return;
            }
        
            if (_isSource)
            {
                _edgeView.ChangeEdgeSourceNode();
            }
            else
            {
                _edgeView.ChangeEdgeTargetNode();
            }
        
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
            if (_runtimeGraphEditor.EditingEdge == null || _runtimeGraphEditor.EditingEdge.IsPreview)
            {
                return;
            }

            NodeView otherNode = _runtimeGraphEditor.EditingEdge.FindOtherNode();

            if (otherNode != null)
            {
                _runtimeGraphEditor.OnClicked(otherNode);
            }
            else
            {
                _runtimeGraphEditor.UndoController.Undo();
            }
            
            _runtimeGraphEditor.EditingEdge = null;
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
        
            if (_isSource)
            {
                _edgeView.ChangeEdgeSourceNode();
            }
            else
            {
                _edgeView.ChangeEdgeTargetNode();
            }
        }
    
        /// <summary>
        /// Активирует кнопку
        /// </summary>
        public void Activate()
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
        }

        /// <summary>
        /// Деактивирует кнопку
        /// </summary>
        public void Deactivate()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
        }
    }
}
