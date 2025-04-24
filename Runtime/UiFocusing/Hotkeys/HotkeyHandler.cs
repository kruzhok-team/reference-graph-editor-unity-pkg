using System.Collections.Generic;
using System.Linq;
using UI.Focusing;
using UnityEngine.InputSystem;

public class HotkeyHandler : IHotkeyHandler
{
    private readonly Dictionary<IContextLayer, List<HotkeyAction>> _mappingToActions = new();
    private readonly Dictionary<InputActionReference, List<HotkeyAction>> _hotkeyChains = new();

    public void RegisterHotkeysMapping(IContextLayer contextLayer)
    {
        if (contextLayer == null)
        {
            return;
        }

        _mappingToActions[contextLayer] = contextLayer.HotkeysMapping.ToList();

        RefreshAll();
    }

    public void UnregisterHotkeysMapping(IContextLayer contextLayer)
    {
        if (contextLayer == null)
        {
            return;
        }

        _mappingToActions.Remove(contextLayer);

        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (KeyValuePair<InputActionReference, List<HotkeyAction>> chain in _hotkeyChains)
        {
            InputActionReference inputRef = chain.Key;

            foreach (HotkeyAction action in chain.Value)
            {
                action.UnsubscribeFrom(inputRef);
            }
        }

        _hotkeyChains.Clear();

        foreach (IContextLayer ctx in UIFocusingSystem.Instance.ContextsInOrder)
        {
            List<HotkeyAction> actions = _mappingToActions[ctx];

            foreach (HotkeyAction action in actions)
            {
                foreach (InputActionReference inputRef in action.Hotkeys)
                {
                    if (!_hotkeyChains.TryGetValue(inputRef, out List<HotkeyAction> chain))
                    {
                        chain = new List<HotkeyAction>();
                        _hotkeyChains[inputRef] = chain;
                    }

                    chain.Add(action);
                }
            }
        }

        foreach (InputActionReference inputRef in _hotkeyChains.Keys)
        {
            UpdateActiveHandler(inputRef);
        }
    }

    private void UpdateActiveHandler(InputActionReference inputRef)
    {
        List<HotkeyAction> chain = _hotkeyChains[inputRef];

        foreach (HotkeyAction action in chain)
        {
            action.UnsubscribeFrom(inputRef);
        }

        foreach (IContextLayer ctx in Enumerable.Reverse(UIFocusingSystem.Instance.ContextsInOrder))
        {
            List<HotkeyAction> actions = _mappingToActions[ctx];
            bool hasThisKey = actions.Any(a => a.Hotkeys.Contains(inputRef));

            if (ctx.BlockOtherHotkeys)
            {
                if (hasThisKey)
                {
                    foreach (HotkeyAction a in actions.Where(a => a.Hotkeys.Contains(inputRef)))
                    {
                        a.SubscribeTo(inputRef);
                    }
                }
                return;
            }
            else if (hasThisKey)
            {
                HotkeyAction a = actions.Last(act => act.Hotkeys.Contains(inputRef));
                a.SubscribeTo(inputRef);

                return;
            }
        }
    }
}
