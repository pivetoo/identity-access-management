using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class SubscriptionTests
    {
        [Test]
        public void GrantsAccess_WhenActive_ReturnsTrue()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, now, now.AddMonths(1));

            Assert.That(subscription.GrantsAccess(now), Is.True);
        }

        [Test]
        public void GrantsAccess_WhenPastDue_ReturnsTrue()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, now, now.AddMonths(1));
            subscription.MarkPastDue();

            Assert.That(subscription.GrantsAccess(now), Is.True);
        }

        [Test]
        public void GrantsAccess_WhenTrialingWithinTrial_ReturnsTrue()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartTrialing(1, 1, now, 14);

            Assert.That(subscription.GrantsAccess(now), Is.True);
        }

        [Test]
        public void GrantsAccess_WhenTrialingExactlyAtBoundary_ReturnsTrue()
        {
            DateTimeOffset start = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartTrialing(1, 1, start, 14);
            DateTimeOffset boundary = start.AddDays(14);

            Assert.That(subscription.GrantsAccess(boundary), Is.True);
        }

        [Test]
        public void GrantsAccess_WhenTrialingExpired_ReturnsFalse()
        {
            DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-15);
            Subscription subscription = Subscription.StartTrialing(1, 1, start, 14);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Assert.That(subscription.GrantsAccess(now), Is.False);
        }

        [Test]
        public void GrantsAccess_WhenSuspended_ReturnsFalse()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, now, now.AddMonths(1));
            subscription.Suspend();

            Assert.That(subscription.GrantsAccess(now), Is.False);
        }

        [Test]
        public void GrantsAccess_WhenCanceled_ReturnsFalse()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, now, now.AddMonths(1));
            subscription.Cancel(now);

            Assert.That(subscription.GrantsAccess(now), Is.False);
        }
    }
}
