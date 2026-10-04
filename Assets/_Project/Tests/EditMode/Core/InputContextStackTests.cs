using System;
using System.Collections.Generic;
using NUnit.Framework;
using TheShadowWood.Core.Input;

namespace TheShadowWood.Core.Tests
{
    public sealed class InputContextStackTests
    {
        private InputContextStack _stack;
        private List<InputContext> _changes;

        [SetUp]
        public void SetUp()
        {
            _stack = new InputContextStack(InputContext.Gameplay);
            _changes = new List<InputContext>();
            _stack.Changed += _changes.Add;
        }

        [Test]
        public void Current_IsBaseContext_WhenEmpty()
        {
            Assert.That(_stack.Current, Is.EqualTo(InputContext.Gameplay));
            Assert.That(_stack.Depth, Is.Zero);
        }

        [Test]
        public void Push_MakesContextCurrent_AndDisposeRestoresPrevious()
        {
            IDisposable focus = _stack.Push(InputContext.Focus);
            Assert.That(_stack.Current, Is.EqualTo(InputContext.Focus));

            focus.Dispose();

            Assert.That(_stack.Current, Is.EqualTo(InputContext.Gameplay));
            Assert.That(_changes, Is.EqualTo(new[] { InputContext.Focus, InputContext.Gameplay }));
        }

        [Test]
        public void NestedPushes_UnwindInReverseOrder()
        {
            IDisposable puzzle = _stack.Push(InputContext.Puzzle);
            IDisposable ui = _stack.Push(InputContext.UI);

            ui.Dispose();
            Assert.That(_stack.Current, Is.EqualTo(InputContext.Puzzle));

            puzzle.Dispose();
            Assert.That(_stack.Current, Is.EqualTo(InputContext.Gameplay));
        }

        [Test]
        public void DisposingLowerEntryFirst_KeepsTopContext()
        {
            IDisposable puzzle = _stack.Push(InputContext.Puzzle);
            IDisposable ui = _stack.Push(InputContext.UI);
            _changes.Clear();

            puzzle.Dispose();

            Assert.That(_stack.Current, Is.EqualTo(InputContext.UI));
            Assert.That(_stack.Depth, Is.EqualTo(1));
            Assert.That(_changes, Is.Empty, "Removing a non-top entry must not raise Changed.");

            ui.Dispose();
            Assert.That(_stack.Current, Is.EqualTo(InputContext.Gameplay));
        }

        [Test]
        public void DisposingTwice_IsNoOp()
        {
            IDisposable ui = _stack.Push(InputContext.UI);
            IDisposable focus = _stack.Push(InputContext.Focus);

            focus.Dispose();
            focus.Dispose();

            Assert.That(_stack.Current, Is.EqualTo(InputContext.UI));
            Assert.That(_stack.Depth, Is.EqualTo(1));

            ui.Dispose();
        }

        [Test]
        public void PushingSameContextAsCurrent_DoesNotRaiseChanged()
        {
            IDisposable gameplay = _stack.Push(InputContext.Gameplay);

            Assert.That(_changes, Is.Empty);
            Assert.That(_stack.Depth, Is.EqualTo(1));

            gameplay.Dispose();
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void TwoOwnersOfSameContext_StayActiveUntilBothRelease()
        {
            IDisposable first = _stack.Push(InputContext.Focus);
            IDisposable second = _stack.Push(InputContext.Focus);

            first.Dispose();
            Assert.That(_stack.Current, Is.EqualTo(InputContext.Focus));

            second.Dispose();
            Assert.That(_stack.Current, Is.EqualTo(InputContext.Gameplay));
        }
    }
}
