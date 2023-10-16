using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public sealed class InputMidiDevice : MidiDevice, IInputMidiDevice
{
  public static MidiDeviceInfo[] Enumerate()
  {
    var count = midiInGetNumDevs();
    return Enumerable.Range(0, count).Select(Get).ToArray();
  }

  public static MidiDeviceInfo Get(int deviceId)
  {
    var caps = new MidiInCapabilities();
    var devId = (IntPtr)deviceId;
    RunMidiProc(MidiDeviceType.Input, () => midiInGetDevCaps(devId, ref caps, SizeOfMidiHeader));
    return new MidiDeviceInfo(deviceId, caps);
  }

  public static MidiDeviceInfo Get(string deviceName)
  {
    var dev = Enumerate().FirstOrDefault(a => a.Name == deviceName);
    if (dev == null)
      throw new ArgumentException($"Device '{deviceName}' not found.", nameof(deviceName));
    return dev;
  }


  private volatile int _bufferCount = 0;

  private readonly object _lockObject = new object();

  private readonly MidiInProc _midiInProc;

  private MidiHeaderBuilder HeaderBuilder { get; } = new MidiHeaderBuilder();

  private IntPtr _handle;

  private bool HandleValid => _handle != default;

  public int SysExBufferSize { get; set; } = 4096;

  private bool IsResetting { get; set; }

  private List<byte> SysExDataBuffer { get; } = new List<byte>();


  /// <summary>
  /// Initializes a new instance of the InputDevice class with the 
  /// specified device ID.
  /// </summary>
  public InputMidiDevice(MidiDeviceInfo device)
    : base(device)
  {
    device.AssertType(MidiDeviceType.Input);
    _midiInProc = HandleMessage;
  }

  public InputMidiDevice(string deviceName)
    : this(Get(deviceName))
  {
  }


  public override void Open()
  {
    if (IsOpen) return;
    RunMidiProc(() => midiInOpen(out _handle, DeviceId, _midiInProc, IntPtr.Zero, CallbackFunction));
    lock (_lockObject)
    {
      foreach (var _ in Enumerable.Range(0, 4))
        AddSysExBuffer();
      RunMidiProc(() => midiInStart(_handle));
    }

    IsOpen = true;
  }

  public override void Close()
  {
    if (!IsOpen) return;

    Reset();
    RunMidiProc(() => midiInClose(_handle));

    IsOpen = false;
    _handle = default;
  }

  public override void Reset()
  {
    if (!IsOpen) return;
    lock (_lockObject)
    {
      IsResetting = true;
      try
      {
        RunMidiProc(() => midiInReset(_handle));
        while (_bufferCount > 0)
          Monitor.Wait(_lockObject);
      }
      finally
      {
        IsResetting = false;
      }
    }
  }

  public override void Dispose()
  {
    Reset();
    Close();
  }

  /// <summary>
  /// Occurs when any message was received. The underlying type of the message is as specific as possible.
  /// Channel, Common, Realtime or SysEx.
  /// </summary>
  public event EventHandler<IMidiMessage> MessageReceived;

  public event EventHandler<int> InvalidShortMessageReceived;

  public event EventHandler<byte[]> InvalidSysExMessageReceived;

  private void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
  {
    var param = new MidiInParams(param1, param2);
    switch (msg)
    {
      case MIM_OPEN:
        break;
      case MIM_CLOSE:
        break;
      case MIM_DATA:
      case MIM_MOREDATA:
        HandleShortMessage(param);
        break;
      case MIM_LONGDATA:
        HandleSysExMessage(param);
        break;
      case MIM_ERROR:
        InvalidShortMessageReceived?.Invoke(this, param.Param1.ToInt32());
        break;
      case MIM_LONGERROR:
        HandleInvalidSysExMessage(param);
        break;
    }
  }

  private void HandleShortMessage(MidiInParams param)
  {
    var msg = MessageBuilder.Build(param);
    MessageReceived?.Invoke(this, msg);
  }

  private void HandleSysExMessage(object state)
  {
    lock (_lockObject)
    {
      var param = (MidiInParams)state;
      var headerPtr = param.Param1;

      var header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

      if (!IsResetting)
      {
        for (var i = 0; i < header.bytesRecorded; i++)
        {
          SysExDataBuffer.Add(Marshal.ReadByte(header.data, i));
        }

        if (SysExDataBuffer.Count > 1 && SysExDataBuffer[0] == 0xF0 && SysExDataBuffer[^1] == 0xF7)
        {
          var message = new SysExMessage(SysExDataBuffer.ToArray())
          {
            Timestamp = param.Param2.ToInt32()
          };

          SysExDataBuffer.Clear();

          MessageReceived?.Invoke(this, message);
        }

        try
        {
          AddSysExBuffer();
        }
        catch (MidiDeviceException ex)
        {
          RaiseErrorEvent(ex);
        }
      }

      ReleaseBuffer(headerPtr);
    }
  }

  private void HandleInvalidSysExMessage(object state)
  {
    lock (_lockObject)
    {
      var param = (MidiInParams)state;
      var headerPtr = param.Param1;

      var header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));
      if (!IsResetting)
      {
        var data = new byte[header.bytesRecorded];

        Marshal.Copy(header.data, data, 0, data.Length);

        InvalidSysExMessageReceived?.Invoke(this, data);
        try
        {
          AddSysExBuffer();
        }
        catch (MidiDeviceException ex)
        {
          RaiseErrorEvent(ex);
        }
      }

      ReleaseBuffer(headerPtr);
    }
  }

  private void ReleaseBuffer(IntPtr headerPtr)
  {
    try
    {
      RunMidiProc(() => midiInUnprepareHeader(_handle, headerPtr, SizeOfMidiHeader));
    }
    catch (MidiDeviceException ex)
    {
      RaiseErrorEvent(ex);
    }

    HeaderBuilder.Destroy(headerPtr);

    _bufferCount--;

    Debug.Assert(_bufferCount >= 0);

    Monitor.Pulse(_lockObject);
  }

  private void AddSysExBuffer()
  {
    // Initialize the MidiHeader builder.
    HeaderBuilder.BufferLength = SysExBufferSize;
    HeaderBuilder.Build();

    // Get the pointer to the built MidiHeader.
    var headerPtr = HeaderBuilder.Result;

    try
    {
      RunMidiProc(() => midiInPrepareHeader(_handle, headerPtr, SizeOfMidiHeader));
      _bufferCount++;

      try
      {
        RunMidiProc(() => midiInAddBuffer(_handle, headerPtr, SizeOfMidiHeader));
      }
      catch
      {
        RunMidiProc(() => midiInUnprepareHeader(_handle, headerPtr, SizeOfMidiHeader));
        _bufferCount--;
        throw;
      }
    }
    // Else the header could not be prepared.
    catch
    {
      HeaderBuilder.Destroy();
      throw;
    }
  }

  // Represents the method that handles messages from Windows.
  private delegate void MidiInProc(IntPtr handle, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

  [DllImport("winmm.dll")]
  private static extern int midiInOpen(out IntPtr handle, int deviceID,
    MidiInProc proc, IntPtr instance, int flags);

  [DllImport("winmm.dll")]
  private static extern int midiInClose(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiInStart(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiInStop(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiInReset(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiInPrepareHeader(IntPtr handle,
    IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiInUnprepareHeader(IntPtr handle,
    IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiInAddBuffer(IntPtr handle,
    IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiInGetDevCaps(IntPtr deviceID,
    ref MidiInCapabilities caps, int sizeOfMidiInCaps);

  [DllImport("winmm.dll")]
  private static extern int midiInGetNumDevs();

  private const int MIDI_IO_STATUS = 0x00000020;
  private const int MIM_OPEN = 0x3C1;
  private const int MIM_CLOSE = 0x3C2;
  private const int MIM_DATA = 0x3C3;
  private const int MIM_LONGDATA = 0x3C4;
  private const int MIM_ERROR = 0x3C5;
  private const int MIM_LONGERROR = 0x3C6;
  private const int MIM_MOREDATA = 0x3CC;
  private const int MHDR_DONE = 0x00000001;
}