using System;
using System.Linq;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class VirtualMidiInputDevice : IInputMidiDevice
{
  private readonly VirtualMidiPort _port;
  public int DeviceId { get; }
  public string Name => _port.Name;


  public VirtualMidiInputDevice(MidiDeviceInfo device)
  {
    DeviceId = device.Id;
    _port = VirtualMidiPort.Ports.First(p => p.Name.Equals(device.Name, StringComparison.OrdinalIgnoreCase));
  }


  public void Open()
  {
    _port.CommandReceived += PortOnCommandReceived;
  }

  private void PortOnCommandReceived(object? sender, byte[] e)
  {
    var msg = ParseMessage(e);
    if (msg != null)
      MessageReceived?.Invoke(this, msg);
  }

  private static IMidiMessage? ParseMessage(byte[] e)
  {
    if (e[0] == (byte)SysExType.Start)
    {
      return new SysExMessage(e);
    }

    if (e.Length is > 0 and <= 4)
    {
      var ba = new[]
      {
        e[0],
        (byte)(e.Length >= 2 ? e[1] : 0),
        (byte)(e.Length >= 3 ? e[2] : 0),
        (byte)(e.Length >= 4 ? e[3] : 0)
      };
      var pm = (ba[3] << 24) | (ba[2] << 16) | (ba[1] << 8) | ba[0];
      return MessageBuilder.Build(pm);
    }

    return null;
  }

  public void Close()
  {
    _port.CommandReceived -= PortOnCommandReceived;
  }

  public void Reset()
  {
  }

  public event EventHandler<IMidiMessage>? MessageReceived;
}