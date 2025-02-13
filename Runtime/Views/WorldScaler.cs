using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент для установки желаемого глобального масштаба объекта 
    /// </summary>
    public class WorldScaler : MonoBehaviour
    {
        [SerializeField] private Vector3 _targetLossyScale;

        private void Awake()
        {
            AdjustScale();
        }

        private void Update()
        {
            AdjustScale();
        }

        private void AdjustScale()
        {
            Vector3 currentLossyScale = transform.lossyScale.x * Vector3.one;

            if (!Mathf.Approximately(currentLossyScale.x, _targetLossyScale.x))
            {
                Vector3 parentLossyScale = transform.parent.lossyScale.x * Vector3.one;

                transform.localScale = Vector3.one * _targetLossyScale.x / parentLossyScale.x;
            }
        }
    }
}

