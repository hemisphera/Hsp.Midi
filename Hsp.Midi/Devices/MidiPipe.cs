using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class MidiPipe : IInputMidiDevice, IOutputMidiDevice
{
  public IInputMidiDevice InputMidiDevice { get; }

  public IOutputMidiDevice OutputMidiDevice { get; }

  public Predicate<IMidiMessage> ForwardFilter { get; set; }

  public string Name => $"{InputMidiDevice.Name} => {OutputMidiDevice.Name}";

  public int DeviceId => -1;


  public event EventHandler<IMidiMessage>? MessageReceived;

  public event EventHandler<MessageForwardEventArgs>? MessageReceivedForForward;


  public MidiPipe(IInputMidiDevice inputMidiDevice, IOutputMidiDevice outputMidiDevice)
  {
    InputMidiDevice = inputMidiDevice;
    OutputMidiDevice = outputMidiDevice;
  }

  public void Open()
  {
    InputMidiDevice.MessageReceived += InputDevice_MessageReceived;
  }

  public void Reset()
  {
  }

  public void Close()
  {
    InputMidiDevice.MessageReceived -= InputDevice_MessageReceived;
  }

  public void Send(IMidiMessage message)
  {
    OutputMidiDevice.Send(message);
  }


  private void InputDevice_MessageReceived(object? sender, IMidiMessage e)
  {
    MessageReceived?.Invoke(this, e);
    var forwardEventArgs = new MessageForwardEventArgs(e);

    var canForward = ForwardFilter?.Invoke(e) ?? true;
    if (!canForward) return;

    MessageReceivedForForward?.Invoke(this, forwardEventArgs);
    foreach (var msg in forwardEventArgs.OutputMessages)
    {
      OutputMidiDevice.Send(msg);
    }
  }


  public override string ToString()
  {
    return $"{InputMidiDevice.Name} => {OutputMidiDevice.Name}";
  }
}