using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, отвечающий за редактор имени узла
    /// </summary>
    public class EditNodeNamePopUp : MonoBehaviour, IUndoable, IElementSelectable, IPanZoomIgnorer
    {
        private const string NotUniqueNameError = "Состояние с таким именем уже существует на этом уровне";

        [Header("Scene Context")]
        [SerializeField] private PanZoom _panZoom;
        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;
        [Header("Controls")]
        [SerializeField] private GameObject _errorMessage;
        [SerializeField] private TextMeshProUGUI _errorText;
        [SerializeField] private Button _applyButton;
        [SerializeField] private TMP_InputField _nodeNameInputField;

        private NodeView _nodeView;
        private bool _needRebuild;
        private string _desiredInitialName;
        private SelectionContextSource _selectionContextSource;

        public string GetUndoContext() => _desiredInitialName;
        public string GetCurrentContext() => _nodeNameInputField.text;
        public GameObject SelectedObject => gameObject;
        public ISelectionContextSource SelectionContextSource => _selectionContextSource;

        /// <summary>
        /// Инициализирует <see cref="EditNodeNamePopUp"/>
        /// </summary>
        /// <param name="nodeView">Представление узла</param>
        /// <param name="desiredInitialName">Желаемое начально имя</param>
        /// <param name="needRebuild">Нужно ли перестраивать представление графа</param>
        public void Init(NodeView nodeView, string desiredInitialName, bool needRebuild = false)
        {
            _nodeView = nodeView;
            _desiredInitialName = desiredInitialName;
            _needRebuild = needRebuild;
            _nodeNameInputField.text = desiredInitialName;
            OnInputFieldValueChanged(desiredInitialName);
        }

        private void Awake()
        {
            _selectionContextSource = new SelectionContextSource();
            _selectionContextSource.AddHotkeyAction(new HotkeyAction(OnCancelHotkeyPressed, KeyCode.Escape));
        }

        private void OnEnable()
        {
            _panZoom.FocusOnRectTransform(_nodeView.transform as RectTransform);
            _applyButton.onClick.AddListener(ApplyNodeName);
            _nodeNameInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            _runtimeGraphEditor.ElementSelectionProvider.Select(this);
            _runtimeGraphEditor.UndoController.CreateUndoState(this);
            _runtimeGraphEditor.UndoController.LockUndoable(this);
        }

        private void OnDisable()
        {
            _applyButton.onClick.RemoveListener(ApplyNodeName);
            _nodeNameInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            _nodeView.Select(false);
            _runtimeGraphEditor.UndoController.LockUndoable(null);
        }

        /// <inheritdoc/>
        public void Undo(string context)
        {
            _nodeNameInputField.text = context;
        }

        /// <inheritdoc/>
        public void Redo(string context)
        {
            _nodeNameInputField.text = context;
        }

        /// <inheritdoc/>
        public void Unselect()
        {
            _runtimeGraphEditor.ElementSelectionProvider?.Unselect(this);
        }

        private void ApplyNodeName()
        {
            _nodeView.SetName(_nodeNameInputField.text);
            gameObject.SetActive(false);

            if (_needRebuild)
            {
                _runtimeGraphEditor.Rebuild();
                _runtimeGraphEditor.ElementSelectionProvider.Select(_nodeView.ID);
            }
        }

        private void OnInputFieldValueChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _errorMessage.SetActive(false);
                _applyButton.gameObject.SetActive(true);
                _applyButton.interactable = false;
                return;
            }

            if (!_nodeView.IsUniqueNodeName(_nodeNameInputField.text))
            {
                _errorMessage.SetActive(true);
                _applyButton.gameObject.SetActive(false);
                _errorText.SetText(NotUniqueNameError);
                return;
            }

            _errorMessage.SetActive(false);
            _applyButton.gameObject.SetActive(true);
            _applyButton.interactable = true;
        }

        private void OnCancelHotkeyPressed()
        {
            if (string.IsNullOrEmpty(_nodeNameInputField.text) || !_nodeView.IsUniqueNodeName(_nodeNameInputField.text) || string.IsNullOrEmpty(_desiredInitialName))
            {
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
