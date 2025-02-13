using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент, слушающий нажатие горячих клавиш
    /// </summary>
    public class HotkeyListener : MonoBehaviour
    {
        [SerializeField] RuntimeGraphEditor _runtimeGraphEditor;

        private IElementSelectionProvider _elementSelectionProvider;

        private ISelectionContextSource _selectionContextSource;

        private void Start()
        {
            _elementSelectionProvider = _runtimeGraphEditor.ElementSelectionProvider;

            _elementSelectionProvider.Selected += OnElementSelected;
            _elementSelectionProvider.Deselected += OnElementDeselected;
        }

        private void OnElementSelected(IElementSelectable element)
        {
            if (element == null)
            {
                return;
            }

            _selectionContextSource = element.SelectionContextSource;
        }

        private void OnElementDeselected(IElementSelectable element)
        {
            _selectionContextSource = null;
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        _runtimeGraphEditor.UndoController.Redo();
                    }
                    else
                    {
                        _runtimeGraphEditor.UndoController.Undo();
                    }

                    return;
                }

                return;
            }

            if (_selectionContextSource == null)
            {
                return;
            }
        
            foreach (HotkeyAction hotkeyAction in _selectionContextSource.HotkeyActions)
            {
                if (Input.GetKeyDown(hotkeyAction.Hotkey))
                {
                    hotkeyAction.OnHotkeyPressed();
                }
            }
        }
    }
}
