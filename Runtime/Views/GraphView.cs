using Talent.GraphEditor.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, представляющий представление графа
    /// </summary>
    public class GraphView : MonoBehaviour, IGraphView
    {
        /// <summary>
        /// Родительский узел
        /// </summary>
        public NodeView ParentNode { get; set; }

        private void LateUpdate()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }
    }
}
