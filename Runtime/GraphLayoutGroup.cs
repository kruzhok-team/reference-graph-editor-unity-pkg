using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    public class GraphLayoutGroup : LayoutGroup
    {
        public RectTransform moveTransform;
        public RectTransform sizeTransform;

        public override void SetLayoutHorizontal() { }

        public override void CalculateLayoutInputVertical()
        {
            if (rectChildren.Count <= 0 || moveTransform == null || sizeTransform == null)
            {
                return;
            }

            List<Vector2> childrenWorldPos = new();

            foreach (var child in rectChildren)
            {
                childrenWorldPos.Add(child.position);
            }

            GetGraphCorners(out float left, out float top, out float right, out float bottom);

            Vector3 position = Vector3.zero;
            Vector2 size = Vector2.zero;

            if (rectChildren.Count > 0)
            {
                position = new Vector3((right + left) / 2, (top + bottom) / 2);
                size = new Vector2(Mathf.Abs(right - left), Mathf.Abs(bottom - top));
            }

            moveTransform.position += position;

            if (moveTransform.TryGetComponent<NodeView>(out var nodeView) && nodeView.VisualData != null)
            {
                nodeView.VisualData.Position = moveTransform.localPosition;
            }

            LayoutRebuilder.MarkLayoutForRebuild(moveTransform.parent.transform as RectTransform);

            LayoutElement layoutElement = sizeTransform.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = size.x / transform.lossyScale.x;
                layoutElement.preferredHeight = size.y / transform.lossyScale.y;
            }

            for (int i = 0; i < rectChildren.Count; i++)
            {
                rectChildren[i].position = childrenWorldPos[i];

                if (rectChildren[i].TryGetComponent<NodeView>(out nodeView) && nodeView.VisualData != null)
                {
                    nodeView.VisualData.Position = rectChildren[i].localPosition;
                }
                else if (rectChildren[i].TryGetComponent<EdgeView>(out EdgeView edgeView) && edgeView.VisualData != null)
                {
                    edgeView.VisualData.Position = rectChildren[i].localPosition;
                }
            }

            LayoutRebuilder.MarkLayoutForRebuild(sizeTransform.parent.transform as RectTransform);
        }

        public override void SetLayoutVertical() { }

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

        private void OnDrawGizmos()
        {
            GetGraphCorners(out float left, out float top, out float right, out float bottom);

            Vector2 position = (Vector2)transform.position + new Vector2((right + left) / 2, (top + bottom) / 2);
            Vector2 size = new Vector2(Mathf.Abs(right - left), Mathf.Abs(bottom - top));

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
