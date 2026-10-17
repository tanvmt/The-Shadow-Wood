using NUnit.Framework;
using TheShadowWood.Core.Input;
using TheShadowWood.Interaction.Tests;

namespace TheShadowWood.UI.Tests
{
    public sealed class UIScreenServiceTests
    {
        private FakeInputService _input;
        private UIScreenService _service;

        [SetUp]
        public void SetUp()
        {
            _input = new FakeInputService();
            _service = new UIScreenService(_input);
        }

        [Test]
        public void Open_ShowsScreenAndPushesUIContext()
        {
            FakeScreen screen = new FakeScreen();

            _service.Open(screen);

            Assert.That(screen.Shown && screen.Interactive, Is.True);
            Assert.That(_service.Top, Is.SameAs(screen));
            Assert.That(_input.Context, Is.EqualTo(InputContext.UI));
        }

        [Test]
        public void Open_SameScreenTwice_IsIgnored()
        {
            FakeScreen screen = new FakeScreen();

            _service.Open(screen);
            _service.Open(screen);
            _service.Close(screen);

            Assert.That(_service.HasOpenScreen, Is.False);
            Assert.That(_input.Context, Is.EqualTo(InputContext.Gameplay));
        }

        [Test]
        public void Close_HidesScreenAndRestoresContext()
        {
            FakeScreen screen = new FakeScreen();
            _service.Open(screen);

            _service.Close(screen);

            Assert.That(screen.Shown || screen.Interactive, Is.False);
            Assert.That(_service.HasOpenScreen, Is.False);
            Assert.That(_input.Context, Is.EqualTo(InputContext.Gameplay));
        }

        [Test]
        public void StackedScreens_OnlyTopIsInteractive()
        {
            FakeScreen pause = new FakeScreen();
            FakeScreen settings = new FakeScreen();

            _service.Open(pause);
            _service.Open(settings);

            Assert.That(pause.Shown, Is.True);
            Assert.That(pause.Interactive, Is.False);
            Assert.That(settings.Interactive, Is.True);

            _service.Close(settings);

            Assert.That(_service.Top, Is.SameAs(pause));
            Assert.That(pause.Interactive, Is.True);
            Assert.That(_input.Context, Is.EqualTo(InputContext.UI));
        }

        [Test]
        public void Cancel_ClosesOnlyTopScreen()
        {
            FakeScreen pause = new FakeScreen();
            FakeScreen settings = new FakeScreen();
            _service.Open(pause);
            _service.Open(settings);

            _input.CancelPressed = true;
            _service.Tick();

            Assert.That(_service.IsOpen(settings), Is.False);
            Assert.That(_service.Top, Is.SameAs(pause));
        }

        [Test]
        public void Cancel_KeepsScreenThatDoesNotCloseOnCancel()
        {
            FakeScreen menu = new FakeScreen { CloseOnCancel = false };
            _service.Open(menu);

            _input.CancelPressed = true;
            _service.Tick();

            Assert.That(_service.IsOpen(menu), Is.True);
        }

        [Test]
        public void Dispose_ReturnsEveryContext()
        {
            _service.Open(new FakeScreen());
            _service.Open(new FakeScreen());

            _service.Dispose();

            Assert.That(_service.HasOpenScreen, Is.False);
            Assert.That(_input.Context, Is.EqualTo(InputContext.Gameplay));
        }

        private sealed class FakeScreen : IUIScreen
        {
            public bool CloseOnCancel { get; set; } = true;
            public bool Shown { get; private set; }
            public bool Interactive { get; private set; }

            public void Show() => Shown = true;
            public void Hide() => Shown = false;
            public void SetInteractive(bool interactive) => Interactive = interactive;
        }
    }
}
