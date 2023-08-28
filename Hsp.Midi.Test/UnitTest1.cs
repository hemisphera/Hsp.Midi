using Bogus;
using FluentAssertions;
using Hsp.Midi.Messages;
using Hsp.Midi.TeVirtualMidi;

namespace Hsp.Midi.Test;

public class UnitTest1
{

  private Faker Any { get; }


  public UnitTest1()
  {
    Randomizer.Seed = new Random(1);
    Any = new Faker();
  }



  [Fact]
  public void Test1()
  {
    var devices = MidiDevice.EnumerateAll();
    Assert.NotEmpty(devices);
  }

  [Fact]
  public async Task CreateAndDestroyPort()
  {
    // arrange
    var portName = Any.Random.Word();

    OutputDevice odi = null;
    InputDevice idi = null;

    // act
    using (var tep = new TeVirtualMidiPort(portName))
    {
      odi = new OutputDevice(portName);
      odi.Open();

      idi = new InputDevice(portName);
      idi.Open();

      // assert
      odi.DeviceInfo.Name.Should().Be(portName);
      odi.DeviceInfo.DriverVersion.Should().NotBeNull();
      odi.Close();

      idi.DeviceInfo.Name.Should().Be(portName);
      idi.DeviceInfo.DriverVersion.Should().NotBeNull();
      idi.Close();
    }

    Assert.ThrowsAny<Exception>(() => odi.Open());
    Assert.ThrowsAny<Exception>(() => idi.Open());

    await Task.CompletedTask;
  }


  [Fact]
  public async Task SendMessageToPort()
  {
    // arrange
    const string name = "loopMIDI Port";

    var message = new ChannelMessage(
      ChannelCommand.NoteOn,
      Any.Random.Int(1, 16),
      Any.Random.Int(0, 127),
      Any.Random.Int(0, 127));

    ChannelMessage receivedMessage = null;

    //using var tep = new TeVirtualMidiPort(name);
    //var x = tep.GetProcessIds();
    //while (true) await Task.Delay(100);

    var inDev = new InputDevice(name);
    inDev.Open();
    inDev.ChannelMessageReceived += (s, m) => receivedMessage = m;

    var outDev = new OutputDevice(name);
    outDev.Open();

    // act
    outDev.Send(message);

    try
    {
      // assert
      var start = DateTime.Now;
      while (receivedMessage == null)
      {
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        if (DateTime.Now.Subtract(start) > TimeSpan.FromSeconds(5))
          throw new TimeoutException();
      }

      receivedMessage.MidiChannel.Should().Be(message.MidiChannel);
      receivedMessage.Data1.Should().Be(message.Data1);
      receivedMessage.Data2.Should().Be(message.Data2);
    }
    finally
    {
      // cleanup
      outDev.Close();
      inDev.Close();
      //tep.Dispose();
    }
  }


  [Fact]
  public async Task SendMessageToPortOnAdhocPort()
  {
    // arrange
    const string name = "asd";

    var message = new ChannelMessage(
      ChannelCommand.NoteOn,
      Any.Random.Int(1, 16),
      Any.Random.Int(0, 127),
      Any.Random.Int(0, 127));

    ChannelMessage receivedMessage = null;

    using var tep = new TeVirtualMidiPort(name);
    //tep.SendCommand(message);

    var inDev = new InputDevice(name);
    inDev.Open();
    inDev.Reset();
    inDev.ChannelMessageReceived += (s, m) => receivedMessage = m;

    var outDev = new OutputDevice(name);
    outDev.Open();

    // act
    outDev.Reset();
    outDev.Send(message);

    try
    {
      // assert
      var start = DateTime.Now;
      while (receivedMessage == null)
      {
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        if (DateTime.Now.Subtract(start) > TimeSpan.FromSeconds(5))
          throw new TimeoutException();
      }

      receivedMessage.MidiChannel.Should().Be(message.MidiChannel);
      receivedMessage.Data1.Should().Be(message.Data1);
      receivedMessage.Data2.Should().Be(message.Data2);
    }
    finally
    {
      // cleanup
      outDev.Close();
      inDev.Close();
      tep.Dispose();
    }
  }
  private void InDev_MessageReceived(object? sender, IMidiMessage e)
  {
    throw new NotImplementedException();
  }
}