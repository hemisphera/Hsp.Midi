using System;
using System.Collections.Generic;
using System.Linq;

namespace Hsp.Midi;

public abstract class MidiDevicePool<T> where T : IMidiDevice
{
  private readonly List<T> _devices = new();

  private readonly Dictionary<int, int> _openCounter = new();

  public abstract MidiDeviceInfo[] Enumerate();

  public abstract MidiDeviceInfo Get(int deviceId);

  public MidiDeviceInfo Get(string deviceName)
  {
    var dev = Enumerate().FirstOrDefault(a => a.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
    if (dev == null)
      throw new ArgumentException($"Device '{deviceName}' not found.", nameof(deviceName));
    return dev;
  }


  protected abstract T CreateDevice(MidiDeviceInfo info);


  public T Open(MidiDeviceInfo info)
  {
    lock (_devices)
    {
      var device = _devices.FirstOrDefault(d => d.DeviceId == info.Id);
      if (device == null)
      {
        device = CreateDevice(info);
        device.Open();
        _devices.Add(device);
      }

      UpdateCounter(device, 1);
      return device;
    }
  }

  private void UpdateCounter(T device, int delta)
  {
    if (_openCounter.TryGetValue(device.DeviceId, out var currCount))
      currCount = 0;
    _openCounter[device.DeviceId] = currCount + delta;
  }

  public T Open(int id)
  {
    var info = Get(id);
    return Open(info);
  }

  public T Open(string deviceName)
  {
    var info = Get(deviceName);
    return Open(info);
  }


  public bool Close(T device, bool force = false)
  {
    lock (_devices)
    {
      UpdateCounter(device, -1);
      if (!force && _openCounter[device.DeviceId] > 0) return false;
      device.Close();
      _devices.Remove(device);
      _openCounter[device.DeviceId] = 0;
      return true;
    }
  }
}