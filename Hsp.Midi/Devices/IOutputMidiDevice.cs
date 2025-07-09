using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IOutputMidiDevice : IMidiDevice
{
  void Send(IMidiMessage message);
  
  
}