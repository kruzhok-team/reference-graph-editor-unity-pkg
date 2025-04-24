using System.Collections.Generic;
using UnityEngine;

namespace UI.Focusing
{
    public class DimmingHandler : IDimmingHandler
    {
        private readonly DimmingObject _dimmingObject;

        public DimmingHandler(DimmingObject dimmingObject)
        {
            _dimmingObject = GameObject.Instantiate(dimmingObject);
            _dimmingObject.gameObject.SetActive(false);
        }

        private IEnumerable<GameObject> _currentFocused;

        public void EnableDimming(IEnumerable<GameObject> FocusedElements)
        {
            if (_currentFocused != null)
            {
                DisableDimming();
            }

            _currentFocused = FocusedElements;

            _dimmingObject.EnableDimming(_currentFocused);
        }

        public void DisableDimming()
        {
            if (_currentFocused == null)
            {
                return;
            }

            _dimmingObject.DisableDimming(_currentFocused);

            _currentFocused = null;
        }
    }
}
