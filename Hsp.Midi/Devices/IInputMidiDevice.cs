using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IInputMidiDevice
{
  event EventHandler<IMidiMessage> MessageReceived;
}