using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Talent.GraphEditor.Unity.Runtime
{
    public class GraphLayoutGroup : LayoutGroup
    {
        [SerializeField]
        private LayoutElement _layoutElement;
        
        public NodeView ParentNode { get; set; }

        public override void SetLayoutHorizontal() {}

        public override void CalculateLayoutInputVertical()
        {
            if (rectChildren.Count <= 0 || _layoutElement == null)
            {
                return;
            }

            List<Vector2> childrenWorldPos = new();
            
            foreach (var child in rectChildren)
            {
                childrenWorldPos.Add(child.position);
            }

            GetGraphCorners(out float left, out float top, out float right, out float bottom);

            Vector3 position = new Vector3((right + left) / 2, (top + bottom) / 2);
            Vector2 size = new Vector2(Mathf.Abs(right - left), Mathf.Abs(bottom - top));

            if (ParentNode != null)
            {
                ParentNode.transform.position += position;

                if (ParentNode.TryGetComponent(out NodeView nodeView) && nodeView.VisualData != null)
                {
                    nodeView.VisualData.Position = ParentNode.transform.localPosition;
                }

                LayoutRebuilder.MarkLayoutForRebuild(ParentNode.transform.parent as RectTransform);
            }

            _layoutElement.preferredWidth = size.x / transform.lossyScale.x;
            _layoutElement.preferredHeight = size.y / transform.lossyScale.y;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                rectChildren[i].position = childrenWorldPos[i];

                if (rectChildren[i].TryGetComponent(out NodeView nodeView) && nodeView.VisualData != null)
                {
                    nodeView.VisualData.Position = rectChildren[i].localPosition;
                }
                else if (rectChildren[i].TryGetComponent(out EdgeView edgeView) && edgeView.VisualData != null)
                {
                    edgeView.VisualData.Position = rectChildren[i].localPosition;
                }
            }

            LayoutRebuilder.MarkLayoutForRebuild(_layoutElement.transform.parent as RectTransform);
        }

        public override void SetLayoutVertical() {}

        public void GetGraphCorners(out float left, out float top, out float right, out float bottom)
        {
            Vector3[] corners = new Vector3[4];

            left = float.MaxValue;
            top = float.MinValue;
            right = float.MinValue;
            bottom = float.MaxValue;

            foreach (RectTransform rect in rectChildren)
            {
                Vector3 position = rect.localPosition;
                rect.GetLocalCorners(corners);
                left = Mathf.Min(corners[1].x + position.x, left);
                top = Mathf.Max(corners[1].y + position.y, top);
                right = Mathf.Max(corners[3].x + position.x, right);
                bottom = Mathf.Min(corners[3].y + position.y, bottom);
            }

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out EdgeView edgeView))
                {
                    edgeView.DrawLine();
                    Vector2 localMin = transform.InverseTransformPoint(edgeView.Line.Bounds.min);
                    Vector2 localMax = transform.InverseTransformPoint(edgeView.Line.Bounds.max);
                    left = Mathf.Min(localMin.x, left);
                    top = Mathf.Max(localMax.y, top);
                    right = Mathf.Max(localMax.x, right);
                    bottom = Mathf.Min(localMin.y, bottom);
                }
            }

            if (left == float.MaxValue)
            {
                left = 0;
            }

            if (top == float.MinValue)
            {
                top = 0;
            }

            if (right == float.MinValue)
            {
                right = 0;
            }

            if (bottom == float.MaxValue)
            {
                bottom = 0;
            }

            left -= padding.left;
            top += padding.top;
            right += padding.right;
            bottom -= padding.bottom;

            left *= transform.lossyScale.x;
            top *= transform.lossyScale.y;
            right *= transform.lossyScale.x;
            bottom *= transform.lossyScale.y;
        }

        public int GetRectChildrenCount()
        {
            return rectChildren.Count;
        }

        private void OnDrawGizmosSelected()
        {
            Bounds lineBounds = new Bounds();
            
            foreach (var child in rectChildren)
            {
                if (child.TryGetComponent(out EdgeView edgeView))
                {
                    if (lineBounds.extents == Vector3.zero)
                    {
                        lineBounds = edgeView.Line.Bounds;
                    }
                    else
                    {
                        lineBounds.Encapsulate(edgeView.Line.Bounds);
                    }
                }
            }
            
            GetGraphCorners(out float left, out float top, out float right, out float bottom);

            Vector2 position = (Vector2)transform.position + new Vector2((right + left) / 2, (top + bottom) / 2);
            Vector2 size = new Vector2(Mathf.Abs(right - left), Mathf.Abs(bottom - top));
            Bounds bounds = new Bounds(position, size);
            bounds.Encapsulate(lineBounds);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(position, size);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.DrawSphere(position, 10f);
            Gizmos.DrawSphere((Vector2)transform.position + new Vector2(left, top), 10f);
            Gizmos.DrawSphere((Vector2)transform.position + new Vector2(left, bottom), 10f);
            Gizmos.DrawSphere((Vector2)transform.position + new Vector2(right, top), 10f);
            Gizmos.DrawSphere((Vector2)transform.position + new Vector2(right, bottom), 10f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere((Vector2)transform.position + new Vector2(left, (top + bottom) / 2), 10f);
            Gizmos.DrawSphere((Vector2)transform.position + new Vector2(right, (top + bottom) / 2), 10f);
        }
    }
}
