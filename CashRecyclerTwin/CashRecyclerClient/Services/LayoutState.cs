using System;

namespace CashRecyclerClient.Services
{
    public class LayoutState
    {
        public bool IsFullScreen { get; private set; }
        public event Action? OnChange;

        public void SetFullScreen(bool isFullScreen)
        {
            IsFullScreen = isFullScreen;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
