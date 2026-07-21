using System;
using Kingdom.Contracts;

namespace Kingdom.Core
{
    public sealed class GameSession
    {
        public event Action Changed;

        public GuestAuthData Authentication { get; private set; }
        public BootstrapData Bootstrap { get; private set; }

        public bool IsAuthenticated =>
            Authentication != null &&
            !string.IsNullOrWhiteSpace(Authentication.access_token);

        public bool HasBootstrap => Bootstrap?.player != null && Bootstrap.city != null;

        public void ApplyGuestAuthentication(GuestAuthData authentication)
        {
            if (authentication == null)
                throw new ArgumentNullException(nameof(authentication));
            if (string.IsNullOrWhiteSpace(authentication.access_token))
                throw new ArgumentException("Authentication must include an access token.", nameof(authentication));

            Authentication = authentication;
            Bootstrap = null;
            Changed?.Invoke();
        }

        public void ApplyBootstrap(BootstrapData bootstrap)
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Authenticate before applying bootstrap data.");
            if (bootstrap?.player == null || bootstrap.city == null)
                throw new ArgumentException("Bootstrap data must include player and city state.", nameof(bootstrap));

            Bootstrap = bootstrap;
            Changed?.Invoke();
        }

        public void Clear()
        {
            Authentication = null;
            Bootstrap = null;
            Changed?.Invoke();
        }
    }
}
