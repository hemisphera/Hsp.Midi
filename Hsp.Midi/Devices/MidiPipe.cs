using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class MidiPipe : IInputMidiDevice, IOutputMidiDevice
{

  public InputMidiDevice InputMidiDevice { get; }

  public OutputMidiDevice OutputMidiDevice { get; }

  public bool IsOpen { get; private set; }


  public event EventHandler<IMidiMessage> MessageReceived;


  public MidiPipe(InputMidiDevice inputMidiDevice, OutputMidiDevice outputMidiDevice)
  {
    InputMidiDevice = inputMidiDevice;
    OutputMidiDevice = outputMidiDevice;
  }

  public MidiPipe(string inputDeviceName, string outputDeviceName)
    : this(new InputMidiDevice(inputDeviceName), new OutputMidiDevice(outputDeviceName))
  {
  }

  public void Open()
  {
    if (IsOpen) return;
    try
    {
      InputMidiDevice.MessageReceived += InputDevice_MessageReceived;
      InputMidiDevice.Open();
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
    InputMidiDevice.MessageReceived -= InputDevice_MessageReceived;
    if (InputMidiDevice.IsOpen) InputMidiDevice.TryClose();
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
    return $"{InputMidiDevice.DeviceInfo.Name} => {OutputMidiDevice.DeviceInfo.Name}";
  }

}