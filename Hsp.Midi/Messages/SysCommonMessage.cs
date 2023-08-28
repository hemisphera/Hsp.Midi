using System.ComponentModel;
using Hsp.Midi.Infrastructure;

namespace Hsp.Midi.Messages;

/// <summary>
/// Represents MIDI system common messages.
/// </summary>
[ImmutableObject(true)]
public sealed class SysCommonMessage : ShortMessage
{

  /// <summary>
  /// Gets the SysCommonType.
  /// </summary>
  public SysCommonType SysCommonType
  {
    get => (SysCommonType)Status;
    set => Status = (int)value;
  }


  public SysCommonMessage(SysCommonType type, int data1 = 0, int data2 = 0)
  {
    SysCommonType = type;
    Data1 = data1;
    Data2 = data2;
  }

 
  public SysCommonMessage(int message)
  {
    Message = message;
  }

}