using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Hsp.Midi.Messages;

namespace Hsp.Midi;

public sealed class InputMidiDevice : MidiDevice, IInputMidiDevice
{
  private int _bufferCount;
  private IntPtr _handle;
  private bool _isResetting;

  private readonly object _lockObject = new();
  private readonly MidiInProc _midiInProc;
  private readonly List<byte> _sysExDataBuffer = new();

  public int SysExBufferSize { get; set; } = 4096;


  public InputMidiDevice(MidiDeviceInfo device)
    : base(device)
  {
    device.AssertType(MidiDeviceType.Input);
    _midiInProc = HandleMessage;
  }


  public override void Open()
  {
    if (IsOpen) return;
    RunMidiProc(() => midiInOpen(out _handle, DeviceId, _midiInProc, IntPtr.Zero, Constants.CallbackFunction));
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
      _isResetting = true;
      try
      {
        RunMidiProc(() => midiInReset(_handle));
        while (_bufferCount > 0)
          Monitor.Wait(_lockObject);
      }
      finally
      {
        _isResetting = false;
      }
    }
  }

  /// <summary>
  /// Occurs when any message was received. The underlying type of the message is as specific as possible.
  /// Channel, Common, Realtime or SysEx.
  /// </summary>
  public event EventHandler<IMidiMessage>? MessageReceived;

  public event EventHandler<int>? InvalidShortMessageReceived;

  public event EventHandler<byte[]>? InvalidSysExMessageReceived;

  private void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
  {
    const int mimOpen = 0x3C1;
    const int mimClose = 0x3C2;
    const int mimData = 0x3C3;
    const int mimLongdata = 0x3C4;
    const int mimError = 0x3C5;
    const int mimLongerror = 0x3C6;
    const int mimMoredata = 0x3CC;

    var param = new MidiInParams(param1, param2);
    switch (msg)
    {
      case mimOpen:
        break;
      case mimClose:
        break;
      case mimData:
      case mimMoredata:
        HandleShortMessage(param);
        break;
      case mimLongdata:
        HandleSysExMessage(param);
        break;
      case mimError:
        InvalidShortMessageReceived?.Invoke(this, param.Param1.ToInt32());
        break;
      case mimLongerror:
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
      if (!_isResetting)
      {
        for (var i = 0; i < header.bytesRecorded; i++)
        {
          _sysExDataBuffer.Add(Marshal.ReadByte(header.data, i));
        }

        if (_sysExDataBuffer.Count > 1 && _sysExDataBuffer[0] == 0xF0 && _sysExDataBuffer[^1] == 0xF7)
        {
          var message = new SysExMessage(_sysExDataBuffer.ToArray())
          {
            Timestamp = param.Param2.ToInt32()
          };

          _sysExDataBuffer.Clear();

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

      if (!_isResetting)
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
      RunMidiProc(() => midiInUnprepareHeader(_handle, headerPtr, Constants.SizeOfMidiHeader));
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
      RunMidiProc(() => midiInPrepareHeader(_handle, headerPtr, Constants.SizeOfMidiHeader));
      IncrementBufferCount();

      try
      {
        RunMidiProc(() => midiInAddBuffer(_handle, headerPtr, Constants.SizeOfMidiHeader));
      }
      catch
      {
        RunMidiProc(() => midiInUnprepareHeader(_handle, headerPtr, Constants.SizeOfMidiHeader));
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
}