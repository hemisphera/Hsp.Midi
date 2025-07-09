namespace Hsp.Midi;

public interface IMidiDevice
{
  int DeviceId { get; }
  string Name { get; }

  void Open();
  void Close();
  void Reset();
}