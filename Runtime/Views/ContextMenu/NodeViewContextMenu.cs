using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime.ContextMenu
{
    /// <summary>
    /// Класс, представляющий контекстное меню представления узла
    /// </summary>
    public class NodeViewContextMenu : MonoBehaviour
    {
        [SerializeField] private NodeView _nodeView;

        [Space]

        [SerializeField] private GameObject _container;

        [SerializeField] private Button _newActionButton;
        [SerializeField] private Button _unparentButton;
        [SerializeField] private Button _parentButton;
        [SerializeField] private Button _childNodeButton;
        [SerializeField] private Button _dublicateButton;
        [SerializeField] private Button _deleteButton;

        private void OnEnable()
        {
            _newActionButton.onClick.AddListener(OnNewActionClicked);
            _unparentButton.onClick.AddListener(OnUnparentClicked);
            _parentButton.onClick.AddListener(OnParentClicked);
            _childNodeButton.onClick.AddListener(OnChildNodeClicked);
            _dublicateButton.onClick.AddListener(OnDublicateClicked);
            _deleteButton.onClick.AddListener(OnDeleteClicked);

            _unparentButton.interactable = _nodeView.HasParent;
        }

        private void OnDisable()
        {
            _newActionButton.onClick.RemoveListener(OnNewActionClicked);
            _unparentButton.onClick.RemoveListener(OnUnparentClicked);
            _parentButton.onClick.RemoveListener(OnParentClicked);
            _childNodeButton.onClick.RemoveListener(OnChildNodeClicked);
            _dublicateButton.onClick.RemoveListener(OnDublicateClicked);
            _deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }

        private void OnNewActionClicked()
        {
            _nodeView.AddEvent();
            _nodeView.Select(false);
        }

        private void OnUnparentClicked()
        {
            _nodeView.Unparent();
            _nodeView.Select(false);
        }

        private void OnParentClicked()
        {
            _nodeView.ConnectParent();
            _nodeView.Select(false);
            gameObject.SetActive(false);
            _container.SetActive(false);
        }

        private void OnChildNodeClicked()
        {
            _nodeView.AddChildNode();
            _nodeView.Select(false);
        }

        private void OnDublicateClicked()
        {
            _nodeView.Duplicate();
            _nodeView.Select(false);
        }

        private void OnDeleteClicked()
        {
            _nodeView.Delete();
            _nodeView.Unselect();
        }
    }
}
