using System;

namespace UniversalTwin.Client.Services
{
    public class StateService
    {
        public string CurrentIndustry { get; set; } = "recycler";

        public event Action? OnChange;

        public void SetIndustry(string industry)
        {
            CurrentIndustry = industry;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
