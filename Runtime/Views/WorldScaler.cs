using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент для установки желаемого глобального масштаба объекта 
    /// </summary>
    public class WorldScaler : MonoBehaviour
    {
        [SerializeField] private Vector3 _targetLossyScale = Vector3.one;

        private Canvas _parentCanvas;

        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();

            AdjustScale();
        }

        private void OnEnable()
        {
            AdjustScale();
        }

        private void Update()
        {
            AdjustScale();
        }

        private void AdjustScale()
        {
            float currentLossy = transform.lossyScale.x;
            float desired = _targetLossyScale.x;

            if (!Mathf.Approximately(currentLossy, desired))
            {
                float parentLossy = 1f;

                if (transform.parent != null)
                {
                    parentLossy = transform.parent.lossyScale.x;
                }

                float canvasScale = 1f;

                if (_parentCanvas != null)
                {
                    canvasScale = _parentCanvas.scaleFactor;
                }

                float newLocal = desired / (parentLossy / canvasScale);
                transform.localScale = Vector3.one * newLocal;
            }
        }
    }
}

