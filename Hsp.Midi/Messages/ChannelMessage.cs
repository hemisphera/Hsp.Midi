using System.ComponentModel;
using Hsp.Midi.Infrastructure;

namespace Hsp.Midi.Messages;

/// <summary>
/// Represents MIDI channel messages.
/// </summary>
[ImmutableObject(true)]
public sealed class ChannelMessage : ShortMessage
{

  private const int CommandMask = ~240;

  private const int MidiChannelMask = ~15;


  public ChannelCommand Command
  {
    get => (ChannelCommand)(Message & DataMask & MidiChannelMask);
    set => Message = Message & CommandMask | (int)value;
  }

  public int Channel
  {
    get => Message & DataMask & CommandMask;
    set => Message = Message & MidiChannelMask | value;
  }

  public int MaxDataBytes => Command is ChannelCommand.ChannelPressure or ChannelCommand.ProgramChange ? 1 : 2;


  public ChannelMessage(ChannelCommand command, int channel, int data1, int data2 = 0)
  {
    Command = command;
    Channel = channel;
    Data1 = data1;
    Data2 = data2;
  }

  public ChannelMessage(int message)
  {
    Message = message;
  }


  public override string ToString()
  {
    return $"{Command}{Data1} (Ch{Channel + 1}): {Data2}";
  }

}