using System.Runtime.InteropServices;

namespace Hsp.Midi
{

  /// <summary>
  /// Represents MIDI input device capabilities.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct MidiInCapabilities
  {

    /// <summary>
    /// Manufacturer identifier of the device driver for the Midi output 
    /// device. 
    /// </summary>
    public short mid;

    /// <summary>
    /// Product identifier of the Midi output device. 
    /// </summary>
    public short pid;

    /// <summary>
    /// Version number of the device driver for the Midi output device. The 
    /// high-order byte is the major version number, and the low-order byte 
    /// is the minor version number. 
    /// </summary>
    public int driverVersion;

    /// <summary>
    /// Product name.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string name;

    /// <summary>
    /// Optional functionality supported by the device. 
    /// </summary>
    public int support;

  }

}