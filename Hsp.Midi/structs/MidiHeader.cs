using System;
using System.Linq;
using System.Runtime.InteropServices;
using Hsp.Midi.Messages;

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


    public static MidiHeader FromPointer(IntPtr headerPtr)
    {
      if (Marshal.PtrToStructure(headerPtr, typeof(MidiHeader)) is not MidiHeader header)
        throw new InvalidOperationException("Invalid header pointer.");
      return header;
    }

    public static IntPtr Allocate(byte[] data)
    {
      var header = new MidiHeader
      {
        bufferLength = data.Length,
        bytesRecorded = data.Length,
        data = Marshal.AllocHGlobal(data.Length),
        flags = 0
      };

      for (var i = 0; i < data.Length; i++)
        Marshal.WriteByte(header.data, i, data[i]);

      IntPtr result;
      try
      {
        result = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MidiHeader)));
      }
      catch (Exception)
      {
        Marshal.FreeHGlobal(header.data);

        throw;
      }

      try
      {
        Marshal.StructureToPtr(header, result, false);
      }
      catch (Exception)
      {
        Marshal.FreeHGlobal(header.data);
        Marshal.FreeHGlobal(result);

        throw;
      }

      return result;
    }

    public static IntPtr Allocate(SysExMessage message)
    {
      var messageData = message.SysExType == SysExType.Start
        ? message.GetBytes()
        : message.GetBytes().Skip(1).ToArray();
      return Allocate(messageData);
    }

    public static void Deallocate(IntPtr headerPtr)
    {
      var header = FromPointer(headerPtr);
      Marshal.FreeHGlobal(header.data);
      Marshal.FreeHGlobal(headerPtr);
    }
  }
}