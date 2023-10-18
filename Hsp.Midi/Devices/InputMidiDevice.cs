using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public sealed class InputMidiDevice : MidiDevice, IInputMidiDevice
{
  private const int MimOpen = 0x3C1;
  private const int MimClose = 0x3C2;
  private const int MimData = 0x3C3;
  private const int MimLongdata = 0x3C4;
  private const int MimError = 0x3C5;
  private const int MimLongerror = 0x3C6;
  private const int MimMoredata = 0x3CC;

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


  private int _bufferCount;

  private readonly object _lockObject = new object();

  private readonly MidiInProc _midiInProc;

  private IntPtr _handle;

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
      case MimOpen:
        break;
      case MimClose:
        break;
      case MimData:
      case MimMoredata:
        HandleShortMessage(param);
        break;
      case MimLongdata:
        HandleSysExMessage(param);
        break;
      case MimError:
        InvalidShortMessageReceived?.Invoke(this, param.Param1.ToInt32());
        break;
      case MimLongerror:
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
      var header = MidiHeader.FromPointer(headerPtr);
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
      var header = MidiHeader.FromPointer(headerPtr);

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

    MidiHeader.Deallocate(headerPtr);
    DecrementBufferCount();
    Monitor.Pulse(_lockObject);
  }

  private void AddSysExBuffer()
  {
    var headerPtr = MidiHeader.Allocate(new byte[SysExBufferSize]);

    try
    {
      RunMidiProc(() => midiInPrepareHeader(_handle, headerPtr, SizeOfMidiHeader));
      IncrementBufferCount();

      try
      {
        RunMidiProc(() => midiInAddBuffer(_handle, headerPtr, SizeOfMidiHeader));
      }
      catch
      {
        RunMidiProc(() => midiInUnprepareHeader(_handle, headerPtr, SizeOfMidiHeader));
        DecrementBufferCount();
        throw;
      }
    }
    catch
    {
      MidiHeader.Deallocate(headerPtr);
      throw;
    }
  }

  private void IncrementBufferCount()
  {
    lock (_lockObject)
    {
      _bufferCount++;
    }
  }

  private void DecrementBufferCount()
  {
    lock (_lockObject)
    {
      _bufferCount--;
    }
  }

  private delegate void MidiInProc(IntPtr handle, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

  [DllImport("winmm.dll")]
  private static extern int midiInOpen(out IntPtr handle, int deviceId, MidiInProc proc, IntPtr instance, int flags);

  [DllImport("winmm.dll")]
  private static extern int midiInClose(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiInStart(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiInReset(IntPtr handle);

  [DllImport("winmm.dll")]
  private static extern int midiInPrepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiInUnprepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiInAddBuffer(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

  [DllImport("winmm.dll")]
  private static extern int midiInGetDevCaps(IntPtr deviceId, ref MidiInCapabilities caps, int sizeOfMidiInCaps);

  [DllImport("winmm.dll")]
  private static extern int midiInGetNumDevs();
}