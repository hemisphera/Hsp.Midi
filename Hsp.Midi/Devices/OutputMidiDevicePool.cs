using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hsp.Midi;

public class OutputMidiDevicePool : MidiDevicePool<IOutputMidiDevice>
{
  public static OutputMidiDevicePool Instance { get; } = new();


  [DllImport("winmm.dll")]
  private static extern int midiOutGetDevCaps(IntPtr deviceId, ref MidiOutCapabilities caps, int sizeOfMidiOutCaps);

  [DllImport("winmm.dll")]
  private static extern int midiOutGetNumDevs();

  public override MidiDeviceInfo[] Enumerate()
  {
    var count = midiOutGetNumDevs();
    return Enumerable.Range(0, count).Select(Get).ToArray();
  }

  public override MidiDeviceInfo Get(int deviceId)
  {
    var caps = new MidiOutCapabilities();
    var devId = (IntPtr)deviceId;
    MidiDevice.RunMidiProc(MidiDeviceType.Input, () => midiOutGetDevCaps(devId, ref caps, Constants.SizeOfMidiHeader));
    return new MidiDeviceInfo(deviceId, caps);
  }

  protected override IOutputMidiDevice CreateDevice(MidiDeviceInfo info)
  {
    return info.IsVirtual
      ? new VirtualMidiOutputDevice(info)
      : new OutputMidiDevice(info);
  }

  private OutputMidiDevicePool()
  {
  }
}