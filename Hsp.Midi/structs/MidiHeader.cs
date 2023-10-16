using System;
using System.Runtime.InteropServices;

namespace Hsp.Midi
{
  /// <summary>
  /// Represents the Windows Multimedia MIDIHDR structure.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct MidiHeader
  {

    /// <summary>
    /// Pointer to MIDI data.
    /// </summary>
    public IntPtr data;

    /// <summary>
    /// Size of the buffer.
    /// </summary>
    public int bufferLength;

    /// <summary>
    /// Actual amount of data in the buffer. This value should be less than 
    /// or equal to the value given in the dwBufferLength member.
    /// </summary>
    public int bytesRecorded;

    /// <summary>
    /// Custom user data.
    /// </summary>
    public int user;

    /// <summary>
    /// Flags giving information about the buffer.
    /// </summary>
    public int flags;

    /// <summary>
    /// Reserved; do not use.
    /// </summary>
    public IntPtr next;

    /// <summary>
    /// Reserved; do not use.
    /// </summary>
    public int reserved;

    /// <summary>
    /// Offset into the buffer when a callback is performed. (This 
    /// callback is generated because the MEVT_F_CALLBACK flag is 
    /// set in the dwEvent member of the MidiEventArgs structure.) 
    /// This offset enables an application to determine which 
    /// event caused the callback. 
    /// </summary>
    public int offset;

    /// <summary>
    /// Reserved; do not use.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public int[] reservedArray;

  }

}