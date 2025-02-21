using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, отвечающий за редактор имени узла
    /// </summary>
    public class EditNodeNamePopUp : MonoBehaviour
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

        /// <summary>
        /// Инициализирует <see cref="EditNodeNamePopUp"/>
        /// </summary>
        /// <param name="nodeView">Представление узла</param>
        /// <param name="desiredInitialName">Желаемое начально имя</param>
        /// <param name="needRebuild">Нужно ли перестраивать представление графа</param>
        public void Init(NodeView nodeView, string desiredInitialName, bool needRebuild = false)
        {
            _nodeView = nodeView;
            _nodeNameInputField.text = desiredInitialName;
            _needRebuild = needRebuild;
            OnInputFieldValueChanged(desiredInitialName);
        }

        private void OnEnable()
        {
            _panZoom.FocusOnRectTransform(_nodeView.transform as RectTransform);
            _applyButton.onClick.AddListener(ApplyNodeName);
            _nodeNameInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        private void OnDisable()
        {
            _applyButton.onClick.RemoveListener(ApplyNodeName);
            _nodeNameInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
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
