using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class MidiPipe : IInputMidiDevice, IOutputMidiDevice
{

  public InputMidiMidiDevice InputMidiMidiDevice { get; }

  public OutputMidiDevice OutputMidiDevice { get; }

  public bool IsOpen { get; private set; }


  public event EventHandler<IMidiMessage> MessageReceived;


  public MidiPipe(InputMidiMidiDevice inputMidiMidiDevice, OutputMidiDevice outputMidiDevice)
  {
    InputMidiMidiDevice = inputMidiMidiDevice;
    OutputMidiDevice = outputMidiDevice;
  }

  public MidiPipe(string inputDeviceName, string outputDeviceName)
    : this(new InputMidiMidiDevice(inputDeviceName), new OutputMidiDevice(outputDeviceName))
  {
  }

  public void Open()
  {
    if (IsOpen) return;
    try
    {
      InputMidiMidiDevice.MessageReceived += InputDevice_MessageReceived;
      InputMidiMidiDevice.Open();
      OutputMidiDevice.Open();
      IsOpen = true;
    }
    catch
    {
      Close();
      throw;
    }
  }

  public void Close()
  {
    InputMidiMidiDevice.MessageReceived -= InputDevice_MessageReceived;
    if (InputMidiMidiDevice.IsOpen) InputMidiMidiDevice.TryClose();
    if (OutputMidiDevice.IsOpen) OutputMidiDevice.TryClose();
    IsOpen = false;
  }

  public void Send(IMidiMessage message)
  {
    OutputMidiDevice.Send(message);
  }


  private void InputDevice_MessageReceived(object sender, IMidiMessage e)
  {
    OutputMidiDevice.Send(e);
    MessageReceived?.Invoke(this, e);
  }


  public override string ToString()
  {
    return $"{InputMidiMidiDevice.DeviceInfo.Name} => {OutputMidiDevice.DeviceInfo.Name}";
  }

}