using System.Diagnostics;
using Bogus;
using Hsp.Midi.Messages;

namespace Hsp.Midi.Test;

[Collection(nameof(VirtualPortTests))]
public class VirtualPortTests : IClassFixture<VirtualPortTestFixture>
{
  private readonly VirtualPortTestFixture _fixture;
  private readonly Faker _faker;


  public VirtualPortTests(VirtualPortTestFixture fixture)
  {
    _fixture = fixture;
    _faker = new Faker();
  }

  private void PreparePort()
  {
    _fixture.Port.Loopback = false;
  }


  private static IMidiMessage CreateMessage(int index)
  {
    if (index == 0) return new SysCommonMessage(SysCommonType.SongPositionPointer, 1);
    if (index == 1) return new ChannelMessage(ChannelCommand.NoteOn, 3, 10, 20);
    if (index == 2) return new SysRealtimeMessage(SysRealtimeType.Start);
    if (index == 3)
      return new SysExMessage(
        (byte)SysExType.Start,
        1, 2, 3, 4,
        (byte)SysExType.End
      );
    throw new IndexOutOfRangeException();
  }

  [Fact]
  public async Task SendMessageToPortWithLoopback()
  {
    PreparePort();
    _fixture.Port.Loopback = true;

    // arrange
    var message = new ChannelMessage(
      ChannelCommand.NoteOn,
      _faker.Random.Int(1, 16),
      _faker.Random.Int(0, 127),
      _faker.Random.Int(0, 127));

    ChannelMessage? receivedMessage = null;

    var inDev = new InputMidiDevice(InputMidiDevicePool.Instance.Get(_fixture.Port.Name));
    inDev.Open();
    inDev.MessageReceived += (s, m) => receivedMessage = m as ChannelMessage;

    var outDev = new OutputMidiDevice(OutputMidiDevicePool.Instance.Get(_fixture.Port.Name));
    outDev.Open();
    outDev.Send(message);

    var sw = Stopwatch.StartNew();
    try
    {
      // assert
      while (receivedMessage == null)
      {
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        if (sw.Elapsed > TimeSpan.FromSeconds(2))
          throw new TimeoutException();
      }

      Assert.Equal(message.GetBytes(), receivedMessage.GetBytes());
    }
    finally
    {
      // cleanup
      InputMidiDevicePool.Instance.Close(inDev);
      OutputMidiDevicePool.Instance.Close(outDev);
    }
  }


  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public async Task ReceiveOnVirtual(int index)
  {
    var msg = CreateMessage(index);
    PreparePort();

    var inDevice = InputMidiDevicePool.Instance.Open(_fixture.Port.Name);
    Assert.IsType<VirtualMidiInputDevice>(inDevice);

    IMidiMessage? receivedMessage = null;
    inDevice.MessageReceived += (s, e) => receivedMessage = e;

    var outDevice = new OutputMidiDevice(OutputMidiDevicePool.Instance.Get(_fixture.Port.Name));
    try
    {
      outDevice.Open();
      outDevice.Send(msg);

      var start = Stopwatch.GetTimestamp();
      while (receivedMessage == null)
      {
        await Task.Delay(100);
        if (TimeSpan.FromTicks(Stopwatch.GetTimestamp() - start) > TimeSpan.FromSeconds(5))
          throw new TimeoutException();
      }
    }
    finally
    {
      outDevice.Close();
    }

    Assert.Equal(msg.GetBytes(), receivedMessage.GetBytes());
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public async Task SendOnVirtual(int index)
  {
    var msg = CreateMessage(index);
    PreparePort();

    var outDevice = OutputMidiDevicePool.Instance.Open(_fixture.Port.Name);
    Assert.IsType<VirtualMidiOutputDevice>(outDevice);

    var inDevice = new InputMidiDevice(InputMidiDevicePool.Instance.Get(_fixture.Port.Name));
    IMidiMessage? receivedMessage = null;
    inDevice.MessageReceived += (s, e) => receivedMessage = e;
    try
    {
      inDevice.Open();

      outDevice.Send(msg);

      var start = Stopwatch.GetTimestamp();
      while (receivedMessage == null)
      {
        await Task.Delay(100);
        if (TimeSpan.FromTicks(Stopwatch.GetTimestamp() - start) > TimeSpan.FromSeconds(5))
          throw new TimeoutException();
      }
    }
    finally
    {
      inDevice.Close();
    }

    Assert.Equal(msg.GetBytes(), receivedMessage.GetBytes());
  }
}