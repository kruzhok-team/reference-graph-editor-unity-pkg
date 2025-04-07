using UnityEngine;

namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент, слушающий нажатие горячих клавиш
    /// </summary>
    public class HotkeyListener : MonoBehaviour
    {
        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;

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
                }
            }

            if (_selectionContextSource == null)
            {
                return;
            }
            
            foreach (HotkeyAction hotkeyAction in _selectionContextSource.HotkeyActions)
            {
                bool isValidCombination = true;

                for (int i = 0; i < hotkeyAction.Hotkeys.Count; i++)
                {
                    KeyCode key = hotkeyAction.Hotkeys[i];

                    if (i < hotkeyAction.Hotkeys.Count - 1)
                    {
                        if (!Input.GetKey(key))
                        {
                            isValidCombination = false;
                            break;
                        }
                    }
                    else
                    {
                        if (!Input.GetKeyDown(key))
                        {
                            isValidCombination = false;
                        }
                    }
                }

                if (isValidCombination)
                {
                    hotkeyAction.OnHotkeyPressed();
                }
            }
        }
    }
}