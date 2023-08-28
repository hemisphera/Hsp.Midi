using System;
using System.Collections.Generic;
using System.Text;
using Hsp.Midi.Infrastructure;

namespace Hsp.Midi.Messages;

internal class MessageBuilder
{

  public static IMidiMessage Build(MidiInParams data)
  {
    var message = data.Param1.ToInt32();
    var status = ShortMessage.GetStatus(message);

    if (IsChannelMessage(status))
      return new ChannelMessage(message);
    if (IsSysCommonMessage(status))
      return new SysCommonMessage(message);
    return new SysRealtimeMessage((SysRealtimeType)status);
  }


  private static bool IsChannelMessage(int status)
  {
    return
      status >= (int)ChannelCommand.NoteOff &&
      status <= (int)ChannelCommand.PitchWheel + Constants.MidiChannelMaxValue;
  }

  private static bool IsSysCommonMessage(int status)
  {
    return
      status == (int)SysCommonType.MidiTimeCode ||
      status == (int)SysCommonType.SongPositionPointer ||
      status == (int)SysCommonType.SongSelect ||
      status == (int)SysCommonType.TuneRequest;
  }

}