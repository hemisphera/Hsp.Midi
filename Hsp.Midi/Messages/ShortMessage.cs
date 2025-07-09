namespace Hsp.Midi.Messages;

/// <summary>
/// Represents the basic class for all MIDI short messages.
/// </summary>
/// <remarks>
/// MIDI short messages represent all MIDI messages except meta messages
/// and system exclusive messages. This includes channel messages, system
/// realtime messages, and system common messages.
/// </remarks>
public abstract class ShortMessage : IPackedMessage
{
  private const int StatusMask = ~255;
  protected const int DataMask = ~StatusMask;
  private const int Data1Mask = ~65280;
  private const int Data2Mask = ~Data1Mask + DataMask;


  internal static int GetStatus(int message)
  {
    return message & DataMask;
  }

  internal static int GetData2(int message)
  {
    return (message & ~Data2Mask) >> 16;
  }

  internal static int GetData1(int message)
  {
    return (message & ~Data1Mask) >> 8;
  }

  /// <summary>
  /// Gets the short message as a packed integer.
  /// </summary>
  /// <remarks>
  /// The message is packed into an integer value with the low-order byte
  /// of the low-word representing the status value. The high-order byte
  /// of the low-word represents the first data value, and the low-order
  /// byte of the high-word represents the second data value.
  /// </remarks>
  public int Message { get; set; }

  /// <summary>
  /// Gets the messages's status value.
  /// </summary>
  public int Status
  {
    get => GetStatus(Message);
    set => Message = Message & StatusMask | value;
  }

  public int Data1
  {
    get => GetData1(Message);
    set => Message = Message & Data1Mask | value << 8;
  }

  public int Data2
  {
    get => GetData2(Message);
    set => Message = Message & Data2Mask | value << 8 * 2;
  }


  public virtual byte[] GetBytes()
  {
    // unchecked?
    return new[] { (byte)Status, (byte)Data1, (byte)Data2 };
  }
}