using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class MidiPipe : IInputMidiDevice, IOutputMidiDevice
{
  public InputMidiDevice InputMidiDevice { get; }

  public OutputMidiDevice OutputMidiDevice { get; }

  public bool IsOpen { get; private set; }


  public string Name => $"{InputMidiDevice.Name} => {OutputMidiDevice.Name}";

  public event EventHandler<IMidiMessage> MessageReceived;


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
    OutputMidiDevice.Send(e);
    MessageReceived?.Invoke(this, e);
  }


  public override string ToString()
  {
    return $"{InputMidiDevice.DeviceInfo.Name} => {OutputMidiDevice.DeviceInfo.Name}";
  }
}