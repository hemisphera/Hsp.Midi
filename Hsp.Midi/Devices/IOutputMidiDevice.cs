using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IOutputMidiDevice
{
  void Send(IMidiMessage message);
}