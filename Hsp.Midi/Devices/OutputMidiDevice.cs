using System;
using System.Runtime.InteropServices;
using System.Threading;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public sealed class OutputMidiDevice : MidiDevice, IOutputMidiDevice
{
  [DllImport("winmm.dll")]
  private static extern int midiOutReset(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiOutShortMsg(IntPtr handle, int message);

  [DllImport("winmm.dll")]
  private static extern int midiOutPrepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiOutUnprepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiOutLongMsg(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiOutOpen(out IntPtr handle, int deviceId, MidiOutProc proc, IntPtr instance,
    int flags);

  [DllImport("winmm.dll")]
  private static extern int midiOutClose(IntPtr handle);


  private delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

  private readonly object _lockObject = new();
  private readonly MidiOutProc _midiOutProc;

  private IntPtr _handle;
  private int _bufferCount;


  public OutputMidiDevice(MidiDeviceInfo device) : base(device)
  {
    device.AssertType(MidiDeviceType.Output);
    _midiOutProc = HandleMessage;
  }


  public void Send(IMidiMessage msg)
  {
    switch (msg)
    {
      case SysExMessage sem:
        Send(sem);
        return;
      case IPackedMessage pm:
        Send(pm.Message);
        return;
      default:
        throw new NotSupportedException();
    }
  }

  private void Send(int message)
  {
    lock (_lockObject)
      RunMidiProc(() => midiOutShortMsg(_handle, message));
  }

  private void Send(SysExMessage message)
  {
    lock (_lockObject)
    {
      var ptr = MidiHeader.Allocate(message);

      try
      {
        RunMidiProc(() => midiOutPrepareHeader(_handle, ptr, Constants.SizeOfMidiHeader));
        _bufferCount++;

        try
        {
          RunMidiProc(() => midiOutLongMsg(_handle, ptr, Constants.SizeOfMidiHeader));
        }
        catch (MidiDeviceException)
        {
          RunMidiProc(() => midiOutUnprepareHeader(_handle, ptr, Constants.SizeOfMidiHeader));
          _bufferCount--;
          throw;
        }
      }
      catch
      {
        MidiHeader.Deallocate(ptr);
        throw;
      }
    }
  }


  public override void Open()
  {
    if (IsOpen) return;
    RunMidiProc(() => midiOutOpen(out _handle, DeviceId, _midiOutProc, IntPtr.Zero, Constants.CallbackFunction));
    IsOpen = true;
  }

  public override void Reset()
  {
    AssertDeviceOpen();
    lock (_lockObject)
    {
      RunMidiProc(() => midiOutReset(_handle));
      while (_bufferCount > 0)
        Monitor.Wait(_lockObject);
    }
  }

  public override void Close()
  {
    if (!IsOpen) return;
    RunMidiProc(() => midiOutClose(_handle));
    _handle = 0;
    IsOpen = false;
  }


  // Handles Windows messages.
  private void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
  {
    // do nothing
  }
}