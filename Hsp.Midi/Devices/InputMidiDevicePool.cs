using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hsp.Midi;

public class InputMidiDevicePool : MidiDevicePool<IInputMidiDevice>
{
  public static InputMidiDevicePool Instance { get; } = new();

  [DllImport("winmm.dll")]
  private static extern int midiInGetDevCaps(IntPtr deviceId, ref MidiInCapabilities caps, int sizeOfMidiInCaps);

  [DllImport("winmm.dll")]
  private static extern int midiInGetNumDevs();


  public override MidiDeviceInfo[] Enumerate()
  {
    var count = midiInGetNumDevs();
    return Enumerable.Range(0, count).Select(Get).ToArray();
  }

  public override MidiDeviceInfo Get(int deviceId)
  {
    var caps = new MidiInCapabilities();
    var devId = (IntPtr)deviceId;
    MidiDevice.RunMidiProc(MidiDeviceType.Input, () => midiInGetDevCaps(devId, ref caps, Constants.SizeOfMidiHeader));
    return new MidiDeviceInfo(deviceId, caps);
  }

  protected override IInputMidiDevice CreateDevice(MidiDeviceInfo info)
  {
    return info.IsVirtual
      ? new VirtualMidiInputDevice(info)
      : new InputMidiDevice(info);
  }

  private InputMidiDevicePool()
  {
  }
}