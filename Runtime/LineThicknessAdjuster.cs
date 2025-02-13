using UnityEngine;
using UnityEngine.UI.Extensions;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент, настраивающий глобальную толщину для всех линий
    /// </summary>
    [RequireComponent(typeof(UILineRenderer))]
    public class LineThicknessAdjuster : MonoBehaviour
    {
        [SerializeField] private float _extraThickness = 1.0f;
        private UILineRenderer _lineRenderer;

        private float _defaultThickness;
        private float _thickness;

        private void Awake()
        {
            _lineRenderer = GetComponent<UILineRenderer>();
            _defaultThickness = _lineRenderer.LineThickness;
        }

        private void Update()
        {
            float currentThickness = _defaultThickness / transform.lossyScale.x + _extraThickness;
            if (!Mathf.Approximately(_thickness, currentThickness))
            {
                _thickness = currentThickness;
                _lineRenderer.LineThickness = currentThickness;
            }
        }
    }
}
