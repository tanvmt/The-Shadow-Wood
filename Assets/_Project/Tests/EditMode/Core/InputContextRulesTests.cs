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
            Assert.That(rule.Pause, Is.True);
            Assert.That(rule.Flashlight, Is.True);
            Assert.That(rule.CursorLocked, Is.True);
        }

        // TV, puzzles and menus must not toggle the beam behind the player's back.
        [TestCase(InputContext.Focus)]
        [TestCase(InputContext.Puzzle)]
        [TestCase(InputContext.UI)]
        public void NonGameplayContexts_BlockFlashlight(InputContext context)
        {
            Assert.That(InputContextRules.Get(context).Flashlight, Is.False);
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

        // Pause and Cancel share Esc; only one of them may be live per context.
        [Test]
        public void EveryContext_EnablesExactlyOneOfPauseAndCancel()
        {
            foreach (InputContext context in Enum.GetValues(typeof(InputContext)))
            {
                InputContextRule rule = InputContextRules.Get(context);
                Assert.That(rule.Pause, Is.Not.EqualTo(rule.Cancel), $"{context} enables both or neither.");
            }
        }

        [TestCase(InputContext.Puzzle)]
        [TestCase(InputContext.UI)]
        public void PointerContexts_FreeTheCursor(InputContext context)
        {
            Assert.That(InputContextRules.Get(context).CursorLocked, Is.False);
        }
    }
}
