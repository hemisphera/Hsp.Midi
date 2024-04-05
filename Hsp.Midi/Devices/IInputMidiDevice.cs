using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IInputMidiDevice
{
  string Name { get; }

  event EventHandler<IMidiMessage> MessageReceived;
}