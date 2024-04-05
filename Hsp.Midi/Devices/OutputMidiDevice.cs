using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public sealed class OutputMidiDevice : MidiDevice, IOutputMidiDevice
{
  private const int MomOpen = 0x3C7;
  private const int MomClose = 0x3C8;
  private const int MomDone = 0x3C9;


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


  public string Name => DeviceInfo.Name;

  private delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

  private Queue<Action> DelegateQueue { get; } = new Queue<Action>();

  private readonly object _lockObject = new object();

  private IntPtr _handle;

  private int _bufferCount;

  private readonly MidiOutProc _midiOutProc;


  internal OutputMidiDevice(MidiDeviceInfo device) : base(device)
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

  public void Send(SysCommonMessage message)
  {
    Send(message.Message);
  }

  public void Send(SysRealtimeMessage message)
  {
    Send(message.Message);
  }


  internal override void Open()
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

  internal override void Close()
  {
    if (!IsOpen) return;
    RunMidiProc(() => midiOutClose(_handle));
    _handle = default;
    IsOpen = false;
  }


  // Handles Windows messages.
  private void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
  {
    if (msg == MomOpen)
    {
    }
    else if (msg == MomClose)
    {
    }
    else if (msg == MomDone)
      DelegateQueue.Enqueue(() => ReleaseBuffer(param1));
  }

  private void ReleaseBuffer(object state)
  {
    lock (_lockObject)
    {
      var headerPtr = (IntPtr)state;

      try
      {
        RunMidiProc(() => midiOutUnprepareHeader(_handle, headerPtr, Constants.SizeOfMidiHeader));
      }
      catch (MidiDeviceException ex)
      {
        RaiseErrorEvent(ex);
      }

      MidiHeader.Deallocate(headerPtr);
      _bufferCount--;

      Monitor.Pulse(_lockObject);
    }
  }
}