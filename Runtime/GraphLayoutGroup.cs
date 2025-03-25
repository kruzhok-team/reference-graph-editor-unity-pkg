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
            
            GetGraphCornersInternal(out float left, out float top, out float right, out float bottom, false);

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
            GetGraphCornersInternal(out left, out top, out right, out bottom, true);
        }

        private void GetGraphCornersInternal(out float left, out float top, out float right, out float bottom, bool ignoreLines)
        {
            left = float.MaxValue;
            top = float.MinValue;
            right = float.MinValue;
            bottom = float.MaxValue;
            
            Vector3[] corners = new Vector3[4];
            
            if (rectChildren.Count == 0)
            {
                left = 0;
                top = 0;
                right = 0;
                bottom = 0;
            }

            foreach (RectTransform rect in rectChildren)
            {
                Vector3 position = rect.localPosition;
                rect.GetLocalCorners(corners);
                left = Mathf.Min(corners[1].x + position.x, left);
                top = Mathf.Max(corners[1].y + position.y, top);
                right = Mathf.Max(corners[3].x + position.x, right);
                bottom = Mathf.Min(corners[3].y + position.y, bottom);
            }

            if (!ignoreLines)
            {
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent(out EdgeView edgeView))
                    {
                        edgeView.DrawLine();
                        Bounds localBounds = new Bounds(transform.InverseTransformPoint(edgeView.Line.Bounds.center),
                            transform.InverseTransformVector(edgeView.Line.Bounds.size));
                        left = Mathf.Min(localBounds.center.x - localBounds.extents.x, left);
                        top = Mathf.Max(localBounds.center.y + localBounds.extents.y, top);
                        right = Mathf.Max(localBounds.center.x + localBounds.extents.x, right);
                        bottom = Mathf.Min(localBounds.center.y - localBounds.extents.y, bottom);
                    }
                }
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
            
            foreach (Transform child in transform)
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
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(lineBounds.center, lineBounds.size);
            Bounds commonBounds = new Bounds(position, size);

            if (lineBounds.extents != Vector3.zero)
            {
                commonBounds.Encapsulate(lineBounds);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(commonBounds.center, commonBounds.size);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(position, size);
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
