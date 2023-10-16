using System;
using System.Collections;
using System.Runtime.InteropServices;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

/// <summary>
/// Builds a pointer to a MidiHeader structure.
/// </summary>
internal class MidiHeaderBuilder
{

  // The length of the system exclusive buffer.
  private int bufferLength;

  // The system exclusive data.
  private byte[] data;

  // Indicates whether the pointer to the MidiHeader has been built.
  private bool built = false;

  // The built pointer to the MidiHeader.
  private IntPtr result;

  /// <summary>
  /// Initializes a new instance of the MidiHeaderBuilder.
  /// </summary>
  public MidiHeaderBuilder()
  {
    BufferLength = 1;
  }

  #region Methods

  /// <summary>
  /// Builds the pointer to the MidiHeader structure.
  /// </summary>
  public void Build()
  {
    // Initialize the MidiHeader.
    var header = new MidiHeader
    {
      bufferLength = BufferLength,
      bytesRecorded = BufferLength,
      data = Marshal.AllocHGlobal(BufferLength),
      flags = 0
    };

    // Write data to the MidiHeader.
    for (var i = 0; i < BufferLength; i++)
      Marshal.WriteByte(header.data, i, data[i]);

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

    built = true;
  }

  /// <summary>
  /// Initializes the MidiHeaderBuilder with the specified SysExMessage.
  /// </summary>
  /// <param name="message">
  /// The SysExMessage to use for initializing the MidiHeaderBuilder.
  /// </param>
  public void InitializeBuffer(SysExMessage message)
  {
    // If this is a start system exclusive message.
    if (message.SysExType == SysExType.Start)
    {
      BufferLength = message.Length;

      // Copy entire message.
      for (int i = 0; i < BufferLength; i++)
      {
        data[i] = message[i];
      }
    }
    // Else this is a continuation message.
    else
    {
      BufferLength = message.Length - 1;

      // Copy all but the first byte of message.
      for (int i = 0; i < BufferLength; i++)
      {
        data[i] = message[i + 1];
      }
    }
  }

  public void InitializeBuffer(ICollection events)
  {
    if (events == null)
      throw new ArgumentNullException("events");

    if (events.Count % 4 != 0)
      throw new ArgumentException("Stream events not word aligned.");

    if (events.Count == 0)
      return;

    BufferLength = events.Count;

    events.CopyTo(data, 0);
  }

  /// <summary>
  /// Releases the resources associated with the built MidiHeader pointer.
  /// </summary>
  public void Destroy()
  {
    #region Require

    if (!built)
    {
      throw new InvalidOperationException("Cannot destroy MidiHeader");
    }

    #endregion

    Destroy(result);
  }

  /// <summary>
  /// Releases the resources associated with the specified MidiHeader pointer.
  /// </summary>
  /// <param name="headerPtr">
  /// The MidiHeader pointer.
  /// </param>
  public void Destroy(IntPtr headerPtr)
  {
    MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

    Marshal.FreeHGlobal(header.data);
    Marshal.FreeHGlobal(headerPtr);
  }

  #endregion

  #region Properties

  /// <summary>
  /// The length of the system exclusive buffer.
  /// </summary>
  public int BufferLength
  {
    get
    {
      return bufferLength;
    }
    set
    {
      #region Require

      if (value <= 0)
      {
        throw new ArgumentOutOfRangeException("BufferLength", value,
          "MIDI header buffer length out of range.");
      }

      #endregion

      bufferLength = value;
      data = new byte[value];
    }
  }

  /// <summary>
  /// Gets the pointer to the MidiHeader.
  /// </summary>
  public IntPtr Result
  {
    get
    {
      return result;
    }
  }

  #endregion
}