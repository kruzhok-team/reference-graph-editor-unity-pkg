/// Credit Ziboo, Andrew Quesenberry 
/// Sourced from - http://forum.unity3d.com/threads/free-reorderable-list.364600/
/// Last Child Fix - https://bitbucket.org/SimonDarksideJ/unity-ui-extensions/issues/70/all-re-orderable-lists-cause-a-transform

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
namespace Talent.GraphEditor.Unity.Runtime
{
    [RequireComponent(typeof(RectTransform), typeof(LayoutElement))]
    public class ReorderableListElementCustom : MonoBehaviour
    {
        [Tooltip("Can this element be dragged?")]
        [SerializeField]
        private InteractArea interactionAreaOverride;

        [SerializeField]
        private GameObject _fakeElementPrefab;

        [Tooltip("Can this element be dragged?")]
        [SerializeField]
        private bool isGrabbable = true;

        [Tooltip("Can this element be dropped in another container?")]
        [SerializeField]
        private bool isTransferable = true;

        [Tooltip("Can this element be dropped in space?")]
        [SerializeField]
        private bool isDroppableInSpace = false;

        [Space]

        [Tooltip("Can this element be dragged along vertical axis?")]
        [SerializeField]
        private bool allowVerticalDrag = true;

        [Tooltip("Can this element be dragged along horizontal axis?")]
        [SerializeField]
        private bool allowHorizontalDrag = true;

        public bool IsTransferable
        {
            get { return isTransferable; }
            set
            {
                _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
                _canvasGroup.blocksRaycasts = value;
                isTransferable = value;
            }
        }

        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private ReorderableListCustom _currentReorderableListRaycasted;

        private int _fromIndex;
        private RectTransform _draggingObject;
        private LayoutElement _draggingObjectLE;
        private Vector2 _draggingObjectOriginalSize;

        private RectTransform _fakeElement;
        private LayoutElement _fakeElementLE;

        private int _displacedFromIndex;
        private RectTransform _displacedObject;
        private LayoutElement _displacedObjectLE;
        private Vector2 _displacedObjectOriginalSize;
        private ReorderableListCustom _displacedObjectOriginList;

        private bool _isDragging;
        private RectTransform _rect;
        private ReorderableListCustom _reorderableList;
        private CanvasGroup _canvasGroup;
        private Vector3 _listStartPosition;
        internal bool isValid;

        private GameObject _initialObject;
        private int _initialIndex;

        private void Awake()
        {
            if (interactionAreaOverride != null)
            {
                interactionAreaOverride.BeginDrag += OnBeginDrag;
                interactionAreaOverride.Drag += OnDrag;
                interactionAreaOverride.EndDrag += OnEndDrag;
            }
        }

    #region IBeginDragHandler Members
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_canvasGroup) { _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>(); }
            _canvasGroup.blocksRaycasts = false;
            isValid = true;
            if (_reorderableList == null)
                return;

            //Can't drag, return...
            if (!_reorderableList.IsDraggable || !this.isGrabbable)
            {
                _draggingObject = null;
                return;
            }

            //If not CloneDraggedObject just set draggingObject to this gameobject
            if (_reorderableList.CloneDraggedObject == false)
            {
                _initialIndex = transform.GetSiblingIndex();

                _initialObject = (GameObject)Instantiate(gameObject);
                _initialObject.transform.SetParent(transform.parent, false);
                _initialObject.transform.SetSiblingIndex(transform.GetSiblingIndex());

                if (!_initialObject.TryGetComponent(out CanvasGroup canvasGroup))
                {
                    canvasGroup = _initialObject.AddComponent<CanvasGroup>();
                }

                canvasGroup.alpha = 0.5f;

                _draggingObject = _rect;
                _fromIndex = _rect.GetSiblingIndex();
                _displacedFromIndex = -1;
                //Send OnElementRemoved Event
                if (_reorderableList.OnElementRemoved != null)
                {
                    _reorderableList.OnElementRemoved.Invoke(new ReorderableListCustom.ReorderableListEventStruct
                    {
                        DroppedObject = _draggingObject.gameObject,
                        IsAClone = _reorderableList.CloneDraggedObject,
                        SourceObject = _reorderableList.CloneDraggedObject ? gameObject : _draggingObject.gameObject,
                        FromList = _reorderableList,
                        FromIndex = _fromIndex,
                    });
                }
                if (isValid == false)
                {
                    _draggingObject = null;
                    return;
                }
            }
            else
            {
                //Else Duplicate
                GameObject clone = (GameObject)Instantiate(gameObject);
                _draggingObject = clone.GetComponent<RectTransform>();
            }

            //Put _dragging object into the dragging area
            _draggingObjectOriginalSize = gameObject.GetComponent<RectTransform>().rect.size;
            _draggingObjectLE = _draggingObject.GetComponent<LayoutElement>();
            _draggingObject.SetParent(_reorderableList.DraggableArea, true);
            _draggingObject.SetAsLastSibling();
            _reorderableList.Refresh();

            //Create a fake element for previewing placement
            if (_fakeElementPrefab == null)
            {
                _fakeElement = new GameObject("Fake").AddComponent<RectTransform>();
                _fakeElementLE = _fakeElement.gameObject.AddComponent<LayoutElement>();
                _fakeElement.SetParent(_reorderableList.Content, false);
            }
            else
            {
                _fakeElement = Instantiate(_fakeElementPrefab, _reorderableList.Content).GetComponent<RectTransform>();
                _fakeElementLE = _fakeElement.GetComponent<LayoutElement>();

                _fakeElement.transform.SetSiblingIndex(_initialObject.transform.GetSiblingIndex());

                _fakeElement.gameObject.SetActive(false);
            }

            _canvasGroup.alpha = 0.5f;

            RefreshSizes();

            //Send OnElementGrabbed Event
            if (_reorderableList.OnElementGrabbed != null)
            {
                _reorderableList.OnElementGrabbed.Invoke(new ReorderableListCustom.ReorderableListEventStruct
                {
                    DroppedObject = _draggingObject.gameObject,
                    IsAClone = _reorderableList.CloneDraggedObject,
                    SourceObject = _reorderableList.CloneDraggedObject ? gameObject : _draggingObject.gameObject,
                    FromList = _reorderableList,
                    FromIndex = _fromIndex,
                });

                if (!isValid)
                {
                    CancelDrag();
                    return;
                }
            }

            _listStartPosition = _reorderableList.Content.position;

            _isDragging = true;
        }
    #endregion

    #region IDragHandler Members
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            if (!isValid)
            {
                CancelDrag();
                return;
            }
            //Set dragging object on cursor
            var canvas = _draggingObject.GetComponentInParent<Canvas>();
            Vector3 worldPoint;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.GetComponent<RectTransform>(), eventData.position,
                canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out worldPoint);

            _draggingObject.position = new Vector3(allowHorizontalDrag ? worldPoint.x : _draggingObject.position.x, allowVerticalDrag ? worldPoint.y : _draggingObject.position.y, _draggingObject.position.z);

            _draggingObject.position = new Vector3(
                _draggingObject.position.x,
                Mathf.Clamp(_draggingObject.position.y, _listStartPosition.y - _reorderableList.Content.rect.height + _draggingObject.rect.size.y / 2, _listStartPosition.y - _draggingObject.rect.size.y / 2),
                _draggingObject.position.z
            );

            ReorderableListCustom _oldReorderableListRaycasted = _currentReorderableListRaycasted;

            //Check everything under the cursor to find a ReorderableListCustom
            EventSystem.current.RaycastAll(eventData, _raycastResults);
            for (int i = 0; i < _raycastResults.Count; i++)
            {
                _currentReorderableListRaycasted = _raycastResults[i].gameObject.GetComponent<ReorderableListCustom>();
                if (_currentReorderableListRaycasted != null)
                {
                    break;
                }
            }

            //If nothing found or the list is not dropable, put the fake element outside
            if (_currentReorderableListRaycasted == null || _currentReorderableListRaycasted.IsDropable == false
                || ((_fakeElement.parent == _currentReorderableListRaycasted.Content
                    ? _currentReorderableListRaycasted.Content.childCount - 1
                    : _currentReorderableListRaycasted.Content.childCount) >= _currentReorderableListRaycasted.maxItems && !_currentReorderableListRaycasted.IsDisplacable)
                || _currentReorderableListRaycasted.maxItems <= 0)
            {
                RefreshSizes();
                //_fakeElement.transform.SetParent(_reorderableList.DraggableArea, false);
                // revert the displaced element when not hovering over its list
                if (_displacedObject != null)
                {
                    revertDisplacedElement();
                }
            }
            //Else find the best position on the list and put fake element on the right index 
            else if (_currentReorderableListRaycasted == _reorderableList || IsTransferable)
            {
                if (_currentReorderableListRaycasted.Content.childCount < _currentReorderableListRaycasted.maxItems && _fakeElement.parent != _currentReorderableListRaycasted.Content)
                {
                    _fakeElement.SetParent(_currentReorderableListRaycasted.Content, false);
                }

                _fakeElement.SetParent(_currentReorderableListRaycasted.transform);

                float minDistance = float.PositiveInfinity;
                int targetIndex = 0;
                RectTransform targetElement = null;
                float dist = 0;

                for (int j = 0; j < _currentReorderableListRaycasted.Content.childCount; j++)
                {
                    var c = _currentReorderableListRaycasted.Content.GetChild(j).GetComponent<RectTransform>();
                
                    if (_currentReorderableListRaycasted.ContentLayout is VerticalLayoutGroup)
                        dist = Mathf.Abs(c.position.y - worldPoint.y);
                    else if (_currentReorderableListRaycasted.ContentLayout is HorizontalLayoutGroup)
                        dist = Mathf.Abs(c.position.x - worldPoint.x);
                    else if (_currentReorderableListRaycasted.ContentLayout is GridLayoutGroup)
                        dist = (Mathf.Abs(c.position.x - worldPoint.x) + Mathf.Abs(c.position.y - worldPoint.y));

                    if (dist < minDistance)
                    {
                        minDistance = dist;

                        if (worldPoint.y > c.position.y)
                        {
                            targetIndex = j;
                        }
                        else
                        {
                            targetIndex = j + 1;
                        }

                        targetIndex = Mathf.Clamp(targetIndex, 0, _currentReorderableListRaycasted.Content.childCount);

                        targetElement = c;
                    }
                }
                if ((_currentReorderableListRaycasted != _oldReorderableListRaycasted || targetIndex != _displacedFromIndex)
                    && _currentReorderableListRaycasted.Content.childCount == _currentReorderableListRaycasted.maxItems)
                {
                    Transform toDisplace = _currentReorderableListRaycasted.Content.GetChild(targetIndex);
                    if (_displacedObject != null)
                    {
                        revertDisplacedElement();
                        if (_currentReorderableListRaycasted.Content.childCount > _currentReorderableListRaycasted.maxItems)
                        {
                            displaceElement(targetIndex, toDisplace);
                        }
                    }
                    else if (_fakeElement.parent != _currentReorderableListRaycasted.Content)
                    {
                        _fakeElement.SetParent(_currentReorderableListRaycasted.Content, false);
                        displaceElement(targetIndex, toDisplace);
                    }
                }
                RefreshSizes();

                _fakeElement.SetParent(_currentReorderableListRaycasted.Content, false);

                bool isInitialPosition = false;

                if (targetIndex == _initialIndex || targetIndex == _initialIndex + 1)
                {
                    isInitialPosition = true;
                }

                _fakeElement.gameObject.SetActive(!isInitialPosition);


                _fakeElement.SetSiblingIndex(targetIndex);
            }
        }
    #endregion

    #region Displacement
        private void displaceElement(int targetIndex, Transform displaced)
        {
            _displacedFromIndex = targetIndex;
            _displacedObjectOriginList = _currentReorderableListRaycasted;
            _displacedObject = displaced.GetComponent<RectTransform>();
            _displacedObjectLE = _displacedObject.GetComponent<LayoutElement>();
            _displacedObjectOriginalSize = _displacedObject.rect.size;

            var args = new ReorderableListCustom.ReorderableListEventStruct
            {
                DroppedObject = _displacedObject.gameObject,
                FromList = _currentReorderableListRaycasted,
                FromIndex = targetIndex,
            };


            int c = _fakeElement.parent == _reorderableList.Content
                ? _reorderableList.Content.childCount - 1
                : _reorderableList.Content.childCount;

            if (_reorderableList.IsDropable && c < _reorderableList.maxItems && _displacedObject.GetComponent<ReorderableListElementCustom>().IsTransferable)
            {
                _displacedObjectLE.preferredWidth = _draggingObjectOriginalSize.x;
                _displacedObjectLE.preferredHeight = _draggingObjectOriginalSize.y;
                _displacedObject.SetParent(_reorderableList.Content, false);
                _displacedObject.rotation = _reorderableList.transform.rotation;
                _displacedObject.SetSiblingIndex(_fromIndex);
                // Force refreshing both lists because otherwise we get inappropriate FromList in ReorderableListEventStruct 
                _reorderableList.Refresh();
                _currentReorderableListRaycasted.Refresh();

                args.ToList = _reorderableList;
                args.ToIndex = _fromIndex;
                _reorderableList.OnElementDisplacedTo.Invoke(args);
                _reorderableList.OnElementAdded.Invoke(args);
            }
            else if (_displacedObject.GetComponent<ReorderableListElementCustom>().isDroppableInSpace)
            {
                _displacedObject.SetParent(_currentReorderableListRaycasted.DraggableArea, true);
                _currentReorderableListRaycasted.Refresh();
                _displacedObject.position += new Vector3(_draggingObjectOriginalSize.x / 2, _draggingObjectOriginalSize.y / 2, 0);
            }
            else
            {
                _displacedObject.SetParent(null, true);
                _displacedObjectOriginList.Refresh();
                _displacedObject.gameObject.SetActive(false);
            }
            _displacedObjectOriginList.OnElementDisplacedFrom.Invoke(args);
            _reorderableList.OnElementRemoved.Invoke(args);
        }

        private void revertDisplacedElement()
        {
            var args = new ReorderableListCustom.ReorderableListEventStruct
            {
                DroppedObject = _displacedObject.gameObject,
                FromList = _displacedObjectOriginList,
                FromIndex = _displacedFromIndex,
            };
            if (_displacedObject.parent != null)
            {
                args.ToList = _reorderableList;
                args.ToIndex = _fromIndex;
            }

            _displacedObjectLE.preferredWidth = _displacedObjectOriginalSize.x;
            _displacedObjectLE.preferredHeight = _displacedObjectOriginalSize.y;
            _displacedObject.SetParent(_displacedObjectOriginList.Content, false);
            _displacedObject.rotation = _displacedObjectOriginList.transform.rotation;
            _displacedObject.SetSiblingIndex(_displacedFromIndex);
            _displacedObject.gameObject.SetActive(true);

            // Force refreshing both lists because otherwise we get inappropriate FromList in ReorderableListEventStruct 
            _reorderableList.Refresh();
            _displacedObjectOriginList.Refresh();

            if (args.ToList != null)
            {
                _reorderableList.OnElementDisplacedToReturned.Invoke(args);
                _reorderableList.OnElementRemoved.Invoke(args);
            }
            _displacedObjectOriginList.OnElementDisplacedFromReturned.Invoke(args);
            _displacedObjectOriginList.OnElementAdded.Invoke(args);

            _displacedFromIndex = -1;
            _displacedObjectOriginList = null;
            _displacedObject = null;
            _displacedObjectLE = null;

        }

        public void finishDisplacingElement()
        {
            if (_displacedObject.parent == null)
            {
                Destroy(_displacedObject.gameObject);
            }
            _displacedFromIndex = -1;
            _displacedObjectOriginList = null;
            _displacedObject = null;
            _displacedObjectLE = null;
        }
    #endregion

    #region IEndDragHandler Members
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;

            if (_draggingObject != null)
            {
                var args = new ReorderableListCustom.ReorderableListEventStruct
                {
                    DroppedObject = _draggingObject.gameObject,
                    IsAClone = _reorderableList.CloneDraggedObject,
                    SourceObject = _reorderableList.CloneDraggedObject ? gameObject : _draggingObject.gameObject,
                    FromList = _reorderableList,
                    FromIndex = _fromIndex,
                    ToList = _reorderableList,
                    ToIndex = _fakeElement.GetSiblingIndex()
                };

                RefreshSizes();
                _draggingObject.SetParent(_reorderableList.Content, false);
                _draggingObject.rotation = _reorderableList.transform.rotation;
                _draggingObject.SetSiblingIndex(_fakeElement.GetSiblingIndex());

                //If the item is transferable, it can be dragged out again
                if (IsTransferable)
                {
                    var cg = _draggingObject.GetComponent<CanvasGroup>();
                    cg.blocksRaycasts = true;
                }
                // Force refreshing both lists because otherwise we get inappropriate FromList in ReorderableListEventStruct 
                _reorderableList.Refresh();
                _reorderableList.Refresh();

                _reorderableList.OnElementAdded.Invoke(args);

                if (_displacedObject != null)
                {
                    finishDisplacingElement();
                }
            }

            //Delete fake element
            if (_fakeElement != null)
            {
                Destroy(_fakeElement.gameObject);
                _fakeElement = null;
            }

            if (_initialObject != null)
            {
                Destroy(_initialObject);
                _initialObject = null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    #endregion

        private void CancelDrag()
        {
            _isDragging = false;
            //If it's a clone, delete it
            if (_reorderableList.CloneDraggedObject)
            {
                Destroy(_draggingObject.gameObject);
            }
            //Else replace the draggedObject to his first place
            else
            {
                RefreshSizes();
                _draggingObject.SetParent(_reorderableList.Content, false);
                _draggingObject.rotation = _reorderableList.Content.transform.rotation;
                _draggingObject.SetSiblingIndex(_fromIndex);


                var args = new ReorderableListCustom.ReorderableListEventStruct
                {
                    DroppedObject = _draggingObject.gameObject,
                    IsAClone = _reorderableList.CloneDraggedObject,
                    SourceObject = _reorderableList.CloneDraggedObject ? gameObject : _draggingObject.gameObject,
                    FromList = _reorderableList,
                    FromIndex = _fromIndex,
                    ToList = _reorderableList,
                    ToIndex = _fromIndex
                };

                _reorderableList.Refresh();

                _reorderableList.OnElementAdded.Invoke(args);

                if (!isValid)
                    throw new Exception("Transfer is already Canceled.");

            }

            //Delete fake element
            if (_fakeElement != null)
            {
                Destroy(_fakeElement.gameObject);
                _fakeElement = null;
            }
            if (_displacedObject != null)
            {
                revertDisplacedElement();
            }

            if (_initialObject != null)
            {
                Destroy(_initialObject);
                _initialObject = null;
            }

            _canvasGroup.blocksRaycasts = true;
        }

        private void RefreshSizes()
        {
            Vector2 size = _draggingObjectOriginalSize;

            if (_currentReorderableListRaycasted != null
                && _currentReorderableListRaycasted.IsDropable
                && _currentReorderableListRaycasted.Content.childCount > 0
                && _currentReorderableListRaycasted.EqualizeSizesOnDrag)
            {
                var firstChild = _currentReorderableListRaycasted.Content.GetChild(0);
                if (firstChild != null)
                {
                    size = firstChild.GetComponent<RectTransform>().rect.size;
                }
            }

            _draggingObject.sizeDelta = size;

            if (_fakeElementPrefab == null)
            {
                _fakeElementLE.preferredHeight = _draggingObjectLE.preferredHeight = size.y;
                _fakeElementLE.preferredWidth = _draggingObjectLE.preferredWidth = size.x;
                _fakeElement.GetComponent<RectTransform>().sizeDelta = size;
            }
        }

        public void Init(ReorderableListCustom reorderableList)
        {
            _reorderableList = reorderableList;
            _rect = GetComponent<RectTransform>();
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        }
    }
}
