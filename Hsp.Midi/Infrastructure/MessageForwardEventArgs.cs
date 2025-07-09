using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public class MessageForwardEventArgs : EventArgs
{
  public IMidiMessage InputMessage { get; }

  public IMidiMessage[] OutputMessages { get; set; }

  public MessageForwardEventArgs(IMidiMessage inputMessage)
  {
    InputMessage = inputMessage;
    OutputMessages =
    [
      inputMessage
    ];
  }
}