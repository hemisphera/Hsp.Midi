using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hsp.Midi.Infrastructure;

namespace Hsp.Midi.Messages;

/// <summary>
/// Represents MIDI system exclusive messages.
/// </summary>
public sealed class SysExMessage : IMidiMessage, IEnumerable
{

  /// <summary>
  /// Maximum value for system exclusive channels.
  /// </summary>
  public const int SysExChannelMaxValue = 127;

  // The system exclusive data.
  private byte[] Data { get; }

  public int Timestamp { get; set; }

  public SysExMessage(string bytes)
    : this(bytes.Split(' ').Select(b => Convert.ToByte(b, 16)))
  {
  }

  public SysExMessage(IEnumerable<byte> data)
  {
    Data = data.ToArray();
    if (Data.Length < 1)
      throw new ArgumentException("System exclusive data is too short.", nameof(data));

    if (Data[0] != (byte)SysExType.Start && Data[0] != (byte)SysExType.Continuation)
      throw new ArgumentException("Unknown status value.", nameof(data));
  }

  public SysExMessage(params byte[] data) : this((IEnumerable<byte>)data)
  {
  }


  public byte[] GetBytes()
  {
    return Data;
  }


  /// <summary>
  /// Gets the element at the specified index.
  /// </summary>
  /// <exception cref="ArgumentOutOfRangeException">
  /// If index is less than zero or greater than or equal to the length 
  /// of the message.
  /// </exception>
  public byte this[int index]
  {
    get
    {
      if (index < 0 || index >= Length)
        throw new ArgumentOutOfRangeException(nameof(index), index, "Index into system exclusive message out of range.");
      return Data[index];
    }
  }

  /// <summary>
  /// Gets the length of the system exclusive data.
  /// </summary>
  public int Length => Data.Length;

  /// <summary>
  /// Gets the system exclusive type.
  /// </summary>
  public SysExType SysExType => (SysExType)Data[0];

  /// <summary>
  /// Gets the status value.
  /// </summary>
  public int Status => Data[0];

  public IEnumerator GetEnumerator()
  {
    return Data.GetEnumerator();
  }

}