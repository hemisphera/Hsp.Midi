using System.Collections.Generic;
using System.Text;

namespace Hsp.Midi.Messages;

public class MessageBuilder
{
  public static IMidiMessage Build(int data1, int data2)
  {
    var status = ShortMessage.GetStatus(data1);

    if (IsChannelMessage(status))
      return new ChannelMessage(data1);
    if (BuildSysCommonMessage(status, ShortMessage.GetData(data1), data2, out var scm))
      return scm;
    return new SysRealtimeMessage((SysRealtimeType)status);
  }

  private static bool BuildSysCommonMessage(int status, int data1, int data2, out IMidiMessage msg)
  {
    msg = null;
    switch ((SysCommonType)status)
    {
      case SysCommonType.SongPositionPointer:
        msg = SongPositionPointerMessage.Parse(data1, data2);
        return true;
      case SysCommonType.SongSelect:
      case SysCommonType.TuneRequest:
      case SysCommonType.MidiTimeCode:
        msg = new SysCommonMessage((SysCommonType)status, data1);
        return true;
      default:
        return false;
    }
  }

  internal static IMidiMessage Build(MidiInParams data)
  {
    return Build(data.Param1.ToInt32(), data.Param2.ToInt32());
  }


  private static bool IsChannelMessage(int status)
  {
    return
      status >= (int)ChannelCommand.NoteOff &&
      status <= (int)ChannelCommand.PitchWheel + Constants.MidiChannelMaxValue;
  }
}