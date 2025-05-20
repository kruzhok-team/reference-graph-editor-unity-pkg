using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Focusing
{
    public class UIFocusingSystem
    {
        private IHotkeyHandler _hotkeyHandler;
        private IDimmingHandler _dimmingHandler;
        public ISelectionHandler SelectionHandler { get; private set; }

        private Stack<IContextLayer> _contextStack = new();
        public IEnumerable<IContextLayer> Contexts => _contextStack;

        public static UIFocusingSystem Instance { get; private set; }

        public UIFocusingSystem(IHotkeyHandler hotkeyHandler, IDimmingHandler dimmingHandler, ISelectionHandler selectionHandler)
        {
            _hotkeyHandler = hotkeyHandler;
            _dimmingHandler = dimmingHandler;
            SelectionHandler = selectionHandler;

            Instance = this;
        }

        #region CONTEXT

        public void PushContextLayer(IContextLayer context)
        {
            if (_contextStack.Contains(context))
            {
                return;
            }

            _contextStack.Push(context);
            _hotkeyHandler.RegisterHotkeysMapping(context);

            context.Activate();

            if (context.FocusedElements != null && context.FocusedElements.Count() > 0)
            {
                _dimmingHandler.EnableDimming(context.FocusedElements);
            }
        }

        public void RemoveContextLayer(IContextLayer context)
        {
            if (!_contextStack.Contains(context))
            {
                Debug.LogWarning("Can`t remove context layer, because there is no such context in stack!");
                return;
            }

            _dimmingHandler.DisableDimming();

            while (_contextStack.Count > 0)
            {
                IContextLayer current = _contextStack.Pop();

                current.Deactivate();
                _hotkeyHandler.UnregisterHotkeysMapping(current);

                if (current == context)
                {
                    break;
                }
            }

            if (_contextStack.Count > 0)
            {
                PushContextLayer(_contextStack.Peek());
            }
        }

        public void GoToContextLayer(IContextLayer context)
        {
            while (_contextStack.Count > 0)
            {
                IContextLayer current = _contextStack.Pop();

                _hotkeyHandler.UnregisterHotkeysMapping(current);
                context.Deactivate();

                if (current == context)
                {
                    break;
                }
            }

            PushContextLayer(context);
        }

        public void ResetAllContextsLayers()
        {
            while (_contextStack.Count > 0)
            {
                IContextLayer context = _contextStack.Pop();

                _hotkeyHandler.UnregisterHotkeysMapping(context);

                context.Deactivate();
            }

            _dimmingHandler.DisableDimming();
        }

        #endregion

        #region SELECTION

        public void Select(ISelectable selectable)
        {
            SelectionHandler.Select(selectable);
        }

        public void Deselect(ISelectable selectable)
        {
            SelectionHandler.Deselect(selectable);
        }

        #endregion
    }
}

