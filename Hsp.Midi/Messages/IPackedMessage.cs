namespace Hsp.Midi.Messages;

public interface IPackedMessage : IMidiMessage
{

  int Message { get; }

}