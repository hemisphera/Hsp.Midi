namespace Hsp.Midi.Messages;

/// <summary>
/// Represents MIDI system realtime messages.
/// </summary>
/// <remarks>
/// System realtime messages are MIDI messages that are primarily concerned 
/// with controlling and synchronizing MIDI devices. 
/// </remarks>
public sealed class SysRealtimeMessage : ShortMessage
{

  /// <summary>
  /// Gets the SysRealtimeType.
  /// </summary>
  public SysRealtimeType SysRealtimeType
  {
    get => (SysRealtimeType)Status;
    set => Status = (int)value;
  }


  // Make construction private so that a system realtime message cannot 
  // be constructed directly.
  public SysRealtimeMessage(SysRealtimeType type)
  {
    SysRealtimeType = type;
  }

}