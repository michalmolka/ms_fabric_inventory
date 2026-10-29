using blz0.Authorization;

namespace blz0.SharedServices
{
    public class SharedFabricAuth(FabricAuth fabricAuth)
    {
        public FabricAuth FabricAuth { get; } = fabricAuth;
        public bool IsTokenReady => FabricAuth.tokenAuto != null;
        public event Func<Task>? OnChange;

        public async Task NotifyStateChangedAsync()
        {
            if (OnChange == null)
            {
                return;
            }

            foreach (Func<Task> handler in OnChange.GetInvocationList())
            {
                await handler();
            }
        }
    }
}
