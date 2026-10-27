using GodOfWar.Combat;
using NUnit.Framework;

namespace GodOfWar.Tests.Combat
{
    public class HitOutcomeResolverTests
    {
        [Test]
        public void Resolve_WithOpenStance_ReturnsHit()
        {
            Assert.That(HitOutcomeResolver.Resolve(DefenseStance.Open), Is.EqualTo(HitOutcome.Hit));
        }

        [Test]
        public void Resolve_WithEvadingStance_ReturnsEvaded()
        {
            Assert.That(HitOutcomeResolver.Resolve(DefenseStance.Evading), Is.EqualTo(HitOutcome.Evaded));
        }
    }
}
