using System;
using System.Linq;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class VirtualMidiOutputDevice : IOutputMidiDevice
{
  private readonly VirtualMidiPort _port;
  public int DeviceId { get; }
  public string Name => _port.Name;


  public VirtualMidiOutputDevice(MidiDeviceInfo device)
  {
    DeviceId = device.Id;
    _port = VirtualMidiPort.Ports.First(p => p.Name.Equals(device.Name, StringComparison.OrdinalIgnoreCase));
  }


  public void Open()
  {
    // do nothing
  }

  public void Close()
  {
    // do nothing
  }

  public void Reset()
  {
    // do nothing
  }

  public void Send(IMidiMessage message)
  {
    switch (message)
    {
      case SysExMessage:
      case IPackedMessage:
        _port.WriteCommand(message.GetBytes());
        return;
      default:
        throw new NotSupportedException();
    }
  }
}