using System;
using System.Collections.Generic;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class MidiPipe : IInputMidiDevice, IOutputMidiDevice
{
  public InputMidiDevice InputMidiDevice { get; }

  public OutputMidiDevice OutputMidiDevice { get; }

  public Predicate<IMidiMessage> ForwardFilter { get; set; }

  public string Name => $"{InputMidiDevice.Name} => {OutputMidiDevice.Name}";


  public event EventHandler<IMidiMessage> MessageReceived;

  public event EventHandler<MessageForwardEventArgs> MessageReceivedForForward;


  public MidiPipe(InputMidiDevice inputMidiDevice, OutputMidiDevice outputMidiDevice)
  {
    InputMidiDevice = inputMidiDevice;
    OutputMidiDevice = outputMidiDevice;
  }

  public MidiPipe(string inputDeviceName, string outputDeviceName)
    : this(
      InputMidiDevicePool.Instance.Open(inputDeviceName),
      OutputMidiDevicePool.Instance.Open(outputDeviceName))
  {
    InputMidiDevice.MessageReceived += InputDevice_MessageReceived;
  }

  public void Close()
  {
    InputMidiDevice.MessageReceived -= InputDevice_MessageReceived;
    InputMidiDevicePool.Instance.Close(InputMidiDevice);
    OutputMidiDevicePool.Instance.Close(OutputMidiDevice);
  }

  public void Send(IMidiMessage message)
  {
    OutputMidiDevice.Send(message);
  }


  private void InputDevice_MessageReceived(object sender, IMidiMessage e)
  {
    MessageReceived?.Invoke(this, e);
    var forwardEventArgs = new MessageForwardEventArgs(e);

    var canForward = ForwardFilter?.Invoke(e) ?? true;
    if (!canForward) return;

    MessageReceivedForForward?.Invoke(this, forwardEventArgs);
    foreach (var msg in forwardEventArgs.OutputMessages ?? Array.Empty<IMidiMessage>())
    {
      OutputMidiDevice.Send(msg);
    }
  }


  public override string ToString()
  {
    return $"{InputMidiDevice.DeviceInfo.Name} => {OutputMidiDevice.DeviceInfo.Name}";
  }
}