using System;

namespace Hsp.Midi;

public abstract class MidiDevice
{
  internal int OpenCount { get; set; }

  public int DeviceId => DeviceInfo.Id;
  
  public string Name => DeviceInfo.Name;

  public MidiDeviceInfo DeviceInfo { get; }

  public bool IsOpen { get; protected set; }


  public event EventHandler<Exception> Error;


  internal MidiDevice(MidiDeviceInfo info)
  {
    DeviceInfo = info;
  }

  protected virtual void RaiseErrorEvent(Exception e)
  {
    Error?.Invoke(this, e);
  }

  protected void AssertDeviceOpen()
  {
    if (IsOpen) return;
    throw new NotSupportedException("The device is not open.");
  }

  /// <summary>
  /// Opens the device.
  /// </summary>
  internal abstract void Open();

  /// <summary>
  /// Closes the device.
  /// </summary>
  internal abstract void Close();

  protected void RunMidiProc(Func<int> func)
  {
    RunMidiProc(this, func);
  }

  internal static void RunMidiProc(MidiDeviceType deviceType, Func<int> func)
  {
    var result = func();
    if (result != MidiDeviceException.MmSysErrNoerror)
      throw new MidiDeviceException(deviceType, result);
  }

  protected static void RunMidiProc(MidiDevice device, Func<int> func)
  {
    var result = func();
    if (result != MidiDeviceException.MmSysErrNoerror)
      throw new MidiDeviceException(device, result);
  }

  /// <summary>
  /// Resets the device.
  /// </summary>
  public abstract void Reset();
}