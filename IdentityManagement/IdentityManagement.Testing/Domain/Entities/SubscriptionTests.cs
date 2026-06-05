using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;

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

        [Test]
        public void Block_sets_flag_reason_and_time_without_changing_status()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, now, now.AddMonths(1));
            subscription.MarkPastDue();

            subscription.Block(SubscriptionBlockReason.PaymentOverdue, now);

            Assert.Multiple(() =>
            {
                Assert.That(subscription.IsBlocked, Is.True);
                Assert.That(subscription.BlockReason, Is.EqualTo(SubscriptionBlockReason.PaymentOverdue));
                Assert.That(subscription.BlockedAt, Is.EqualTo(now));
                Assert.That(subscription.Status, Is.EqualTo(SubscriptionStatus.PastDue));
            });
        }

        [Test]
        public void Block_when_already_blocked_with_same_reason_is_noop()
        {
            DateTimeOffset first = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset second = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, first, first.AddMonths(1));
            subscription.MarkPastDue();

            subscription.Block(SubscriptionBlockReason.PaymentOverdue, first);
            subscription.Block(SubscriptionBlockReason.PaymentOverdue, second);

            Assert.That(subscription.BlockedAt, Is.EqualTo(first));
        }

        [Test]
        public void Unblock_clears_flag_reason_and_time()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, now, now.AddMonths(1));
            subscription.Block(SubscriptionBlockReason.PaymentOverdue, now);

            subscription.Unblock();

            Assert.Multiple(() =>
            {
                Assert.That(subscription.IsBlocked, Is.False);
                Assert.That(subscription.BlockReason, Is.EqualTo(SubscriptionBlockReason.None));
                Assert.That(subscription.BlockedAt, Is.Null);
            });
        }

        [Test]
        public void GrantsAccess_is_unaffected_by_block_flag()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartActive(1, 1, now, now.AddMonths(1));
            subscription.MarkPastDue();
            subscription.Block(SubscriptionBlockReason.PaymentOverdue, now);

            // O bloqueio e ortogonal: o gate combina IsBlocked com GrantsAccess, mas GrantsAccess
            // sozinho continua refletindo apenas o Status.
            Assert.That(subscription.GrantsAccess(now), Is.True);
        }
    }
}
