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

        private IElementSelectable _selectedElement;

        private void Start()
        {
            _runtimeGraphEditor.ElementSelectionProvider.Selected += OnElementSelected;
            _runtimeGraphEditor.ElementSelectionProvider.Deselected += OnElementDeselected;
            _runtimeGraphEditor.StartEdgeEditing += OnStartEdgeEditing;
            _runtimeGraphEditor.EndEdgeEditing += OnEndEdgeEditing;
            OnElementDeselected(null);
        }

        private void OnDestroy()
        {
            _runtimeGraphEditor.ElementSelectionProvider.Selected -= OnElementSelected;
            _runtimeGraphEditor.ElementSelectionProvider.Deselected -= OnElementDeselected;
            _runtimeGraphEditor.StartEdgeEditing -= OnStartEdgeEditing;
            _runtimeGraphEditor.EndEdgeEditing -= OnEndEdgeEditing;
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающее при нажатии на кнопек редактирования
        /// </summary>
        public void OnEditButtonPressed()
        {
            switch (_selectedElement)
            {
                case NodeEventView nodeEventView:
                    nodeEventView.OpenEventEditor();
                    break;
                case EdgeView edgeView:
                    if (_runtimeGraphEditor.EditingEdge != null && _runtimeGraphEditor.EditingEdge.IsPreview)
                    {
                        return;
                    }

                    edgeView.OpenEdgeEditor();
                    break;
                default:
                    OnElementDeselected(null);
                    break;
            }
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии на кнопку создания узла
        /// </summary>
        public void OnCreateNodeButtonPressed()
        {
            if (_selectedElement is NodeView nodeView)
            {
                nodeView.AddEvent();
            }
            else
            {
                OnElementDeselected(null);
            }
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии на кнопку "Сделать узел дочерним"
        /// </summary>
        public void OnChildNodeButtonPressed()
        {
            if (_selectedElement is NodeView nodeView)
            {
                nodeView.AddChildNode();
            }
            else
            {
                OnElementDeselected(null);
            }
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии на кнопку "Сделать узел родительским"
        /// </summary>
        public void OnParentNodeButtonPressed()
        {
            if (_selectedElement is NodeView nodeView)
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
        /// Функция обратного вызова, срабатывающая при нажатии на кнопку "Дублировать"
        /// </summary>
        public void OnDuplicateButtonPressed()
        {
            switch (_selectedElement)
            {
                case NodeView nodeView:
                    nodeView.Duplicate();
                    break;
                case EdgeView edgeView:
                    edgeView.Duplicate();
                    break;
                default:
                    OnElementDeselected(null);
                    break;
            }
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии на кнопку, которая делает узел не родительским
        /// </summary>
        public void OnUnparentButtonPressed()
        {
            if (_selectedElement is NodeView nodeView)
            {
                nodeView.Unparent();
            }
            else
            {
                OnElementDeselected(null);
            }
        }

        /// <summary>
        /// Функция обратного вызова, срабатывающая при нажатии на кнопку удаления
        /// </summary>
        public void OnDeleteButtonPressed()
        {
            switch (_selectedElement)
            {
                case NodeView nodeView:
                    nodeView.Delete();
                    break;
                case NodeEventView nodeEventView:
                    nodeEventView.Delete();
                    break;
                case EdgeView edgeView:
                    edgeView.Delete();
                    break;
                default:
                    _noSelectionBlock.SetActive(true);
                    break;
            }

            OnElementDeselected(null);
        }

        private void OnElementSelected(IElementSelectable element)
        {
            _selectedElement = element;

            _noSelectionBlock.SetActive(false);
            _nodeSelectionBlock.SetActive(false);
            _stateSelectionBlock.SetActive(false);
            _edgeSelectionBlock.SetActive(false);

            switch (element)
            {
                case null:
                    break;
                case NodeView nodeView:

                    if (nodeView.Vertex == "initial")
                    {
                        OnElementDeselected(null);
                        break;
                    }

                    _nodeSelectionBlock.SetActive(true);
                    _unparentButton.interactable = nodeView.HasParent;
                    break;
                case NodeEventView _:
                    _stateSelectionBlock.SetActive(true);
                    break;
                case EdgeView edgeView:
                    if (edgeView.SourceView.Vertex != "initial")
                    {
                        _edgeSelectionBlock.SetActive(true);
                    }
                    break;
                default:
                    _noSelectionBlock.SetActive(true);
                    break;
            }
        }

        private void OnElementDeselected(IElementSelectable element)
        {
            _noSelectionBlock.SetActive(true);
            _nodeSelectionBlock.SetActive(false);
            _stateSelectionBlock.SetActive(false);
            _edgeSelectionBlock.SetActive(false);

            _unparentButton.interactable = false;

            _selectedElement = null;
        }

        private void OnStartEdgeEditing()
        {
            _canvasGroup.interactable = false;
        }

        private void OnEndEdgeEditing()
        {
            _canvasGroup.interactable = true;
        }
    }
}
