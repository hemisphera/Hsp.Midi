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

    public static MidiDeviceInfo[] Enumerate()
    {
      var count = midiOutGetNumDevs();
      return Enumerable.Range(0, count).Select(Get).ToArray();
    }

    public static MidiDeviceInfo Get(int deviceId)
    {
      var caps = new MidiOutCapabilities();

      // Get the device's capabilities.
      var devId = (IntPtr)deviceId;
      var result = midiOutGetDevCaps(devId, ref caps, Marshal.SizeOf(caps));
      if (result != DeviceException.MmSysErrNoerror)
        throw new OutputException(result);
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
    private static extern int midiOutOpen(out IntPtr handle, int deviceId, MidiOutProc proc, IntPtr instance, int flags);

    [DllImport("winmm.dll")]
    private static extern int midiOutClose(IntPtr handle);


    private const int MOM_OPEN = 0x3C7;
    private const int MOM_CLOSE = 0x3C8;
    private const int MOM_DONE = 0x3C9;

    // Represents the method that handles messages from Windows.
    private delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

    // For releasing buffers.
    private Queue<Action> DelegateQueue { get; } = new Queue<Action>();

    private readonly object _lockObject = new object();

    private IntPtr Handle { get; set; }

    // The number of buffers still in the queue.
    private int bufferCount = 0;

    // Builds MidiHeader structures for sending system exclusive messages.
    private MidiHeaderBuilder headerBuilder = new MidiHeaderBuilder();

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

    public void Send(int message)
    {
      lock (_lockObject)
      {
        var result = midiOutShortMsg(Handle, message);
        if (result != DeviceException.MmSysErrNoerror)
          throw new OutputException(result);
      }
    }

    public void Send(SysExMessage message)
    {
      lock (_lockObject)
      {
        headerBuilder.InitializeBuffer(message);
        headerBuilder.Build();

        // Prepare system exclusive buffer.
        int result = midiOutPrepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);

        // If the system exclusive buffer was prepared successfully.
        if (result == DeviceException.MmSysErrNoerror)
        {
          bufferCount++;

          // Send system exclusive message.
          result = midiOutLongMsg(Handle, headerBuilder.Result, SizeOfMidiHeader);

          // If the system exclusive message could not be sent.
          if (result != DeviceException.MmSysErrNoerror)
          {
            midiOutUnprepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);
            bufferCount--;
            headerBuilder.Destroy();

            // Throw an exception.
            throw new OutputException(result);
          }
        }
        // Else the system exclusive buffer could not be prepared.
        else
        {
          // Destroy system exclusive buffer.
          headerBuilder.Destroy();

          // Throw an exception.
          throw new OutputException(result);
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
      var result = midiOutOpen(out var handle, DeviceId, _midiOutProc, IntPtr.Zero, CallbackFunction);
      Handle = handle;
      if (result != DeviceException.MmSysErrNoerror)
        throw new OutputException(result);
      IsOpen = true;
    }

    public override void Reset()
    {
      AssertDeviceOpen();
      lock (_lockObject)
      {
        // Reset the OutputDevice.
        var result = midiOutReset(Handle);
        if (result == DeviceException.MmSysErrNoerror)
        {
          while (bufferCount > 0)
            Monitor.Wait(_lockObject);
        }
        else
          throw new OutputException(result);
      }
    }

    public override void Close()
    {
      if (!IsOpen) return;
      var result = midiOutClose(Handle);
      if (result != DeviceException.MmSysErrNoerror)
        throw new OutputException(result);
      Handle = default;
      IsOpen = false;
    }


    // Handles Windows messages.
    private void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
    {
      if (msg == MOM_OPEN)
      {
      }
      else if (msg == MOM_CLOSE)
      {
      }
      else if (msg == MOM_DONE)
        DelegateQueue.Enqueue(() => ReleaseBuffer(param1));
    }

    // Releases buffers.
    private void ReleaseBuffer(object state)
    {
      lock (_lockObject)
      {
        var headerPtr = (IntPtr)state;

        // Unprepare the buffer.
        var result = midiOutUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);
        if (result != DeviceException.MmSysErrNoerror)
        {
          Exception ex = new OutputException(result);
          RaiseErrorEvent(ex);
        }

        // Release the buffer resources.
        headerBuilder.Destroy(headerPtr);

        bufferCount--;

        Monitor.Pulse(_lockObject);

        Debug.Assert(bufferCount >= 0);
      }
    }

    public override void Dispose()
    {
      Reset();
      Close();
    }

  }
}