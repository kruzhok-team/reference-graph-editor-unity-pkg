using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, реализующий <see cref="IElementSelectable"/>
    /// </summary>
    public class SimpleSelection : MonoBehaviour, IElementSelectable, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// Событие, срабатывающее при выборе элемента
        /// </summary>
        public UnityEvent Selected;
    
        /// <summary>
        /// Событие, срабатывающее при отмене выбора элемента
        /// </summary>
        public UnityEvent Deselected;

        [SerializeField] private GameObject _selectionObjectOverride;

        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;

        /// <inheritdoc/>
        public GameObject SelectedObject => _selectionObjectOverride == null ? gameObject : _selectionObjectOverride;

        private SelectionContextSource _selectionContextSource;
        /// <inheritdoc/>
        public ISelectionContextSource SelectionContextSource => _selectionContextSource;

        private void Awake()
        {
            _selectionContextSource = new();

            if (_runtimeGraphEditor == null)
            {
                _runtimeGraphEditor = FindAnyObjectByType<RuntimeGraphEditor>();

                if (_runtimeGraphEditor == null)
                {
                    Debug.LogError($"Can't find {nameof(RuntimeGraphEditor)} for {gameObject} {nameof(SimpleSelection)}");
                    Destroy(this);
                }
            }
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
            if (eventData.dragging || eventData.delta.magnitude > 0)
            {
                return;
            }

            _runtimeGraphEditor.ElementSelectionProvider.Select(this);

            Selected?.Invoke();
        }

        /// <summary>
        /// Отменяет выделение данного элемента
        /// </summary>
        public void Unselect()
        {
            _runtimeGraphEditor.ElementSelectionProvider.Unselect(this);

            Deselected?.Invoke();
        }
    }
}
