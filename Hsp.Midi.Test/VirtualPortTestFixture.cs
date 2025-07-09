namespace Hsp.Midi.Test;

public sealed class VirtualPortTestFixture : IDisposable
{
  public VirtualMidiPort Port { get; }


  public VirtualPortTestFixture()
  {
    var portName = Guid.NewGuid().ToString()[..8];
    Port = VirtualMidiPort.Create(portName);
  }

  public void Dispose()
  {
    Port.Dispose();
  }
}