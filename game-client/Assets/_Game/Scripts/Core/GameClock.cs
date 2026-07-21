using System;

namespace Kingdom.Core
{
    public sealed class GameClock
    {
        private TimeSpan serverOffset = TimeSpan.Zero;

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow + serverOffset;

        public void Synchronize(DateTimeOffset serverTime)
        {
            serverOffset = serverTime - DateTimeOffset.UtcNow;
        }
    }
}

