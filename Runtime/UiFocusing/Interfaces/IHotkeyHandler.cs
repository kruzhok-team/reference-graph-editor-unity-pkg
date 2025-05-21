namespace UI.Focusing
{
    public interface IHotkeyHandler
    {
        void RegisterHotkeysMapping(IContextLayer contextLayer);
        void UnregisterHotkeysMapping(IContextLayer contextLayer);
    }
}
