using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Hsp.Midi.Messages;

namespace Hsp.Midi
{
  public sealed class OutputMidiDevice : MidiDevice, IOutputMidiDevice
  {

    private const int MomOpen = 0x3C7;
    private const int MomClose = 0x3C8;
    private const int MomDone = 0x3C9;
    
    
    public static MidiDeviceInfo[] Enumerate()
    {
      var count = midiOutGetNumDevs();
      return Enumerable.Range(0, count).Select(Get).ToArray();
    }

    public static MidiDeviceInfo Get(int deviceId)
    {
      var caps = new MidiOutCapabilities();
      var devId = (IntPtr)deviceId;
      RunMidiProc(MidiDeviceType.Output, () => midiOutGetDevCaps(devId, ref caps, Marshal.SizeOf(caps)));
      return new MidiDeviceInfo(deviceId, caps);
    }

    public static MidiDeviceInfo Get(string deviceName)
    {
      var dev = Enumerate().FirstOrDefault(a => a.Name == deviceName);
      if (dev == null)
        throw new ArgumentException($"Device '{deviceName}' not found.", nameof(deviceName));
      return dev;
    }


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
    private static extern int midiOutGetDevCaps(IntPtr deviceId, ref MidiOutCapabilities caps, int sizeOfMidiOutCaps);

    [DllImport("winmm.dll")]
    private static extern int midiOutGetNumDevs();

    [DllImport("winmm.dll")]
    private static extern int midiOutOpen(out IntPtr handle, int deviceId, MidiOutProc proc, IntPtr instance,
      int flags);

    [DllImport("winmm.dll")]
    private static extern int midiOutClose(IntPtr handle);


    private delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

    private Queue<Action> DelegateQueue { get; } = new Queue<Action>();

    private readonly object _lockObject = new object();

    private IntPtr _handle;

    private int _bufferCount;

    private readonly MidiOutProc _midiOutProc;


    public OutputMidiDevice(MidiDeviceInfo device) : base(device)
    {
      _midiOutProc = HandleMessage;
      device.AssertType(MidiDeviceType.Output);
    }

    public OutputMidiDevice(string name)
      : this(Get(name))
    {
    }


    public void Send(ChannelMessage message)
    {
      Send(message.Message);
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
          RunMidiProc(() => midiOutPrepareHeader(_handle, ptr, SizeOfMidiHeader));
          _bufferCount++;

          // Send system exclusive message.
          try
          {
            RunMidiProc(() => midiOutLongMsg(_handle, ptr, SizeOfMidiHeader));
          }
          catch (MidiDeviceException)
          {
            RunMidiProc(() => midiOutUnprepareHeader(_handle, ptr, SizeOfMidiHeader));
            _bufferCount--;
            throw;
          }
        }
        // Else the system exclusive buffer could not be prepared.
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


    public override void Open()
    {
      if (IsOpen) return;
      RunMidiProc(() => midiOutOpen(out _handle, DeviceId, _midiOutProc, IntPtr.Zero, CallbackFunction));
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
          RunMidiProc(() => midiOutUnprepareHeader(_handle, headerPtr, SizeOfMidiHeader));
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

    public override void Dispose()
    {
      Reset();
      Close();
    }
  }
}