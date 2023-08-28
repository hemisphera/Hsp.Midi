using System;

namespace Hsp.Midi;

public class MidiDeviceInfo
{

  public MidiDeviceType Type { get; }

  public string Name { get; }

  public int Id { get; }

  public int ManufacturerId { get; }

  public int ProductId { get; }

  public Version DriverVersion { get; }


  internal MidiDeviceInfo(int id, MidiInCapabilities inCaps)
  {
    Id = id;
    Type = MidiDeviceType.Input;
    Name = inCaps.name;
    ManufacturerId = inCaps.mid;
    ProductId = inCaps.pid;
    DriverVersion = ParseVersion(inCaps.driverVersion);
  }

  internal MidiDeviceInfo(int id, MidiOutCapabilities outCaps)
  {
    Id = id;
    Type = MidiDeviceType.Output;
    Name = outCaps.name;
    ManufacturerId = outCaps.mid;
    ProductId = outCaps.pid;
    DriverVersion = ParseVersion(outCaps.driverVersion);
  }


  public MidiDevice CreateDevice()
  {
    var device = Type == MidiDeviceType.Input
      ? (MidiDevice)new InputMidiMidiDevice(this)
      : (MidiDevice)new OutputMidiDevice(this);
    return device;
  }


  private static Version ParseVersion(int versionNum)
  {
    return new Version(
      versionNum >> 8 & 0xff,
      versionNum & 0xff
    );
  }

  internal void AssertType(MidiDeviceType requiredType)
  {
    if (Type != requiredType)
      throw new NotSupportedException($"The device must be a {requiredType} device");
  }

  public override string ToString()
  {
    return $"{Type}: {Name}";
  }
}