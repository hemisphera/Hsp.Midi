using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IOutputMidiDevice
{
  string Name { get; }
  void Send(IMidiMessage message);
}