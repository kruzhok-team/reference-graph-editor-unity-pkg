using Talent.GraphEditor.Unity.Runtime;
using TMPro;
using UnityEngine;

public class EditGraphName : MonoBehaviour
{
    [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;
    [SerializeField] private TMP_InputField _graphNameInputField;
    [SerializeField] private CanvasGroup _canvasGroup;

    private void OnEnable()
    {
        _graphNameInputField.onEndEdit.AddListener(_runtimeGraphEditor.SetGraphDocumentName);
        _runtimeGraphEditor.StartEdgeEditing += OnStartEdgeEditing;
        _runtimeGraphEditor.EndEdgeEditing += OnEndEdgeEditing;
    }

    private void OnDisable()
    {
        _graphNameInputField.onEndEdit.RemoveListener(_runtimeGraphEditor.SetGraphDocumentName);
        _graphNameInputField.onSelect.RemoveAllListeners();
        _graphNameInputField.onDeselect.RemoveAllListeners();
        _runtimeGraphEditor.StartEdgeEditing -= OnStartEdgeEditing;
        _runtimeGraphEditor.EndEdgeEditing -= OnEndEdgeEditing;
    }
    
    private void OnStartEdgeEditing()
    {
        _canvasGroup.interactable = false;
    }

    private void OnEndEdgeEditing()
    {
        _canvasGroup.interactable = true;
    }
}
