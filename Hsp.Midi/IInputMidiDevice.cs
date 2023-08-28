using System;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IInputMidiDevice : IDevice
{

  event EventHandler<IMidiMessage> MessageReceived;

}