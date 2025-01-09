namespace Hsp.Midi;

/// <summary>
/// Defines constants representing the various system common message types.
/// </summary>
public enum SysCommonType
{
  /// <summary>
  /// Represents the MTC system common message type.
  /// </summary>
  MidiTimeCode = 0xF1,

  /// <summary>
  /// Represents the song position pointer type.
  /// </summary>
  SongPositionPointer = 0xF2,

  /// <summary>
  /// Represents the song select type.
  /// </summary>
  SongSelect = 0xF3,

  /// <summary>
  /// Represents the tune request type.
  /// </summary>
  TuneRequest = 0xF6
}