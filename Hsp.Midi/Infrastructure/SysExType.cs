namespace Hsp.Midi.Infrastructure;

/// <summary>
/// Defines constants representing various system exclusive message types.
/// </summary>
public enum SysExType
{

  /// <summary>
  /// Represents the start of system exclusive message type.
  /// </summary>
  Start = 0xF0,

  /// <summary>
  /// Represents the continuation of a system exclusive message.
  /// </summary>
  Continuation = 0xF7

}