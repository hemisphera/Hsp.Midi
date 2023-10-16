using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hsp.Midi;

public abstract class MidiDevice : IMidiDevice, IDisposable
{

  public static MidiDeviceInfo[] EnumerateAll()
  {
    return InputMidiDevice.Enumerate().Concat(OutputMidiDevice.Enumerate()).ToArray();
  }


  protected const int CallbackFunction = 0x30000;

  protected const int CallbackEvent = 0x50000;

  protected static readonly int SizeOfMidiHeader = Marshal.SizeOf(typeof(MidiHeader));


  public int DeviceId => DeviceInfo.Id;

  public MidiDeviceInfo DeviceInfo { get; }

  public bool IsOpen { get; protected set; }


  public event EventHandler<Exception> Error;


  protected MidiDevice(MidiDeviceInfo info)
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
  public abstract void Open();

  /// <summary>
  /// Closes the device.
  /// </summary>
  public abstract void Close();

  public bool TryClose()
  {
    try
    {
      Close();
      return true;
    }
    catch
    {
      return false;
    }
  }

  protected bool TryRunMidiProc(Func<int> func)
  {
    try
    {
      RunMidiProc(func);
      return true;
    }
    catch (MidiDeviceException)
    {
      return false;
    }
  }

  protected void RunMidiProc(Func<int> func)
  {
    RunMidiProc(this, func);
  }

  protected static void RunMidiProc(MidiDeviceType deviceType, Func<int> func)
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

  /// <summary>
  /// Disposes of the device.
  /// </summary>
  public abstract void Dispose();

}