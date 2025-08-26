using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Focusing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент для приближения и удаления представления графа, а также для перемещения по нему
    /// </summary>
    public class PanZoom : MonoBehaviour
    {
        [SerializeField] private SimpleContextLayer _context;

        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;
        [SerializeField] private RectTransform _viewportRectTransform;
        [SerializeField] private RenderController _renderController;
        [SerializeField] private TextMeshProUGUI _zoomValueText;
        [SerializeField] private RectTransform _targetRectTransform;
        [SerializeField] private GraphLayoutGroup _graphLayoutGroup;
        [SerializeField] private RawImage _background;
        [SerializeField] private GameObject[] _panBlockingElements = new GameObject[0]; 
        [SerializeField] private Vector2 _minMaxZoom = new Vector2(0.1f, 2f);
        [SerializeField] private float _speed = 0.1f;
        [SerializeField] private float _focusAnimatingSpeed;

        [SerializeField] private Vector2 _leftBottomSizeBorders = new Vector2(1500, 1000);
        [SerializeField] private Vector2 _leftBottomSizeOffsets = new Vector2(600, 400);

        private RectTransform _backgroundRectTransform;
        private Vector2 _defaultScale;
        private float _defaultBackgroundWidth;
        private float _defaultBackgroundHeight;

        private Vector3 _lastMousePos;
        private Vector2 _defaultPosition;
        private Vector2 _defaultMinMaxZoom;
        private Canvas _canvas;

        private bool _isPanning;
        private bool _isAnimating;

        private ISelectable _selectedElement;

        private void Start()
        {
            if (_canvas == null)
            {
                Init();
            }
        }

        private void OnEnable()
        {
            UIFocusingSystem.Instance.SelectionHandler.Selected += ElementSelected;
            UIFocusingSystem.Instance.SelectionHandler.Deselected += ElementDeselected;

            _context.PushLayer();
        }

        private void OnDisable()
        {
            UIFocusingSystem.Instance.SelectionHandler.Selected -= ElementSelected;
            UIFocusingSystem.Instance.SelectionHandler.Deselected -= ElementDeselected;

            _context.RemoveLayer();

            _isAnimating = false;
        }
        private void Init()
        {
            _canvas = GetComponentInParent<Canvas>();
            _backgroundRectTransform = _background.GetComponent<RectTransform>();
            _defaultBackgroundWidth = _background.uvRect.width;
            _defaultBackgroundHeight = _background.uvRect.height;
            _defaultScale = _targetRectTransform.localScale;

            _defaultPosition = _targetRectTransform.localPosition;

            _defaultMinMaxZoom = _minMaxZoom;
        }

        private void ElementSelected(ISelectable element)
        {
            _selectedElement = element;
        }

        private void ElementDeselected(ISelectable element)
        {
            if (_selectedElement == element)
            {
                _selectedElement = null;
            }
        }

        private void Update()
        {
            if (!Application.isFocused || _isAnimating)
            {
                return;
            }

            if (!IsCursorWithinScreen(Input.mousePosition))
            {
                return;
            }

            HandlePan();

            UpdateBackground();

            _zoomValueText.text = $"{(int)(_targetRectTransform.localScale.x * 100)}%";
        }

        private void HandlePan()
        {
            if (Input.GetMouseButtonUp(0) || _runtimeGraphEditor.EditingEdge != null && _runtimeGraphEditor.EditingEdge.IsDraggableMode || UIFocusingSystem.Instance.Contexts.First().BlockOtherHotkeys)
            {
                _isPanning = false;
                return;
            }
            
            if (Input.GetMouseButtonDown(0) && !UIFocusingSystem.Instance.Contexts.First().BlockOtherHotkeys && (_selectedElement == null || _selectedElement?.Object == _background.gameObject ||
                    _selectedElement?.Object != null && !IsCursorOverElement(_selectedElement.Object)))
            {
                foreach (GameObject element in _panBlockingElements)
                {
                    if (IsCursorOverElement(element))
                    {
                        _isPanning = false;
                        return;
                    }
                }

                _lastMousePos = Input.mousePosition;

                _isPanning = true;
            }

            if (_isPanning && Input.GetMouseButton(0))
            {
                Vector3 deltaPos = _lastMousePos - Input.mousePosition;
                _targetRectTransform.localPosition += new Vector3(-deltaPos.x, -deltaPos.y) / _canvas.transform.localScale.x;

                _lastMousePos = Input.mousePosition;
            }
        }

        private bool IsCursorOverElement(GameObject element)
        {
            PointerEventData pointer = new(EventSystem.current);
            pointer.position = Input.mousePosition;

            List<RaycastResult> raycastResults = new();
            EventSystem.current.RaycastAll(pointer, raycastResults);

            if (raycastResults.Count > 0)
            {
                foreach (RaycastResult raycastResult in raycastResults)
                {
                    if (raycastResult.gameObject == element)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void UpdateBackground()
        {
            float currentScale = _targetRectTransform.localScale.x;
            Vector2 currentPosition = _targetRectTransform.localPosition;
            float width = _backgroundRectTransform.rect.width;
            float height = _backgroundRectTransform.rect.height;

            var backgroundUVRect = _background.uvRect;

            backgroundUVRect.width = _defaultBackgroundWidth / currentScale;
            backgroundUVRect.height = _defaultBackgroundHeight / currentScale;

            backgroundUVRect.x = _defaultBackgroundWidth / 2 - (currentPosition.x + width / 2) * backgroundUVRect.width / width;
            backgroundUVRect.y = _defaultBackgroundHeight / 2 - (currentPosition.y + height / 2) * backgroundUVRect.height / height;

            _background.uvRect = backgroundUVRect;
        }

        public void HandleZoom()
        {
            if (!Application.isFocused || _isAnimating)
            {
                return;
            }

            if (!IsCursorWithinScreen(Input.mousePosition))
            {
                return;
            }

            float scrollValue = Input.mouseScrollDelta.y * _speed;

            if (scrollValue == 0)
            {
                return;
            }

            float currentScale = _targetRectTransform.localScale.x;
            float newScale = Mathf.Clamp(currentScale + scrollValue, _minMaxZoom.x, _minMaxZoom.y);

            Vector2 mousePosition = Input.mousePosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_targetRectTransform, mousePosition, null, out Vector2 localMousePositionBefore);

            _targetRectTransform.localScale = Vector3.one * newScale;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_targetRectTransform, mousePosition, null, out Vector2 localMousePositionAfter);

            Vector2 offset = localMousePositionAfter - localMousePositionBefore;
            _targetRectTransform.anchoredPosition += offset * newScale;
        }
        

        public void AdjustView(bool overrideFocusOnGraph = false)
        {
            if (_canvas == null)
            {
                Init();
            }

            if (_selectedElement != null && _selectedElement.Object != _background.gameObject && !overrideFocusOnGraph && _selectedElement.Object.TryGetComponent(out RectTransform rectTransform))
            {
                if (_selectedElement.Object.TryGetComponent(out EdgeView edge))
                {
                    FocusOnRectTransform(rectTransform, edge.TargetView.transform as RectTransform, edge.SourceView.transform as RectTransform);

                    return;
                }

                if (_selectedElement.Object.TryGetComponent(out NodeEventView nodeEventView))
                {
                    FocusOnRectTransform(nodeEventView.NodeView.transform as RectTransform);
                    return;
                }

                if (_selectedElement.Object.TryGetComponent(out NodeView nodeView))
                {
                    List<RectTransform> rectTransforms = new() { rectTransform };

                    foreach (EdgeView edgeView in nodeView.EdgeViews)
                    {
                        rectTransforms.Add(edgeView.transform as RectTransform);
                    }

                    if (nodeView.ChildsContainer != null)
                    {
                        foreach (NodeView childNode in nodeView.ChildsContainer.GetComponentsInChildren<NodeView>())
                        {
                            foreach (EdgeView childEdge in childNode.EdgeViews)
                            {
                                rectTransforms.Add(childEdge.SourceView.transform as RectTransform);
                                rectTransforms.Add(childEdge.TargetView.transform as RectTransform);
                            }
                        }
                    }

                    FocusOnRectTransform(rectTransforms.ToArray());

                    return;
                }

                FocusOnRectTransform(rectTransform);

                return;
            }

            _isAnimating = false;
            StopAllCoroutines();

            if (_graphLayoutGroup.GetRectChildrenCount() >= 1)
            {
                Bounds totalBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_targetRectTransform, _targetRectTransform);

                float bbWidth = totalBounds.size.x;
                float bbHeight = totalBounds.size.y;

                if (bbWidth < _leftBottomSizeBorders.x)
                {
                    bbWidth = _leftBottomSizeBorders.x;
                }

                if (bbHeight < _leftBottomSizeBorders.y)
                {
                    bbHeight = _leftBottomSizeBorders.y;
                }

                float contentWidth = bbWidth + 2f * _leftBottomSizeOffsets.x;
                float contentHeight = bbHeight + 2f * _leftBottomSizeOffsets.y;

                Vector2 viewportSize = _viewportRectTransform.rect.size;

                float scaleX = viewportSize.x / contentWidth;
                float scaleY = viewportSize.y / contentHeight;
                float newScale = Mathf.Min(scaleX, scaleY);

                newScale = Mathf.Clamp(newScale, _minMaxZoom.x, _minMaxZoom.y);

                Vector3 localCenter = totalBounds.center;
                Vector3 newPos = -localCenter * newScale;

                StartCoroutine(Focus(newScale, newPos));
            }

            UpdateBackground();
        }

        /// <summary>
        /// Фокусируется на определенных элементах
        /// </summary>
        /// <param name="uiElements">Элементы</param>
        public void FocusOnRectTransform(params RectTransform[] uiElements)
        {
            if (uiElements == null || uiElements.Length == 0)
            {
                return;
            }

            _isAnimating = false;
            StopAllCoroutines();

            Bounds totalBounds = new();
            bool firstBounds = true;

            foreach (var elem in uiElements)
            {
                if (elem == null) continue;

                Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    _targetRectTransform, elem);

                if (firstBounds)
                {
                    totalBounds = b;
                    firstBounds = false;
                }
                else
                {
                    totalBounds.Encapsulate(b.min);
                    totalBounds.Encapsulate(b.max);
                }
            }

            if (firstBounds)
            {
                return;
            }

            float bbWidth = totalBounds.size.x;
            float bbHeight = totalBounds.size.y;

            if (uiElements.Length > 1)
            {
                if (bbWidth < _leftBottomSizeBorders.x) bbWidth = _leftBottomSizeBorders.x;
                if (bbHeight < _leftBottomSizeBorders.y) bbHeight = _leftBottomSizeBorders.y;

                bbWidth += 2f * _leftBottomSizeOffsets.x;
                bbHeight += 2f * _leftBottomSizeOffsets.y;
            }
            else
            {
                bbWidth += 2f * _leftBottomSizeOffsets.x;
                bbHeight += 2f * _leftBottomSizeOffsets.y;
            }

            Vector2 viewportSize = _viewportRectTransform.rect.size;

            float scaleX = viewportSize.x / bbWidth;
            float scaleY = viewportSize.y / bbHeight;
            float newScale = Mathf.Min(scaleX, scaleY);

            newScale = Mathf.Clamp(newScale, _minMaxZoom.x, _minMaxZoom.y);

            Vector3 localCenter = totalBounds.center;
            Vector3 newPos = -localCenter * newScale;

            StartCoroutine(Focus(newScale, newPos));
            UpdateBackground();
        }

        private IEnumerator Focus(float scale, Vector3 pos)
        {
            _isAnimating = true;
            _renderController.EnableFullRendering(_focusAnimatingSpeed);

            Vector3 startScale = _targetRectTransform.localScale;
            Vector3 startPos = _targetRectTransform.localPosition;

            float timer = 0;

            while (timer <= _focusAnimatingSpeed)
            {
                _targetRectTransform.localScale =
                    Vector3.Lerp(startScale, Vector2.one * scale, timer / _focusAnimatingSpeed);

                _targetRectTransform.localPosition = Vector3.Lerp(startPos, pos, timer / _focusAnimatingSpeed);

                timer += Time.deltaTime;

                UpdateBackground();

                yield return null;
            }

            _targetRectTransform.localScale = Vector2.one * scale;
            _targetRectTransform.localPosition = pos;

            _isAnimating = false;
        }

        private bool IsCursorWithinScreen(Vector3 mousePosition)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            return mousePosition.x >= 0 && mousePosition.x <= screenWidth &&
                mousePosition.y >= 0 && mousePosition.y <= screenHeight;
        }
    }
}
