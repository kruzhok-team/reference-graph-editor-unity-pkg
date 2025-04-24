using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Focusing
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class DimmingObject : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int _sortOrder = 100;

        private Canvas _canvas;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void EnableDimming(IEnumerable<GameObject> excludeElements)
        {
            gameObject.SetActive(true);

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            _canvas.sortingOrder = _sortOrder;

            foreach (GameObject element in excludeElements)
            {
                if (!element.TryGetComponent(out Canvas canvas))
                {
                    canvas = element.AddComponent<Canvas>();
                }

                if (!element.TryGetComponent(out GraphicRaycaster graphicRaycaster))
                {
                    graphicRaycaster = element.AddComponent<GraphicRaycaster>();
                }

                canvas.overrideSorting = true;
                canvas.sortingOrder = _sortOrder + 1;
            }
        }

        public void DisableDimming(IEnumerable<GameObject> excludeElements)
        {
            foreach (GameObject element in excludeElements)
            {
                if (element.TryGetComponent(out GraphicRaycaster graphicRaycaster))
                {
                    Destroy(graphicRaycaster);
                }

                if (element.TryGetComponent(out Canvas canvas))
                {
                    Destroy(canvas);
                }
            }

            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UIFocusingSystem.Instance.RemoveContextLayer(UIFocusingSystem.Instance.ContextsInOrder.Last());
        }
    }
}
