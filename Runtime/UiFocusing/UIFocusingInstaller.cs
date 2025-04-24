using UnityEngine;

namespace UI.Focusing
{
    public class UIFocusingInstaller : MonoBehaviour
    {
        [SerializeField] private DimmingObject _dimmingObjectPrefab;

        private void Awake()
        {
            new UIFocusingSystem(new HotkeyHandler(), new DimmingHandler(_dimmingObjectPrefab), new SelectionHandler());
        }
    }
}
