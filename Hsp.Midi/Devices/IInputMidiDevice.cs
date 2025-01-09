using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IInputMidiDevice : IMidiDevice
{
  event EventHandler<IMidiMessage> MessageReceived;
}