using TMPro;
using UnityEngine;

namespace Talent.GraphEditor.Unity.Runtime
{
    public class LocalizedOptionData : TMP_Dropdown.OptionData
    {
        [SerializeField] private string _originalText;
        public string OriginalText { get { return _originalText; } set { _originalText = value; } }
    }
}
