using Hsp.Midi.Messages;

namespace Hsp.Midi;

public interface IOutputMidiDevice : IDevice
{

  void Send(IMidiMessage message);

}