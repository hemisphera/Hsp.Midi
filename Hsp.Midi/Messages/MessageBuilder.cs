using System.Collections.Generic;
using System.Text;

namespace Hsp.Midi.Messages;

public class MessageBuilder
{
  public static IMidiMessage Build(int data)
  {
    var status = ShortMessage.GetStatus(data);

    if (IsChannelMessage(status))
      return new ChannelMessage(data);

    var scm = BuildSysCommonMessage(status, ShortMessage.GetData1(data), ShortMessage.GetData2(data));
    if (scm != null) return scm;

    return new SysRealtimeMessage((SysRealtimeType)status);
  }

  private static IMidiMessage? BuildSysCommonMessage(int status, int data1, int data2)
  {
    switch ((SysCommonType)status)
    {
      case SysCommonType.SongPositionPointer:
        return SongPositionPointerMessage.Parse(data1, data2);
      case SysCommonType.SongSelect:
      case SysCommonType.TuneRequest:
      case SysCommonType.MidiTimeCode:
        return new SysCommonMessage((SysCommonType)status, data1);
    }

    return null;
  }

  internal static IMidiMessage Build(MidiInParams data)
  {
    return Build(data.Param1.ToInt32());
  }


  private static bool IsChannelMessage(int status)
  {
    return
      status >= (int)ChannelCommand.NoteOff &&
      status <= (int)ChannelCommand.PitchWheel + Constants.MidiChannelMaxValue;
  }
}