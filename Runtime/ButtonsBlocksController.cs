using UI.Focusing;
using UnityEngine;
using UnityEngine.UI;

namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, отвечающий за вспомогательные кнопки управления элементами
    /// </summary>
    public class ButtonsBlocksController : MonoBehaviour
    {
        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private GameObject _noSelectionBlock;
        [SerializeField] private GameObject _nodeSelectionBlock;
        [SerializeField] private GameObject _stateSelectionBlock;
        [SerializeField] private GameObject _edgeSelectionBlock;

        [SerializeField] private Button _unparentButton;

        private ISelectable _selectedElement;

        private void Start()
        {
            UIFocusingSystem.Instance.SelectionHandler.ElementSelected += OnElementSelected;
            UIFocusingSystem.Instance.SelectionHandler.ElementDeselected += OnElementDeselected;

            _runtimeGraphEditor.StartEdgeEditing += OnStartEdgeEditing;
            _runtimeGraphEditor.EndEdgeEditing += OnEndEdgeEditing;

            OnElementDeselected(null);
        }

        private void OnDestroy()
        {
            UIFocusingSystem.Instance.SelectionHandler.ElementSelected -= OnElementSelected;
            UIFocusingSystem.Instance.SelectionHandler.ElementDeselected -= OnElementDeselected;

            _runtimeGraphEditor.StartEdgeEditing -= OnStartEdgeEditing;
            _runtimeGraphEditor.EndEdgeEditing -= OnEndEdgeEditing;
        }

        /// <summary>
        /// Универсальный метод: пытается получить компонент View указанного типа T
        /// из текущего выбранного ISelectable.
        /// </summary>
        private bool TryGetView<T>(out T view) where T : Component
        {
            view = null;

            if (_selectedElement == null || _selectedElement.Object == null)
            {
                return false;
            }

            return _selectedElement.Object.TryGetComponent(out view);
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Редактировать"
        /// Открывает редактор события или редактор ребра.
        /// </summary>
        public void OnEditButtonPressed()
        {
            if (TryGetView(out NodeEventView nodeEventView))
            {
                nodeEventView.OpenEventEditor();

                return;
            }

            if (TryGetView(out EdgeView edgeView))
            {
                if (_runtimeGraphEditor.EditingEdge != null && _runtimeGraphEditor.EditingEdge.IsPreview)
                {
                    return;
                }

                edgeView.OpenEdgeEditor();

                return;
            }

            OnElementDeselected(null);
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Добавить событие" на узле
        /// </summary>
        public void OnCreateNodeButtonPressed()
        {
            if (TryGetView(out NodeView nodeView))
            {
                nodeView.AddEvent();
            }
            else
            {
                OnElementDeselected(null);
            }
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Сделать узел дочерним"
        /// </summary>
        public void OnChildNodeButtonPressed()
        {
            if (TryGetView(out NodeView nodeView))
            {
                nodeView.AddChildNode();
            }
            else
            {
                OnElementDeselected(null);
            }
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Сделать узел родительским"
        /// </summary>
        public void OnParentNodeButtonPressed()
        {
            if (TryGetView(out NodeView nodeView))
            {
                nodeView.ConnectParent();
                _nodeSelectionBlock.SetActive(false);
            }
            else
            {
                OnElementDeselected(null);
            }
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Дублировать"
        /// Дублирует узел или ребро.
        /// </summary>
        public void OnDuplicateButtonPressed()
        {
            if (TryGetView(out NodeView nodeView))
            {
                nodeView.Duplicate();

                return;
            }
            if (TryGetView(out EdgeView edgeView))
            {
                edgeView.Duplicate();

                return;
            }

            OnElementDeselected(null);
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Удалить связь с родителем"
        /// </summary>
        public void OnUnparentButtonPressed()
        {
            if (TryGetView(out NodeView nodeView))
            {
                nodeView.Unparent();
            }
            else
            {
                OnElementDeselected(null);
            }
        }

        /// <summary>
        /// Вызывается при нажатии кнопки "Удалить"
        /// Удаляет узел, событие или ребро в зависимости от выбора.
        /// </summary>
        public void OnDeleteButtonPressed()
        {
            if (TryGetView(out NodeView nodeView))
            {
                nodeView.Delete();
            }
            else if (TryGetView(out NodeEventView nodeEventView))
            {
                nodeEventView.Delete();
            }
            else if (TryGetView(out EdgeView edgeView))
            {
                edgeView.Delete();
            }
            else
            {
                _noSelectionBlock.SetActive(true);
            }

            OnElementDeselected(null);
        }

        /// <summary>
        /// Обработчик события выбора элемента в системе UIFocusing
        /// Отображает соответствующий блок кнопок
        /// </summary>
        private void OnElementSelected(ISelectable element)
        {
            _selectedElement = element;

            _noSelectionBlock.SetActive(false);
            _nodeSelectionBlock.SetActive(false);
            _stateSelectionBlock.SetActive(false);
            _edgeSelectionBlock.SetActive(false);

            if (_selectedElement == null)
            {
                return;
            }

            if (TryGetView(out NodeView nodeView))
            {
                if (nodeView.Vertex == "initial")
                {
                    OnElementDeselected(null);

                    return;
                }

                _nodeSelectionBlock.SetActive(true);
                _unparentButton.interactable = nodeView.HasParent;

                return;
            }

            if (TryGetView<NodeEventView>(out _))
            {
                _stateSelectionBlock.SetActive(true);

                return;
            }

            if (TryGetView(out EdgeView edgeView))
            {
                if (edgeView.SourceView.Vertex != "initial")
                {
                    _edgeSelectionBlock.SetActive(true);
                }

                return;
            }

            _noSelectionBlock.SetActive(true);
        }

        /// <summary>
        /// Обработчик события сброса выбора
        /// Скрывает все блоки, активирует блок "нет выбора"
        /// </summary>
        private void OnElementDeselected(ISelectable element)
        {
            _noSelectionBlock.SetActive(true);
            _nodeSelectionBlock.SetActive(false);
            _stateSelectionBlock.SetActive(false);
            _edgeSelectionBlock.SetActive(false);

            _unparentButton.interactable = false;
            _selectedElement = null;
        }

        /// <summary>
        /// Отключает интерактивность кнопок при старте редактирования ребра
        /// </summary>
        private void OnStartEdgeEditing()
        {
            _canvasGroup.interactable = false;
        }

        /// <summary>
        /// Включает интерактивность кнопок по завершении редактирования ребра
        /// </summary>
        private void OnEndEdgeEditing()
        {
            _canvasGroup.interactable = true;
        }
    }
}
