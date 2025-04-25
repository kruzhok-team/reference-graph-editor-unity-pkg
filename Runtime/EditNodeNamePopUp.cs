using TMPro;
using UI.Focusing;
using UnityEngine;
using UnityEngine.UI;

namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, отвечающий за редактор имени узла
    /// </summary>
    public class EditNodeNamePopUp : MonoBehaviour, IUndoable
    {
        private const string NotUniqueNameError = "Состояние с таким именем уже существует на этом уровне";

        [Header("Scene Context")]
        [SerializeField] private SimpleContextLevel _contextLayer;
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

        public string GetUndoContext() => _desiredInitialName;
        public string GetCurrentContext() => _nodeNameInputField.text;

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

            _contextLayer.PushLayer();
        }

        private void OnEnable()
        {
            _panZoom.FocusOnRectTransform(_nodeView.transform as RectTransform);
            _nodeNameInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            _runtimeGraphEditor.UndoController.CreateUndoState(this);
            _runtimeGraphEditor.UndoController.LockUndoable(this);
            _runtimeGraphEditor.LineClickListener.enabled = false;
        }

        private void OnDisable()
        {
            _nodeNameInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            _nodeView.Select();
            _runtimeGraphEditor.UndoController.LockUndoable(null);
            _runtimeGraphEditor.LineClickListener.enabled = true;
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

        public void ApplyNodeName()
        {
            if (string.IsNullOrEmpty(_nodeNameInputField.text))
            {
                return;
            }

            _nodeView.SetName(_nodeNameInputField.text);
            
            if (_needRebuild)
            {
                _runtimeGraphEditor.Rebuild();
                _runtimeGraphEditor.Select(_nodeView.ID);
            }

            _contextLayer.RemoveLayer();
        }

        public void Cancel()
        {
            _contextLayer.RemoveLayer();
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
    }
}
