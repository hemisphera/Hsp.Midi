using System;

namespace Hsp.Midi;

public class MidiDeviceInfo : IEquatable<MidiDeviceInfo>
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

  public bool Equals(MidiDeviceInfo other)
  {
    if (ReferenceEquals(null, other)) return false;
    if (ReferenceEquals(this, other)) return true;
    return Type == other.Type && Id == other.Id;
  }

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(null, obj)) return false;
    if (ReferenceEquals(this, obj)) return true;
    return obj.GetType() == GetType() && Equals((MidiDeviceInfo)obj);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine((int)Type, Id);
  }
}