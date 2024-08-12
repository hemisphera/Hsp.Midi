﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hsp.Midi;

public sealed class VirtualMidiPort : IDisposable
{
  private readonly IntPtr _instance;
  private readonly uint _maxSysexLength;
  private readonly CancellationTokenSource _ct;
  private readonly byte[] _readBuffer;
  private const uint TeVmDefaultSysexSize = 65535;

  public const uint TeVmFlagsParseRx = 1;

  private const string DllName = "teVirtualMIDI.dll";


  [DllImport(DllName, EntryPoint = "virtualMIDICreatePortEx2", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern IntPtr virtualMIDICreatePortEx2(string portName, IntPtr callback, IntPtr dwCallbackInstance, uint maxSysexLength, uint flags);

  [DllImport(DllName, EntryPoint = "virtualMIDIClosePort", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern void virtualMIDIClosePort(IntPtr instance);

  [DllImport(DllName, EntryPoint = "virtualMIDISendData", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern bool virtualMIDISendData(IntPtr midiPort, byte[] midiDataBytes, uint length);

  [DllImport(DllName, EntryPoint = "virtualMIDIGetData", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern bool virtualMIDIGetData(IntPtr midiPort, [Out] byte[] midiDataBytes, ref uint length);

  [DllImport(DllName, EntryPoint = "virtualMIDIShutdown", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern bool virtualMIDIShutdown(IntPtr instance);

  public static VirtualMidiPort Create(string portName, uint maxSysexLength = TeVmDefaultSysexSize, uint flags = TeVmFlagsParseRx)
  {
    var instance = virtualMIDICreatePortEx2(portName, IntPtr.Zero, IntPtr.Zero, maxSysexLength, flags);
    if (instance == IntPtr.Zero)
    {
      VirtualMidiException.ThrowLastError();
    }

    return new VirtualMidiPort(instance, maxSysexLength);
  }


  private VirtualMidiPort(IntPtr instance, uint maxSysexLength)
  {
    _instance = instance;
    _readBuffer = new byte[maxSysexLength];
    _maxSysexLength = maxSysexLength;
    _ct = new CancellationTokenSource();
    Task.Run(() =>
    {
      while (!_ct.Token.IsCancellationRequested)
      {
        try
        {
          _ct.Token.ThrowIfCancellationRequested();
          var command = ReadCommand();
          WriteCommand(command);
        }
        catch
        {
          // ignore
        }
      }
    });
  }

  private byte[] ReadCommand()
  {
    var length = _maxSysexLength;
    if (!virtualMIDIGetData(_instance, _readBuffer, ref length))
    {
      VirtualMidiException.ThrowLastError();
    }

    var outBytes = new byte[length];
    Array.Copy(_readBuffer, outBytes, length);
    return outBytes;
  }

  public void WriteCommand(byte[] command)
  {
    if (command == null || command.Length == 0)
    {
      return;
    }

    if (!virtualMIDISendData(_instance, command, (uint)command.Length))
    {
      VirtualMidiException.ThrowLastError();
    }
  }

  private void Shutdown()
  {
    if (!virtualMIDIShutdown(_instance))
    {
      VirtualMidiException.ThrowLastError();
    }
  }

  public void Dispose()
  {
    if (_instance == IntPtr.Zero) return;
    _ct.Cancel();
    //virtualMIDIClosePort(_instance);
    Shutdown();
  }
}