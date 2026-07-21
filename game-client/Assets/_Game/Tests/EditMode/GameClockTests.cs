using System;
using Kingdom.Core;
using NUnit.Framework;

namespace Kingdom.Tests
{
    public sealed class GameClockTests
    {
        [Test]
        public void UtcNow_BeforeSynchronizationTracksSystemUtc()
        {
            DateTimeOffset before = DateTimeOffset.UtcNow;
            var clock = new GameClock();
            DateTimeOffset observed = clock.UtcNow;
            DateTimeOffset after = DateTimeOffset.UtcNow;

            Assert.That(observed, Is.InRange(before, after));
        }

        [Test]
        public void Synchronize_UsesServerTimeOffset()
        {
            var clock = new GameClock();
            DateTimeOffset expected = DateTimeOffset.UtcNow.AddMinutes(5);
            clock.Synchronize(expected);
            Assert.That(clock.UtcNow, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(1)));
        }
    }
}
