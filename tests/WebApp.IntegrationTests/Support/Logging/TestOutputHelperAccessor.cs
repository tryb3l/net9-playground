using MartinCostello.Logging.XUnit;
using Xunit.Sdk;

namespace WebApp.IntegrationTests.Support.Logging;

public sealed class TestOutputHelperAccessor : IMessageSinkAccessor
{
    private ITestOutputHelper? _output;
    
    public IMessageSink? MessageSink { get; set; }

    public void SetOutput(ITestOutputHelper output) => _output = output;
    
    public IMessageSink GetEffectiveSink() =>
        MessageSink ?? (_output is null ? new NullMessageSink() : new TestOutputHelperMessageSink(_output));

    private sealed class TestOutputHelperMessageSink(ITestOutputHelper output) : IMessageSink
    {
        public bool OnMessage(IMessageSinkMessage message)
        {
            output.WriteLine(message.ToString() ?? string.Empty);
            return true;
        }
    }

    private sealed class NullMessageSink : IMessageSink
    {
        public bool OnMessage(IMessageSinkMessage message) => true;
    }
}