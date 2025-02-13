using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Talent.GraphEditor.Unity.Runtime
{
    public class DropdownRemoveEmptyOnSelection : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private TMP_Dropdown _dropdown;

        private bool _wasNeverSelected;

        private void OnEnable()
        {
            _wasNeverSelected = true;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!string.IsNullOrEmpty(_dropdown.captionText.text))
            {
                return;
            }

            if (_wasNeverSelected)
            {
                RemoveTitle();
            }

            _wasNeverSelected = false;
        }

        private void RemoveTitle()
        {
            _dropdown.options.RemoveAt(_dropdown.value);
            _dropdown.value = 1;
            _dropdown.value = 0;
            _dropdown.RefreshShownValue();
        }

    }
}
