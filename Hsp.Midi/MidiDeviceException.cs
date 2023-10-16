using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Hsp.Midi;

public class MidiDeviceException : ApplicationException
{
  [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
  private static extern int midiInGetErrorText(int errCode, StringBuilder errMsg, int sizeOfErrMsg);

  [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
  private static extern int midiOutGetErrorText(int errCode, StringBuilder message, int sizeOfMessage);


  public const int MmSysErrNoerror = 0; /* no error */

  public int ErrorCode { get; }

  public MidiDevice Device { get; }


  public MidiDeviceException(MidiDevice device, int errCode)
    : this(device.DeviceInfo.Type, errCode)
  {
    Device = device;
  }

  public MidiDeviceException(MidiDeviceType type, int errCode)
    : base(GetErrMessage(type, errCode))
  {
    ErrorCode = errCode;
  }


  private static string GetErrMessage(MidiDeviceType type, int errorCode)
  {
    var sb = new StringBuilder(128);
    var result = type == MidiDeviceType.Input
      ? midiInGetErrorText(errorCode, sb, sb.Capacity)
      : midiOutGetErrorText(errorCode, sb, sb.Capacity);
    return result == MmSysErrNoerror
      ? sb.ToString()
      : "Unknown error.";
  }
}