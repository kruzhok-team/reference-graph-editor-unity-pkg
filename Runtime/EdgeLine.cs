using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, строящий линию между элементами
    /// </summary>
    [RequireComponent(typeof(UILineRenderer))]
    public class EdgeLine : MonoBehaviour
    {
        private static readonly Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        private static readonly DirectionsGroup[] directionsGroups;

        private const float Epsilon = 1e-3f;
        /// <summary>
        /// Стартовая точка линии
        /// </summary>
        public Vector2 StartPoint { get; private set; }
        /// <summary>
        /// Конечная точка линии
        /// </summary>
        public Vector2 EndPoint { get; private set; }
        
        /// <summary>
        /// Границы линии в глобальных координатах
        /// </summary>
        public Bounds Bounds { get; private set; }

        [SerializeField] private UILineRenderer _lineRenderer;
        [SerializeField] private float _cornerOffset = 35;
        [SerializeField] private float _edgeDistanceThreshold = 50;
        [SerializeField] private UILineRenderer _edgeLineBoarders;

        [Header("Arrow")]
        [SerializeField] private bool _drawArrow;

        [SerializeField] private UILineRenderer _arrowRenderer;
        [SerializeField] private float _arrowSize = 15;
        [SerializeField] private float _arrowAngle = 45;
        [Header("Angles")]
        [SerializeField] private bool _isAnglesRounded = true;
        [SerializeField] private float _radius = 15;
        [Min(1)]
        [SerializeField] private int _stepsPerUnit = 1;

        private Transform _container;

        private Bounds _previousSourceBounds;
        private Bounds _previousTargetBounds;
        private Bounds _previousEdgeBounds;
        private Vector2 _previousSourceDirection;
        private Vector2 _previousMidEnterDirection;
        private Vector2 _previousMidExitDirection;
        private Vector2 _previousTargetDirection;

        static EdgeLine()
        {
            directionsGroups = new DirectionsGroup[64];

            for (int i = 0; i < directionsGroups.Length; i++)
            {
                directionsGroups[i] = new DirectionsGroup(directions[i % 4], directions[i / 16], -directions[i / 16],
                    directions[i / 4 % 4]);
            }
        }

        /// <summary>
        /// Инициализирует линию
        /// </summary>
        /// <param name="container">Контейнер для линии</param>
        public void Init(Transform container)
        {
            _container = container;
            transform.SetParent(_container, false);
        }

        /// <summary>
        /// Устанавливает цвет линии
        /// </summary>
        /// <param name="color">Желаемый цвет линии</param>
        public void SetColor(Color color)
        {
            _lineRenderer.color = color;
            _arrowRenderer.color = color;
            _edgeLineBoarders.color = color;
        }

        /// <summary>
        /// Проверяет, содержит ли линия точку
        /// </summary>
        /// <param name="position">Позиция точки</param>
        /// <param name="extraThickness">Дополнительная толщина линии</param>
        /// <returns>true, если линия содержит точку, иначе false</returns>
        public bool ContainsPoint(Vector2 position, float extraThickness = 0)
        {
            List<Vector2[]> segments = _lineRenderer.Segments;

            if (segments == null)
            {
                return false;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                for (int j = 0; j < segments[i].Length - 1; j++)
                {
                    Bounds segment =
                        GetBoundsByTwoPoints(segments[i][j], segments[i][j + 1], _lineRenderer.LineThickness + extraThickness);

                    if (segment.Contains(position))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Рисует линию через два узла и ребро
        /// </summary>
        /// <param name="source">Стартовый узел</param>
        /// <param name="edge">Ребро</param>
        /// <param name="target">Конечный узел</param>
        public void Draw(NodeView source, RectTransform edge, NodeView target)
        {
            Bounds sourceBounds = GetBoundsInContainerSpace((RectTransform)source.transform);
            Bounds targetBounds = GetBoundsInContainerSpace((RectTransform)target.transform);
            Bounds edgeBounds = GetBoundsInContainerSpace(edge);

            if (sourceBounds == _previousSourceBounds && edgeBounds == _previousEdgeBounds &&
                targetBounds == _previousTargetBounds)
            {
                return;
            }

            (Vector2[] firstPath, Vector2[] secondPath) = GetPaths(sourceBounds, edgeBounds, targetBounds);

            Bounds bounds;
            
            if (source.IsDescendant(target))
            {
                secondPath = new[]
                {
                    secondPath[0], (Vector2)targetBounds.ClosestPoint(secondPath[1])
                };
                
                bounds = RecalculateSegmentBounds(firstPath);
            }

            else if (target.IsDescendant(source))
            {
                firstPath = new[]
                {
                    (Vector2)sourceBounds.ClosestPoint(firstPath[^2]), firstPath[^1]
                };
                
                bounds = RecalculateSegmentBounds(secondPath);
            }
            else
            {
                bounds = RecalculateSegmentBounds(firstPath);
                
                if (bounds.extents == Vector3.zero)
                {
                    bounds = RecalculateSegmentBounds(firstPath);
                }
                else
                {
                    bounds.Encapsulate(RecalculateSegmentBounds(secondPath));
                }
            }

            Bounds = bounds;

            DrawLines(firstPath, secondPath);
        }

        /// <summary>
        /// Рисует линию через узел, ребро и точку на экране
        /// </summary>
        /// <param name="source">Стартовый узел</param>
        /// <param name="edge">Ребро</param>
        /// <param name="mousePosition">Точка на экране</param>
        public void Draw(NodeView source, RectTransform edge, Vector2 mousePosition)
        {
            Bounds sourceBounds = GetBoundsInContainerSpace((RectTransform)source.transform);
            Bounds edgeBounds = GetBoundsInContainerSpace(edge);
            Vector2 mousePositionInContainerSpace = _container.InverseTransformPoint(mousePosition);
            Bounds targetBounds = new Bounds(mousePositionInContainerSpace, Vector3.zero);

            if (sourceBounds == _previousSourceBounds && edgeBounds == _previousEdgeBounds &&
                targetBounds == _previousTargetBounds)
            {
                return;
            }

            (Vector2[] firstPath, Vector2[] secondPath) = GetPaths(sourceBounds, edgeBounds, targetBounds);
            DrawLines(firstPath, secondPath);
        }

        /// <summary>
        /// Рисует линию через точку на экране, ребро и узел.
        /// </summary>
        /// <param name="mousePosition">Точка на экране</param>
        /// <param name="edge">Ребро</param>
        /// <param name="target">Конечный узел</param>
        public void Draw(Vector2 mousePosition, RectTransform edge, NodeView target)
        {
            Vector2 mousePositionInContainerSpace = _container.InverseTransformPoint(mousePosition);
            Bounds sourceBounds = new Bounds(mousePositionInContainerSpace, Vector3.zero);
            Bounds edgeBounds = GetBoundsInContainerSpace(edge);
            Bounds targetBounds = GetBoundsInContainerSpace((RectTransform)target.transform);

            if (sourceBounds == _previousSourceBounds && edgeBounds == _previousEdgeBounds &&
                targetBounds == _previousTargetBounds)
            {
                return;
            }

            (Vector2[] firstPath, Vector2[] secondPath) = GetPaths(sourceBounds, edgeBounds, targetBounds);
            DrawLines(firstPath, secondPath);
        }

        private (Vector2[] firstPath, Vector2[] secondPaths) GetPaths(Bounds sourceBounds, Bounds edgeBounds,
            Bounds targetBounds)
        {
            _previousSourceBounds = sourceBounds;
            _previousTargetBounds = targetBounds;
            _previousEdgeBounds = edgeBounds;

            if (edgeBounds.size != Vector3.zero)
            {
                Bounds enterNodeCommonBounds = sourceBounds;
                enterNodeCommonBounds.Encapsulate(edgeBounds);
                Bounds exitNodeCommonBounds = targetBounds;
                exitNodeCommonBounds.Encapsulate(edgeBounds);
                DirectionsGroup[] groups = CalculateSortedGroups(sourceBounds, edgeBounds, targetBounds);
                (Vector2[] first, Vector2[] second) fallbackPath = default;

                foreach (DirectionsGroup group in groups)
                {
                    ConnectionElement source = new ConnectionElement(sourceBounds, group.Start, _cornerOffset);
                    ConnectionElement midEnter = new ConnectionElement(edgeBounds, group.MidEnter);
                    ConnectionElement midExit = new ConnectionElement(edgeBounds, group.MidExit);
                    ConnectionElement target = new ConnectionElement(targetBounds, group.End, _cornerOffset);
                    Vector2[] firstPath = BuildPath(source, midEnter, enterNodeCommonBounds);
                    Vector2[] secondPath = BuildPath(midExit, target, exitNodeCommonBounds);

                    if (IsPathsIntersects(firstPath, secondPath))
                    {
                        continue;
                    }

                    if (fallbackPath == default)
                    {
                        fallbackPath = (firstPath, secondPath);
                    }

                    if (IsLineIntersectElements(firstPath, secondPath, source, target))
                    {
                        continue;
                    }

                    ConnectionElement previousSource = new ConnectionElement(sourceBounds, _previousSourceDirection, _cornerOffset);
                    ConnectionElement previousMidEnter = new ConnectionElement(edgeBounds, _previousMidEnterDirection);
                    ConnectionElement previousMidExit = new ConnectionElement(edgeBounds, _previousMidExitDirection);
                    ConnectionElement previousTarget = new ConnectionElement(targetBounds, _previousTargetDirection, _cornerOffset);
                    Vector2[] previousFirstPath = BuildPath(previousSource, previousMidEnter, enterNodeCommonBounds);
                    Vector2[] previousSecondPath = BuildPath(previousMidExit, previousTarget, exitNodeCommonBounds);
                    int previousPathCornerCount = CalculateCornerCount(previousSource, previousMidEnter) + CalculateCornerCount(previousMidExit, previousTarget);
                    int pathCornerCount = CalculateCornerCount(source, midEnter) + CalculateCornerCount(midExit, target);

                    if (!IsPathsIntersects(previousFirstPath, previousSecondPath) &&
                        !IsLineIntersectElements(previousFirstPath, previousSecondPath, source, target) && previousPathCornerCount <= pathCornerCount)
                    {
                        firstPath = previousFirstPath;
                        secondPath = previousSecondPath;
                        return (firstPath, secondPath);
                    }

                    _previousSourceDirection = source.Direction;
                    _previousMidEnterDirection = midEnter.Direction;
                    _previousMidExitDirection = midExit.Direction;
                    _previousTargetDirection = target.Direction;
                    break;
                }

                return (fallbackPath.first, fallbackPath.second);
            }
            else
            {
                Bounds commonBounds = sourceBounds;
                commonBounds.Encapsulate(targetBounds);
                DirectionsGroup group = CalculateSortedGroups(sourceBounds, edgeBounds, targetBounds)[0];
                ConnectionElement source = new ConnectionElement(sourceBounds, group.Start);
                ConnectionElement target = new ConnectionElement(targetBounds, group.End, _cornerOffset);
                Vector2[] path = BuildPath(source, target, commonBounds);
                return (path, Array.Empty<Vector2>());
            }
        }

        private bool IsLineIntersectElements(Vector2[] firstPath, Vector2[] secondPath, ConnectionElement source, ConnectionElement target)
        {
            return IsPathIntersectElement(firstPath, source) || IsPathIntersectElement(firstPath, target) ||
                IsPathIntersectElement(secondPath, source) || IsPathIntersectElement(secondPath, target);
        }

        private void DrawLine(Vector2[] path)
        {
            _arrowRenderer.Points = _drawArrow ? GetArrowPoints(path) : Array.Empty<Vector2>();

            if (_isAnglesRounded)
            {
                path = AddRoundedAngles(path);
            }

            _lineRenderer.Segments = new List<Vector2[]> { path };
            StartPoint = path[0];
            EndPoint = path[^1];
        }

        private void DrawLines(Vector2[] firstPath, Vector2[] secondPath)
        {
            if (secondPath.Length == 0)
            {
                DrawLine(firstPath);
                return;
            }

            _arrowRenderer.Points = _drawArrow ? GetArrowPoints(secondPath) : Array.Empty<Vector2>();

            if (_isAnglesRounded)
            {
                firstPath = AddRoundedAngles(firstPath);
                secondPath = AddRoundedAngles(secondPath);
            }

            _lineRenderer.Segments = new List<Vector2[]> { firstPath, secondPath };
            AddBoarders(firstPath, secondPath);
            StartPoint = firstPath[0];
            EndPoint = secondPath[^1];
        }

        private Bounds RecalculateSegmentBounds(Vector2[] segment)
        {
            if (segment.Length == 0)
            {
                return default;
            }
            
            Vector2 min = segment[0];
            Vector2 max = segment[0];

            for (int i = 0; i < segment.Length; i++)
            {
                min = Vector2.Min(min, segment[i]);
                max = Vector2.Max(max, segment[i]);
            }
            
            Vector2 containerMin = _container.TransformPoint(min);
            Vector2 containerMax = _container.TransformPoint(max);
            Bounds segmentBounds = new Bounds { min = containerMin, max = containerMax };

            return segmentBounds;
        }

        private Bounds GetBoundsInContainerSpace(RectTransform element)
        {
            Vector2 localRectCenter = GetCenterInContainerSpace(element);
            Vector2 worldRectSize = element.TransformVector(element.rect.size);
            Vector2 localRectSize = _container.InverseTransformVector(worldRectSize);
            Bounds bounds = new Bounds(localRectCenter, localRectSize);
            return bounds;
        }

        private Vector2 GetCenterInContainerSpace(RectTransform element)
        {
            Vector2 worldRectCenter = element.TransformPoint(element.rect.center);
            Vector2 localRectCenter = _container.InverseTransformPoint(worldRectCenter);
            return localRectCenter;
        }

        private DirectionsGroup[] CalculateSortedGroups(Bounds sourceBounds, Bounds edgeBounds, Bounds targetBounds)
        {
            using (UnityEngine.Pool.ListPool<(DirectionsGroup, int, int)>.Get(
                       out List<(DirectionsGroup group, int, int)> tempList))
            {
                if (edgeBounds.size != Vector3.zero)
                {
                    foreach (DirectionsGroup group in directionsGroups)
                    {
                        ConnectionElement source = new ConnectionElement(sourceBounds, group.Start, _cornerOffset);
                        ConnectionElement midEnter = new ConnectionElement(edgeBounds, group.MidEnter);
                        ConnectionElement midExit = new ConnectionElement(edgeBounds, group.MidExit);
                        ConnectionElement target = new ConnectionElement(targetBounds, group.End, _cornerOffset);
                        int fromStartCornerCount = CalculateCornerCount(source, midEnter);
                        int fromEndCornerCount = CalculateCornerCount(midExit, target);
                        tempList.Add((group, fromStartCornerCount, fromEndCornerCount));
                    }
                }
                else
                {
                    foreach (DirectionsGroup group in directionsGroups)
                    {
                        ConnectionElement source = new ConnectionElement(sourceBounds, group.Start);
                        ConnectionElement target = new ConnectionElement(targetBounds, group.End, _cornerOffset);
                        int totalCornerCount = CalculateCornerCount(target, source);
                        tempList.Add((group, totalCornerCount, 0));
                    }
                }

                tempList.Sort(PathComparison);
                DirectionsGroup[] result = tempList.Select(t => t.group).ToArray();
                return result;
            }
        }

        private int PathComparison((DirectionsGroup group, int startCornerCount, int endCornerCount) firstLine,
            (DirectionsGroup group, int startCornerCount, int fromEndCornerCount) secondLine)
        {
            int firstTotalCount = firstLine.startCornerCount + firstLine.endCornerCount;
            int secondTotalCount = secondLine.startCornerCount + secondLine.fromEndCornerCount;
            return firstTotalCount.CompareTo(secondTotalCount);
        }

        private Vector2[] BuildPath(ConnectionElement source, ConnectionElement target, Bounds commonBounds)
        {
            Bounds sourceDirectAccessArea = GetDirectAccessArea(source, commonBounds);

            Bounds targetDirectAccessArea =
                GetDirectAccessArea(target, commonBounds);

            Vector2 startPoint = GetClosestPointOnBorder(source, target.Bounds.center);
            Vector2 endPoint = GetClosestPointOnBorder(target, source.Bounds.center);

            float dotDirections = Vector2.Dot(source.Direction, target.Direction);

            if (Mathf.Abs(dotDirections) < Epsilon)
            {
                Vector2 p2 = default;

                if (sourceDirectAccessArea.Intersects(targetDirectAccessArea))
                {
                    if (source.Direction[source.MainAxisIndex] > 0)
                    {
                        endPoint[source.MainAxisIndex] = Mathf.Max(endPoint[source.MainAxisIndex],
                            startPoint[source.MainAxisIndex] + _edgeDistanceThreshold);
                    }
                    else
                    {
                        endPoint[source.MainAxisIndex] = Mathf.Min(endPoint[source.MainAxisIndex],
                            startPoint[source.MainAxisIndex] - _edgeDistanceThreshold);
                    }

                    if (target.Direction[target.MainAxisIndex] > 0)
                    {
                        startPoint[target.MainAxisIndex] = Mathf.Max(startPoint[target.MainAxisIndex],
                            endPoint[target.MainAxisIndex] + _edgeDistanceThreshold);
                    }
                    else
                    {
                        startPoint[target.MainAxisIndex] = Mathf.Min(startPoint[target.MainAxisIndex],
                            endPoint[target.MainAxisIndex] - _edgeDistanceThreshold);
                    }

                    p2 = source.MainAxisIndex == 1
                        ? new Vector2(startPoint.x, endPoint.y)
                        : new Vector2(endPoint.x, startPoint.y);

                    return new[] { startPoint, p2, endPoint };
                }

                p2[source.MainAxisIndex] = commonBounds.center[source.MainAxisIndex] +
                    source.Direction[source.MainAxisIndex] *
                    (commonBounds.extents[source.MainAxisIndex] + _edgeDistanceThreshold);

                p2[source.SubAxisIndex] = startPoint[source.SubAxisIndex];
                Vector2 p4 = default;

                p4[target.MainAxisIndex] = commonBounds.center[target.MainAxisIndex] +
                    target.Direction[target.MainAxisIndex] *
                    (commonBounds.extents[target.MainAxisIndex] + _edgeDistanceThreshold);

                p4[target.SubAxisIndex] = endPoint[target.SubAxisIndex];
                Vector2 p3 = source.MainAxisIndex == 1 ? new Vector2(p4.x, p2.y) : new Vector2(p2.x, p4.y);
                return new[] { startPoint, p2, p3, p4, endPoint };
            }

            if (Mathf.Abs(dotDirections - 1) < Epsilon)
            {
                float sourceSubAxisLength = source.Bounds.size[source.SubAxisIndex];
                float targetSubAxisLength = target.Bounds.size[target.SubAxisIndex];
                float maxSubAxisLength = Mathf.Max(sourceSubAxisLength, targetSubAxisLength);
                float lengthThreshold = maxSubAxisLength + _cornerOffset;
                float commonBoundsLength = commonBounds.size[source.SubAxisIndex];
                Vector2 p2 = default;
                Vector2 p3 = default;

                if (commonBoundsLength < lengthThreshold)
                {
                    p2 = startPoint + source.Direction * _edgeDistanceThreshold;
                    Vector2 p5 = endPoint + target.Direction * _edgeDistanceThreshold;

                    float subAxisOffset = commonBounds.center[source.SubAxisIndex] +
                        (commonBounds.extents[source.SubAxisIndex] + _edgeDistanceThreshold) *
                        source.Direction[source.SubAxisIndex];

                    p3[source.MainAxisIndex] = p2[source.MainAxisIndex];
                    p3[source.SubAxisIndex] = subAxisOffset;
                    Vector2 p4 = default;
                    p4[target.MainAxisIndex] = p5[target.MainAxisIndex];
                    p4[target.SubAxisIndex] = subAxisOffset;
                    return new[] { startPoint, p2, p3, p4, p5, endPoint };
                }

                if (commonBounds.size[source.SubAxisIndex] <
                    source.Bounds.size[source.SubAxisIndex] + target.Bounds.size[target.SubAxisIndex])
                {
                    (float left, float right) sourceMovementSegment = GetMovementSegment(source);
                    (float left, float right) targetMovementSegment = GetMovementSegment(target);
                    float minValidDistance = _edgeDistanceThreshold + source.Bounds.extents[source.SubAxisIndex];

                    if (sourceMovementSegment.left + minValidDistance < targetMovementSegment.right)
                    {
                        if (sourceMovementSegment.right + minValidDistance < targetMovementSegment.right)
                        {
                            startPoint[source.SubAxisIndex] = sourceMovementSegment.right;

                            endPoint[target.SubAxisIndex] = Mathf.Max(targetMovementSegment.left,
                                sourceMovementSegment.right + minValidDistance);
                        }
                        else
                        {
                            float mid = (sourceMovementSegment.left + sourceMovementSegment.right) / 2;

                            while (Mathf.Abs(sourceMovementSegment.left - sourceMovementSegment.right) > Epsilon)
                            {
                                mid = (sourceMovementSegment.left + sourceMovementSegment.right) / 2;

                                if (mid + minValidDistance < targetMovementSegment.right)
                                {
                                    sourceMovementSegment.left = mid;
                                }
                                else
                                {
                                    sourceMovementSegment.right = mid;
                                }
                            }

                            startPoint[source.SubAxisIndex] = mid;

                            endPoint[target.SubAxisIndex] = Mathf.Max(targetMovementSegment.left,
                                mid + minValidDistance);
                        }
                    }

                    if (sourceMovementSegment.right - minValidDistance > targetMovementSegment.left)
                    {
                        if (sourceMovementSegment.left - minValidDistance > targetMovementSegment.left)
                        {
                            startPoint[source.SubAxisIndex] = sourceMovementSegment.left;

                            endPoint[target.SubAxisIndex] = Mathf.Min(targetMovementSegment.right,
                                sourceMovementSegment.left - minValidDistance);
                        }
                        else
                        {
                            float mid = (sourceMovementSegment.left + sourceMovementSegment.right) / 2;

                            while (Mathf.Abs(sourceMovementSegment.left - sourceMovementSegment.right) > Epsilon)
                            {
                                mid = (sourceMovementSegment.left + sourceMovementSegment.right) / 2;

                                if (mid - minValidDistance > targetMovementSegment.left)
                                {
                                    sourceMovementSegment.left = mid;
                                }
                                else
                                {
                                    sourceMovementSegment.right = mid;
                                }
                            }

                            startPoint[source.SubAxisIndex] = mid;
                            endPoint[target.SubAxisIndex] = Mathf.Min(targetMovementSegment.left, mid + minValidDistance);
                        }
                    }
                }

                p2[source.MainAxisIndex] = commonBounds.center[source.MainAxisIndex] +
                    (commonBounds.extents[source.MainAxisIndex] + _edgeDistanceThreshold) *
                    source.Direction[source.MainAxisIndex];

                p2[source.SubAxisIndex] = startPoint[source.SubAxisIndex];
                p3[source.MainAxisIndex] = p2[source.MainAxisIndex];
                p3[source.SubAxisIndex] = endPoint[source.SubAxisIndex];

                return new[] { startPoint, p2, p3, endPoint };
            }
            else
            {
                if (sourceDirectAccessArea.Intersects(targetDirectAccessArea))
                {
                    return new[] { startPoint, endPoint };
                }

                float distance = Mathf.Abs(startPoint[source.MainAxisIndex] - endPoint[source.MainAxisIndex]);
                Vector2 p2 = default;

                p2[source.MainAxisIndex] = startPoint[source.MainAxisIndex] +
                    source.Direction[source.MainAxisIndex] * distance / 2;

                p2[source.SubAxisIndex] = startPoint[source.SubAxisIndex];
                p2 = startPoint + source.Direction * distance / 2;

                Vector2 p3 = default;
                p3[target.MainAxisIndex] = p2[target.MainAxisIndex];
                p3[target.SubAxisIndex] = endPoint[target.SubAxisIndex];
                return new[] { startPoint, p2, p3, endPoint };
            }
        }

        private (float left, float right) GetMovementSegment(ConnectionElement connectionElement)
        {
            return (connectionElement.Bounds.center[connectionElement.SubAxisIndex] -
                connectionElement.Bounds.extents[connectionElement.SubAxisIndex] + connectionElement.CornerOffset,
                connectionElement.Bounds.center[connectionElement.SubAxisIndex] +
                connectionElement.Bounds.extents[connectionElement.SubAxisIndex] - connectionElement.CornerOffset);
        }

        private bool IsPathsIntersects(Vector2[] firstPath, Vector2[] secondPath)
        {
            for (int i = 0; i < firstPath.Length - 1; i++)
            {
                for (int j = 0; j < secondPath.Length - 1; j++)
                {
                    Bounds firstSegment = GetBoundsByTwoPoints(firstPath[i], firstPath[i + 1],
                        _lineRenderer.LineThickness);

                    Bounds secondSegment = GetBoundsByTwoPoints(secondPath[j], secondPath[j + 1],
                        _lineRenderer.LineThickness);

                    if (firstSegment.Intersects(secondSegment))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsPathIntersectElement(Vector2[] path, ConnectionElement connectionElement)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Bounds segment = GetBoundsByTwoPoints(path[i], path[i + 1],
                    _lineRenderer.LineThickness);

                if (segment.Intersects(connectionElement.Bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private Bounds GetBoundsByTwoPoints(Vector2 a, Vector2 b, float thickness)
        {
            Vector2 segment = b - a;
            int mainAxisIndex = Mathf.Abs(segment.y) < Epsilon ? 0 : 1;
            int subAxisIndex = Mathf.Abs(segment.y) < Epsilon ? 1 : 0;
            Vector2 size = default;
            size[mainAxisIndex] = Mathf.Abs(segment[mainAxisIndex]) - Epsilon;
            size[subAxisIndex] = thickness;
            Bounds bounds = new Bounds((a + b) / 2, size);
            return bounds;
        }

        private Vector2 GetClosestPointOnBorder(ConnectionElement connectionElement, Vector2 position)
        {
            Vector2 startPoint = default;
            Vector2 endPoint = default;

            startPoint[connectionElement.MainAxisIndex] = connectionElement.Bounds.center[connectionElement.MainAxisIndex] +
                connectionElement.Direction[connectionElement.MainAxisIndex] *
                connectionElement.Bounds.extents[connectionElement.MainAxisIndex];

            startPoint[connectionElement.SubAxisIndex] = connectionElement.Bounds.min[connectionElement.SubAxisIndex] + connectionElement.CornerOffset;
            endPoint[connectionElement.MainAxisIndex] = startPoint[connectionElement.MainAxisIndex];
            endPoint[connectionElement.SubAxisIndex] = connectionElement.Bounds.max[connectionElement.SubAxisIndex] - connectionElement.CornerOffset;

            Vector2 segment = endPoint - startPoint;
            Vector2 projection = Vector3.Project(position - startPoint, segment);

            if (Vector2.Dot(projection, segment) <= 0)
            {
                return startPoint;
            }

            Vector2 pointOnSegment = Vector2.Lerp(startPoint, endPoint, projection.magnitude / segment.magnitude);
            return pointOnSegment;
        }

        private Bounds GetDirectAccessArea(ConnectionElement connectionElement, Bounds commonBounds)
        {
            Vector2 startBoxPoint = (Vector2)connectionElement.Bounds.center + connectionElement.Direction *
                (connectionElement.Bounds.extents[connectionElement.MainAxisIndex] + _edgeDistanceThreshold);

            Vector2 endBoxPoint = startBoxPoint + connectionElement.Direction * commonBounds.size[connectionElement.MainAxisIndex];

            float mainAxisLength =
                Mathf.Abs(startBoxPoint[connectionElement.MainAxisIndex] - endBoxPoint[connectionElement.MainAxisIndex]);

            float subAxisLength = connectionElement.Bounds.size[connectionElement.SubAxisIndex] - 2 * connectionElement.CornerOffset;
            Vector2 size = default;
            size[connectionElement.MainAxisIndex] = mainAxisLength;
            size[connectionElement.SubAxisIndex] = subAxisLength;
            Bounds area = new Bounds((startBoxPoint + endBoxPoint) / 2, size);
            return area;
        }

        private Vector2[] AddRoundedAngles(Vector2[] points)
        {
            List<Vector2> additionalPoints = new() { points[0] };

            for (int i = 1; i < points.Length - 1; i++)
            {
                Vector2 firstSegment = points[i - 1] - points[i];
                Vector2 secondSegment = points[i + 1] - points[i];

                if (firstSegment.sqrMagnitude > Epsilon && secondSegment.sqrMagnitude > Epsilon &&
                    Mathf.Abs(Vector2.Dot(firstSegment, secondSegment)) < Epsilon)
                {
                    Vector2 bisector = firstSegment.normalized + secondSegment.normalized;

                    Vector2 center =
                        points[i] + bisector * _radius;

                    float d = Mathf.Min(firstSegment.magnitude / 2, secondSegment.magnitude / 2, _radius);

                    float angleOffset = Vector2.SignedAngle(Vector2.right, bisector) * Mathf.Deg2Rad - Mathf.PI / 4 +
                        Mathf.PI;

                    float alpha = Mathf.Asin((_radius - d) / _radius);
                    float x = (1 - Mathf.Cos(alpha)) * _radius;
                    float order = Vector2.SignedAngle(-firstSegment, secondSegment);
                    float startAngle;
                    float endAngle;
                    Vector2 shift;

                    if (firstSegment.sqrMagnitude < secondSegment.sqrMagnitude && order < 0 ||
                        secondSegment.sqrMagnitude < firstSegment.sqrMagnitude && order > 0)
                    {
                        startAngle = 0;
                        endAngle = Mathf.PI / 2 - alpha;

                        shift = order > 0
                            ? -firstSegment.normalized * x
                            : -secondSegment.normalized * x;
                    }
                    else
                    {
                        startAngle = alpha;
                        endAngle = Mathf.PI / 2;

                        shift = order < 0
                            ? -firstSegment.normalized * x
                            : -secondSegment.normalized * x;
                    }

                    if (order < 0)
                    {
                        (startAngle, endAngle) = (endAngle, startAngle);
                    }

                    Vector3 containerScale = _container.lossyScale;
                    int steps = Mathf.CeilToInt(_radius * Mathf.Max(containerScale.x, containerScale.y, containerScale.z) * _stepsPerUnit);

                    for (int j = 0; j <= steps; j++)
                    {
                        float progress = (float)j / steps;
                        float radian = Mathf.Lerp(startAngle, endAngle, progress) + angleOffset;

                        additionalPoints.Add(new Vector2(Mathf.Cos(radian) * _radius, Mathf.Sin(radian) * _radius) +
                            center + shift);
                    }
                }
                else
                {
                    additionalPoints.Add(points[i]);
                }
            }

            additionalPoints.Add(points[^1]);
            return additionalPoints.ToArray();
        }

        private int CalculateCornerCount(ConnectionElement source, ConnectionElement target)
        {
            Bounds commonBounds = source.Bounds;
            commonBounds.Encapsulate(target.Bounds);
            Bounds sourceDirectAccessArea = GetDirectAccessArea(source, commonBounds);
            Bounds targetDirectAccessArea = GetDirectAccessArea(target, commonBounds);
            float dotDirections = Vector2.Dot(source.Direction, target.Direction);

            // 1 случай: перпендикулярные выходы.
            if (Mathf.Abs(dotDirections) < Epsilon)
            {
                // Если есть пересечение зон прямой доступности у двух элементов, то их можно соединить с использованием
                // одного перегиба, иначе с использованием трех перегибов.
                if (sourceDirectAccessArea.Intersects(targetDirectAccessArea))
                {
                    return 1;
                }

                return 3;
            }

            // 2 случай: сонаправленные выходы
            if (Mathf.Abs(dotDirections - 1) < Epsilon)
            {
                // Если длина общего Bounding Box'а по дополнительной оси не меньше, чем максимальный размер Bound'ов
                // из двух элементов по дополнительной оси + corner offset, то их можно соединить с использованием двух
                // перегибов, иначе с использованием четырех перегибов.
                float sourceSubAxisLength = source.Bounds.size[source.SubAxisIndex];
                float targetSubAxisLength = target.Bounds.size[target.SubAxisIndex];
                float maxSubAxisLength = Mathf.Max(sourceSubAxisLength, targetSubAxisLength);
                float lengthThreshold = maxSubAxisLength + _cornerOffset;
                float commonBoundsLength = commonBounds.size[source.SubAxisIndex];

                if (commonBoundsLength >= lengthThreshold)
                {
                    return 2;
                }

                return 4;
            }

            // 3 случай: выходы противоположно направленные
            // Если есть пересечение зон прямой доступности у двух элементов, то их можно соединить без
            // использования перегибов.
            if (sourceDirectAccessArea.Intersects(targetDirectAccessArea))
            {
                return 0;
            }

            // Если длина общего Bounding Box'а по основой оси не меньше, чем сумма размеров Bound'ов
            // двух элементов по основной оси + 2 * EdgeThreshold, то их можно соединить с использованием двух
            // перегибов, иначе с использованием четырех перегибов.
            if (source.Direction[source.MainAxisIndex] > 0 &&
                target.Bounds.min[target.MainAxisIndex] - source.Bounds.max[source.MainAxisIndex] >
                2 * _edgeDistanceThreshold)
            {
                return 2;
            }

            if (source.Direction[source.MainAxisIndex] < 0 &&
                source.Bounds.min[source.MainAxisIndex] - target.Bounds.max[target.MainAxisIndex] >
                2 * _edgeDistanceThreshold)
            {
                return 2;
            }

            return 4;
        }

        private Vector2[] GetArrowPoints(Vector2[] secondPath)
        {
            Vector2 lastPoint = secondPath[^1];
            Vector2 lastSegment = secondPath[^2] - secondPath[^1];
            Vector2 arrowSegment = lastSegment.normalized * _arrowSize;

            Vector2[] arrowPoints = new Vector2[]
            {
                (Vector2)(Quaternion.Euler(0, 0, -_arrowAngle / 2) * arrowSegment) + lastPoint, lastPoint,
                (Vector2)(Quaternion.Euler(0, 0, _arrowAngle / 2) * arrowSegment) + lastPoint
            };

            return arrowPoints;
        }

        private void AddBoarders(Vector2[] firstPath, Vector2[] secondPath)
        {
            Vector2 firstSegment = (firstPath[^2] - firstPath[^1]).normalized;
            float boarderImageThickness = 1f;
            Vector2 offset = ((RectTransform)transform).pivot;

            Vector2[] firstBoarder =
            {
                new Vector2(-firstSegment.y, firstSegment.x) * _arrowSize / 2 + firstPath[^1] + offset - firstSegment * boarderImageThickness,
                new Vector2(firstSegment.y, -firstSegment.x) * _arrowSize / 2 + firstPath[^1] + offset - firstSegment * boarderImageThickness
            };

            Vector2 secondSegment = (secondPath[1] - secondPath[0]).normalized;

            Vector2[] secondBoarder =
            {
                new Vector2(-secondSegment.y, secondSegment.x) * _arrowSize / 2 + secondPath[0] + offset - secondSegment * boarderImageThickness,
                new Vector2(secondSegment.y, -secondSegment.x) * _arrowSize / 2 + secondPath[0] + offset - secondSegment * boarderImageThickness
            };

            _edgeLineBoarders.Segments = new List<Vector2[]>() { firstBoarder, secondBoarder };
        }

        private void OnDrawGizmosSelected()
        {
            if (_lineRenderer == null)
            {
                return;
            }

            Gizmos.color = Color.green;

            if (_lineRenderer.Segments == null || _lineRenderer.Segments.Count < 1)
            {
                return;
            }
            
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

            foreach (Vector2 point in _lineRenderer.Segments[0])
            {
                Gizmos.DrawSphere(_lineRenderer.transform.TransformPoint(point), 1f);
            }

            if (_lineRenderer.Segments.Count < 2)
            {
                return;
            }

            Gizmos.color = Color.red;

            foreach (Vector2 point in _lineRenderer.Segments[1])
            {
                Gizmos.DrawSphere(_lineRenderer.transform.TransformPoint(point), 1f);
            }
        }

        /// <summary>
        /// Вспомогательная структура для построения линии между элементами
        /// </summary>
        private struct ConnectionElement
        {
            /// <summary>
            /// Направление элемента, из которого выходит линия
            /// </summary>
            public readonly Vector2 Direction;
            /// <summary>
            /// Границы элемента
            /// </summary>
            public readonly Bounds Bounds;
            /// <summary>
            /// Смещение у углов элемента, на которых линия не может быть построена
            /// </summary>
            public readonly float CornerOffset;
            /// <summary>
            /// Основная ось направления линии
            /// </summary>
            public readonly int MainAxisIndex;
            /// <summary>
            /// Побочная ось направления линии
            /// </summary>
            public readonly int SubAxisIndex;

            /// <summary>
            /// Конструктор <see cref="ConnectionElement"/>
            /// </summary>
            /// <param name="bounds">Границы элемента</param>
            /// <param name="direction">Направление линии выходящей из элемента</param>
            /// <param name="cornerOffset">Смещение у углов элемента</param>
            public ConnectionElement(Bounds bounds, Vector2 direction, float cornerOffset)
            {
                Bounds = bounds;
                Direction = direction;
                MainAxisIndex = Direction == Vector2.up || Direction == Vector2.down ? 1 : 0;
                SubAxisIndex = MainAxisIndex == 1 ? 0 : 1;
                CornerOffset = bounds.size == Vector3.zero ? 0 : cornerOffset;
            }

            /// <summary>
            /// Конструктор <see cref="ConnectionElement"/>
            /// </summary>
            /// <param name="bounds">Границы элемента</param>
            /// <param name="direction">Направление линии выходящей из элемента</param>
            public ConnectionElement(Bounds bounds, Vector2 direction)
            {
                Bounds = bounds;
                Direction = direction;
                MainAxisIndex = Direction == Vector2.up || Direction == Vector2.down ? 1 : 0;
                SubAxisIndex = MainAxisIndex == 1 ? 0 : 1;
                CornerOffset = bounds.extents[SubAxisIndex];
            }
        }

        /// <summary>
        /// Группа направлений линии
        /// </summary>
        private struct DirectionsGroup
        {
            /// <summary>
            /// Направление линии из исходящего элемента
            /// </summary>
            public readonly Vector2 Start;
            /// <summary>
            /// Направление линии в промежуточный элемент
            /// </summary>
            public readonly Vector2 MidEnter;
            /// <summary>
            /// Направление линии из промежуточного элемента
            /// </summary>
            public readonly Vector2 MidExit;
            /// <summary>
            /// Направление линии в конечный элемент
            /// </summary>
            public readonly Vector2 End;

            /// <summary>
            /// Конструктор <see cref="DirectionsGroup"/>
            /// </summary>
            /// <param name="start">Направление линии из исходящего элемента</param>
            /// <param name="midEnter">Направление линии в промежуточный элемент</param>
            /// <param name="midExit">Направление линии из промежуточного элемента</param>
            /// <param name="end">Направление линии в конечный элемент</param>
            public DirectionsGroup(Vector2 start, Vector2 midEnter, Vector2 midExit, Vector2 end)
            {
                Start = start;
                MidEnter = midEnter;
                MidExit = midExit;
                End = end;
            }
        }
    }
}
