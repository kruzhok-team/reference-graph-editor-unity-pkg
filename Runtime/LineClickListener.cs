using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент, слушающий за кликом по линиям
    /// </summary>
    public class LineClickListener : MonoBehaviour
    {
        [Min(0)] [SerializeField] private float _extraHitBoxThickness;
        private readonly Dictionary<EdgeLine, EdgeView> _edgeByLine = new();

        [SerializeField]
        private RectTransform _linePlane;

        /// <summary>
        /// Добавляет линию и соответствующее ребро
        /// </summary>
        /// <param name="line">Линия</param>
        /// <param name="edge">Соответствующее представление ребра</param>
        public void AddLine(EdgeLine line, EdgeView edge)
        {
            _edgeByLine[line] = edge;
        }

        /// <summary>
        /// Удаляет линию
        /// </summary>
        /// <param name="line">Линия</param>
        public void RemoveLine(EdgeLine line)
        {
            _edgeByLine.Remove(line);
        }

        private void Update()
        {
            if (!Input.GetMouseButtonUp(0))
            {
                return;
            }

            Vector3 mousePosition = Input.mousePosition;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_linePlane, mousePosition, null,
                out Vector3 worldPoint);
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = mousePosition };
            foreach (EdgeLine line in _edgeByLine.Keys)
            {
                Vector2 localPosition = line.transform.InverseTransformPoint(worldPoint);
                if (line.ContainsPoint(localPosition, _extraHitBoxThickness))
                {
                    if (!IsPointerOverGraphElement(eventData))
                    {
                        _edgeByLine[line].Select(false);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private bool IsPointerOverGraphElement(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (RaycastResult result in results)
            {
                if (result.gameObject.GetComponentInParent<NodeView>() != null ||
                    result.gameObject.GetComponentInParent<EdgeView>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
