using Net.Core.Messages;
using Net.Core.ResourceParser;
using Net.Core.Server.Connection.Identity;

// In VisualStudio2022 there is like 40000000000 recommendations...
// Don't use them, it will only recommend a different thing each time.

namespace Net.Tests
{
    public class Tests
    {
        public ResourceConversionEngine<NetMessage<DefaultId>, DefaultId>
            _engine;

        [SetUp]
        public void Setup()
        {
            /*initialize the engine*/
            _engine = new ResourceConversionEngine<NetMessage<DefaultId>, DefaultId>();
        }

        [Test]
        public void VerifyBasicParse()
        {
            const string eventId = "disconnecting";
            var msg = _engine.Parse($"{eventId}?reason=failed to X");

            if (msg is null)
            {
                Assert.Fail("Engine failed to parse string");
            }

            Assert.That(msg!.EventId, Is.EqualTo(eventId));
            _engine.Reset();
        }

        [Test]
        public void VerifyLongParse()
        {
            const string eventString = 
                "disconnect?reason=0x999&msgType=MessageType.Failure&code=-1&msg=Failed to establish connection";
            var msg = _engine.Parse(eventString);

            if (msg is null)
            {
                Assert.Fail("Engine failed to parse string");
            }

            Assert.That(msg!.EventId, Is.EqualTo("disconnect"));

            Assert.That(msg!.Properties.ContainsKey("reason"));
            Assert.That(msg!.Properties["reason"], Is.EqualTo("0x999"));

            Assert.That(msg!.Properties.ContainsKey("reason"));
            Assert.That(msg!.Properties["msgType"], Is.EqualTo("MessageType.Failure"));

            // code=-1
            Assert.That(msg!.Properties.ContainsKey("code"));
            Assert.That(msg!.Properties["code"], Is.EqualTo("-1"));

            // msg=Failed to establish connection
            Assert.That(msg!.Properties.ContainsKey("msg"));
            Assert.That(msg!.Properties["msg"], Is.EqualTo("Failed to establish connection"));

            Assert.That(msg!.Properties.ContainsKey("msgType"));
            _engine.Reset();
        }

        [Test]
        public void TestStringLiteralInsideResourceString()
        {
            var resource = _engine.Parse("help?text='hello world!'");
            System.Console.WriteLine(resource);

            Assert.That(resource, Is.Not.Null);

            Assert.That(resource.EventId == "help");
            Assert.That(resource.Properties.ContainsKey("text"));
            Assert.That(resource.Properties["text"], Is.EqualTo("hello world!"));

            _engine.Reset();
        }
    }
}