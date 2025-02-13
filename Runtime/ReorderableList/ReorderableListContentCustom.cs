/// Credit Ziboo
/// Sourced from - http://forum.unity3d.com/threads/free-reorderable-list.364600/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    [DisallowMultipleComponent]
    public class ReorderableListContentCustom : MonoBehaviour
    {
        private List<Transform> _cachedChildren;
        private List<ReorderableListElementCustom> _cachedListElement;
        private ReorderableListElementCustom _ele;
        private ReorderableListCustom _extList;
        private RectTransform _rect;
        private bool _started = false;

        private void OnEnable()
        {
            if (_rect) StartCoroutine(RefreshChildren());
        }

        public void OnTransformChildrenChanged()
        {
            if (this.isActiveAndEnabled) StartCoroutine(RefreshChildren());
        }

        public void Init(ReorderableListCustom extList)
        {
            if (_started) { StopCoroutine(RefreshChildren()); }

            _extList = extList;
            _rect = GetComponent<RectTransform>();
            _cachedChildren = new List<Transform>();
            _cachedListElement = new List<ReorderableListElementCustom>();

            StartCoroutine(RefreshChildren());
            _started = true;
        }

        private IEnumerator RefreshChildren()
        {
            //Handle new children
            for (int i = 0; i < _rect.childCount; i++)
            {
                if (_cachedChildren.Contains(_rect.GetChild(i)))
                    continue;

                //Get or Create ReorderableListElementCustom
                _ele = _rect.GetChild(i).gameObject.GetComponent<ReorderableListElementCustom>() ??
                    _rect.GetChild(i).gameObject.AddComponent<ReorderableListElementCustom>();
                _ele.Init(_extList);

                _cachedChildren.Add(_rect.GetChild(i));
                _cachedListElement.Add(_ele);
            }

            //HACK a little hack, if I don't wait one frame I don't have the right deleted children
            yield return 0;

            //Remove deleted child
            for (int i = _cachedChildren.Count - 1; i >= 0; i--)
            {
                if (_cachedChildren[i] == null)
                {
                    _cachedChildren.RemoveAt(i);
                    _cachedListElement.RemoveAt(i);
                }
            }
        }
    }
}
