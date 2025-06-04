using UI.Focusing;
using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime.ContextMenu
{
    /// <summary>
    /// Класс, представляющий контекстное меню действия узла
    /// </summary>
    public class ActionContextMenu : MonoBehaviour
    {
        [SerializeField] private SimpleContextLayer _context;
        [SerializeField] private NodeView _nodeView;

        [Space]

        [SerializeField] private Button _editButton;
        [SerializeField] private Button _deleteButton;

        private NodeEventView _eventView;

        /// <summary>
        /// Инициализирует <see cref="ActionContextMenu"/>
        /// </summary>
        /// <param name="eventView">Представление события узла</param>
        public void Init(NodeEventView eventView)
        {
            _eventView = eventView;

            _context.PushLayer();
        }

        private void OnEnable()
        {
            _editButton.onClick.AddListener(OnEditClicked);
            _deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        private void OnDisable()
        {
            _editButton.onClick.RemoveListener(OnEditClicked);
            _deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }

        private void OnEditClicked()
        {
            _nodeView.Select();
            _eventView.OpenEventEditor();
        }

        private void OnDeleteClicked()
        {
            _eventView.Delete();
            _nodeView.Select();
        }
    }
}
