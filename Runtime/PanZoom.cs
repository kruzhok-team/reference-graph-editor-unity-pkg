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
        [SerializeField] private RenderController _renderController;
        [SerializeField] private TextMeshProUGUI _zoomValueText;
        [SerializeField] private RectTransform _targetRectTransform;
        [SerializeField] private GraphLayoutGroup _graphLayoutGroup;
        [SerializeField] private RawImage _background;
        [SerializeField] private Vector2 _minMaxZoom = new Vector2(0.1f, 2f);
        [SerializeField] private float _speed = 0.1f;
        [SerializeField] private float _focusAnimatingSpeed;

        [SerializeField] private Vector2 _leftBottomSizeBorders = new Vector2(1500, 1000);
        [SerializeField] private Vector2 _leftBottomSizeOffsets = new Vector2(600, 400);

        private RectTransform _backgroundRectTransform;
        private Vector2 _defaultScale;
        private float _defaultWidth;
        private float _defaultHeight;
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
            _defaultWidth = _targetRectTransform.rect.width;
            _defaultHeight = _targetRectTransform.rect.height;
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
            HandleZoom();

            UpdateBackground();

            _zoomValueText.text = $"{(int)(_targetRectTransform.localScale.x * 100)}%";
        }

        private void HandlePan()
        {
            if (Input.GetMouseButtonUp(0) || _runtimeGraphEditor.EditingEdge != null && _runtimeGraphEditor.EditingEdge.IsDraggableMode)
            {
                _isPanning = false;
                return;
            }
            
            if (Input.GetMouseButtonDown(0) && !UIFocusingSystem.Instance.Contexts.First().BlockOtherHotkeys && (_selectedElement == null || _selectedElement?.Object == _background.gameObject ||
                    _selectedElement?.Object != null && !IsCursorOverElement(_selectedElement.Object)))
            {
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

        private void HandleZoom()
        {
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

        public void AdjustView(bool overrideFocusOnGraph = false)
        {
            if (_canvas == null)
            {
                Init();
            }

            if (_selectedElement != null && _selectedElement.Object != _background.gameObject && !overrideFocusOnGraph && _selectedElement.Object.TryGetComponent(out RectTransform rectTransform))
            {
                FocusOnRectTransform(rectTransform);

                return;
            }

            _isAnimating = false;
            StopAllCoroutines();

            if (_graphLayoutGroup.GetRectChildrenCount() >= 1)
            {
                Vector3 currentScale = _targetRectTransform.localScale;
                Vector3 currentPos = _targetRectTransform.localPosition;

                _targetRectTransform.pivot = Vector2.one * 0.5f;

                _targetRectTransform.localScale = _defaultScale;
                _targetRectTransform.localPosition = _defaultPosition;

                _graphLayoutGroup.GetGraphCorners(out float left, out float top, out float right, out float bottom);

                CalculateFocusWithResolutionFactor(ref left, ref bottom, ref right, ref top, out float resolutionFactor);

                Vector2 position = new Vector2((right + left) / 2, (top + bottom) / 2);
                Vector2 size = new Vector2(Mathf.Abs(right - left), Mathf.Abs(bottom - top));

                float newScale = 1 / Mathf.Max(size.x / (_defaultWidth * resolutionFactor) / _canvas.transform.localScale.x,
                    size.y / (_defaultHeight * resolutionFactor) / _canvas.transform.localScale.x);
                newScale = Mathf.Clamp01(newScale);

                _targetRectTransform.localScale = currentScale;
                _targetRectTransform.localPosition = currentPos;

                StartCoroutine(Focus(newScale, -position * newScale));

                _minMaxZoom.x = Mathf.Min(_defaultMinMaxZoom.x, newScale);
                _minMaxZoom.y = Mathf.Max(_defaultMinMaxZoom.y, newScale);
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

            Vector3 minCorner = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxCorner = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var element in uiElements)
            {
                if (element == null)
                    continue;

                Vector3[] corners = new Vector3[4];
                element.GetWorldCorners(corners);

                foreach (var corner in corners)
                {
                    minCorner = Vector3.Min(minCorner, corner);
                    maxCorner = Vector3.Max(maxCorner, corner);
                }
            }

            Vector3[] worldCorners = new Vector3[4];
            worldCorners[0] = new Vector3(minCorner.x, minCorner.y, minCorner.z);
            worldCorners[1] = new Vector3(minCorner.x, maxCorner.y, minCorner.z);
            worldCorners[2] = new Vector3(maxCorner.x, maxCorner.y, maxCorner.z);
            worldCorners[3] = new Vector3(maxCorner.x, minCorner.y, maxCorner.z);

            for (int i = 0; i < 4; i++)
            {
                worldCorners[i] = _targetRectTransform.InverseTransformPoint(worldCorners[i]);
            }

            float left = worldCorners[0].x;
            float bottom = worldCorners[0].y;
            float right = worldCorners[2].x;
            float top = worldCorners[2].y;

            CalculateFocusWithResolutionFactor(ref left, ref bottom, ref right, ref top, out float resolutionFactor);

            Vector2 position = new Vector2((left + right) / 2, (top + bottom) / 2);
            Vector2 size = new Vector2(Mathf.Abs(right - left), Mathf.Abs(top - bottom));

            float newScale = 1 / Mathf.Max(size.x / (_defaultWidth * resolutionFactor) / _canvas.transform.localScale.x,
                size.y / (_defaultHeight * resolutionFactor) / _canvas.transform.localScale.x);
            newScale = Mathf.Clamp(newScale, _minMaxZoom.x, 1);

            StartCoroutine(Focus(newScale, -position * newScale));

            UpdateBackground();
        }

        private void CalculateFocusWithResolutionFactor(ref float left, ref float bottom, ref float right, ref float top, out float resolutionFactor)
        {
            resolutionFactor = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);

            if (Mathf.Abs(right - left) >= _leftBottomSizeBorders.x * resolutionFactor)
            {
                left -= _leftBottomSizeOffsets.x * resolutionFactor;
            }

            if (Mathf.Abs(bottom - top) >= _leftBottomSizeBorders.y * resolutionFactor)
            {
                top += _leftBottomSizeOffsets.y * resolutionFactor;
                bottom -= _leftBottomSizeOffsets.y * resolutionFactor;
            }
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
