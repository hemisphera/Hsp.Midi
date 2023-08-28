using System.Runtime.InteropServices;

namespace Hsp.Midi
{

  /// <summary>
  /// Represents MIDI output device capabilities.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct MidiOutCapabilities
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
    /// Flags describing the type of the Midi output device. 
    /// </summary>
    public short technology;

    /// <summary>
    /// Number of voices supported by an internal synthesizer device. If 
    /// the device is a port, this member is not meaningful and is set 
    /// to 0. 
    /// </summary>
    public short voices;

    /// <summary>
    /// Maximum number of simultaneous notes that can be played by an 
    /// internal synthesizer device. If the device is a port, this member 
    /// is not meaningful and is set to 0. 
    /// </summary>
    public short notes;

    /// <summary>
    /// Channels that an internal synthesizer device responds to, where the 
    /// least significant bit refers to channel 0 and the most significant 
    /// bit to channel 15. Port devices that transmit on all channels set 
    /// this member to 0xFFFF. 
    /// </summary>
    public short channelMask;

    /// <summary>
    /// Optional functionality supported by the device. 
    /// </summary>
    public int support;

  }

}