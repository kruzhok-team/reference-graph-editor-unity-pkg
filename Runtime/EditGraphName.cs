using Talent.GraphEditor.Unity.Runtime;
using TMPro;
using UnityEngine;

public class EditGraphName : MonoBehaviour, IElementSelectable, IPanZoomIgnorer
{
    [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;
    [SerializeField] private TMP_InputField _graphNameInputField;

    public GameObject SelectedObject => gameObject;
    public ISelectionContextSource SelectionContextSource { get; } = new SelectionContextSource();

    private void OnEnable()
    {
        _graphNameInputField.onEndEdit.AddListener(_runtimeGraphEditor.SetGraphDocumentName);
        _graphNameInputField.onSelect.AddListener(_ => _runtimeGraphEditor.ElementSelectionProvider.Select(this));
        _graphNameInputField.onDeselect.AddListener(_ => Unselect());
    }

    private void OnDisable()
    {
        _graphNameInputField.onEndEdit.RemoveListener(_runtimeGraphEditor.SetGraphDocumentName);
        _graphNameInputField.onSelect.RemoveAllListeners();
        _graphNameInputField.onDeselect.RemoveAllListeners();
    }

    public void Unselect()
    {
        _runtimeGraphEditor.ElementSelectionProvider.Unselect(this);
    }
}
