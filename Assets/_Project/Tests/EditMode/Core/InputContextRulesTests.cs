using System;
using NUnit.Framework;
using TheShadowWood.Core.Input;

namespace TheShadowWood.Core.Tests
{
    public sealed class InputContextRulesTests
    {
        [Test]
        public void EveryContext_HasARule()
        {
            foreach (InputContext context in Enum.GetValues(typeof(InputContext)))
            {
                Assert.DoesNotThrow(() => InputContextRules.Get(context), $"{context} has no rule.");
            }
        }

        [Test]
        public void Gameplay_AllowsMovementAndLocksCursor()
        {
            InputContextRule rule = InputContextRules.Get(InputContext.Gameplay);

            Assert.That(rule.Move && rule.Look && rule.Sprint && rule.Crouch && rule.Interact, Is.True);
            Assert.That(rule.Cancel, Is.False);
            Assert.That(rule.CursorLocked, Is.True);
        }

        [TestCase(InputContext.Focus)]
        [TestCase(InputContext.Puzzle)]
        [TestCase(InputContext.UI)]
        public void NonGameplayContexts_BlockMovementAndAllowCancel(InputContext context)
        {
            InputContextRule rule = InputContextRules.Get(context);

            Assert.That(rule.Move || rule.Look || rule.Sprint || rule.Crouch, Is.False);
            Assert.That(rule.Cancel, Is.True);
        }

        [TestCase(InputContext.Puzzle)]
        [TestCase(InputContext.UI)]
        public void PointerContexts_FreeTheCursor(InputContext context)
        {
            Assert.That(InputContextRules.Get(context).CursorLocked, Is.False);
        }
    }
}
