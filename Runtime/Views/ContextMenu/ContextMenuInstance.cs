using UnityEngine;
using UnityEngine.Events;
namespace Talent.GraphEditor.Unity.Runtime.ContextMenu
{
    /// <summary>
    /// Класс, представляющий экземпляр контекстно меню
    /// </summary>
    public class ContextMenuInstance : MonoBehaviour
    {
        /// <summary>
        /// Событие отключения контекстного меню
        /// </summary>
        public UnityEvent OnDisable;

        private static ContextMenuInstance _activeInstance;

        private void OnEnable()
        {
            if (_activeInstance == this)
            {
                return;
            }

            if (_activeInstance != null)
            {
                _activeInstance.gameObject.SetActive(false);
                _activeInstance.OnDisable?.Invoke();
            }

            _activeInstance = this;
        }
    }
}
