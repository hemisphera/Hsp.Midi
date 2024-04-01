using System;
using System.Collections.Generic;
using System.Linq;

namespace Hsp.Midi;

public abstract class MidiDevicePool<T> where T : MidiDevice
{
  private readonly List<T> _devices = new();


  public abstract MidiDeviceInfo[] Enumerate();

  public abstract MidiDeviceInfo Get(int deviceId);

  public MidiDeviceInfo Get(string deviceName)
  {
    var dev = Enumerate().FirstOrDefault(a => a.Name == deviceName);
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

      device.OpenCount += 1;
      return device;
    }
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


  public void Close(T device)
  {
    lock (_devices)
    {
      device.OpenCount -= 1;
      if (device.OpenCount != 0) return;
      device.Close();
      _devices.Remove(device);
    }
  }

  public void CloseAll(T device)
  {
    while (device.OpenCount > 0)
      Close(device);
  }
}