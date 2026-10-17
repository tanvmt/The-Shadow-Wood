using NUnit.Framework;

namespace TheShadowWood.UI.Tests
{
    public sealed class SubtitleServiceTests
    {
        private FakeSubtitleView _view;
        private SubtitleService _service;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeSubtitleView();
            _service = new SubtitleService(_view);
        }

        [Test]
        public void Show_DisplaysText()
        {
            _service.Show("Ai đó đang khóc...", 2f);

            Assert.That(_service.IsShowing, Is.True);
            Assert.That(_view.Text, Is.EqualTo("Ai đó đang khóc..."));
        }

        [Test]
        public void TimedLine_HidesAfterDuration()
        {
            _service.Show("line", 2f);

            _service.Advance(1.5f);
            Assert.That(_service.IsShowing, Is.True);

            _service.Advance(0.6f);
            Assert.That(_service.IsShowing, Is.False);
            Assert.That(_view.Text, Is.Null);
        }

        [Test]
        public void NewLine_RestartsTimer()
        {
            _service.Show("first", 2f);
            _service.Advance(1.5f);

            _service.Show("second", 2f);
            _service.Advance(1.5f);

            Assert.That(_service.IsShowing, Is.True);
            Assert.That(_view.Text, Is.EqualTo("second"));
        }

        [Test]
        public void ZeroDuration_StaysUntilHide()
        {
            _service.Show("line", 0f);
            _service.Advance(100f);
            Assert.That(_service.IsShowing, Is.True);

            _service.Hide();
            Assert.That(_service.IsShowing, Is.False);
        }

        [Test]
        public void EmptyText_Hides()
        {
            _service.Show("line", 0f);

            _service.Show(string.Empty, 2f);

            Assert.That(_service.IsShowing, Is.False);
        }

        private sealed class FakeSubtitleView : ISubtitleView
        {
            public string Text { get; private set; }

            public void Display(string text) => Text = text;
            public void Clear() => Text = null;
        }
    }
}
