using System;
using System.Runtime.InteropServices;

namespace Hsp.Midi;

public class VirtualMidiException : System.Exception
{
  private const int ErrorPathNotFound = 3;
  private const int ErrorInvalidHandle = 6;
  private const int ErrorTooManyCmds = 56;
  private const int ErrorTooManySess = 69;
  private const int ErrorInvalidName = 123;
  private const int ErrorModNotFound = 126;
  private const int ErrorBadArguments = 160;
  private const int ErrorAlreadyExists = 183;
  private const int ErrorOldWinVersion = 1150;
  private const int ErrorRevisionMismatch = 1306;
  private const int ErrorAliasExists = 1379;

  private VirtualMidiException(int reasonCode) : base(ReasonCodeToString(reasonCode))
  {
    ReasonCode = reasonCode;
  }

  public int ReasonCode { get; }

  private static string ReasonCodeToString(int reasonCode)
  {
    switch (reasonCode)
    {
      case ErrorOldWinVersion:
        return "Your Windows-version is too old for dynamic MIDI-port creation.";

      case ErrorInvalidName:
        return "You need to specify at least 1 character as MIDI-portname!";

      case ErrorAlreadyExists:
        return "The name for the MIDI-port you specified is already in use!";

      case ErrorAliasExists:
        return "The name for the MIDI-port you specified is already in use!";

      case ErrorPathNotFound:
        return "Possibly the teVirtualMIDI-driver has not been installed!";

      case ErrorModNotFound:
        return "The teVirtualMIDIxx.dll could not be loaded!";

      case ErrorRevisionMismatch:
        return "The teVirtualMIDIxx.dll and teVirtualMIDI.sys driver differ in version!";

      case ErrorTooManySess:
        return "Maximum number of ports reached";

      case ErrorInvalidHandle:
        return "Port not enabled";

      case ErrorTooManyCmds:
        return "MIDI-command too large";

      case ErrorBadArguments:
        return "Invalid flags specified";

      default:
        return "Unspecified virtualMIDI-error: " + reasonCode;
    }
  }


  public static void ThrowExceptionForReasonCode(int reasonCode)
  {
    var exception = new VirtualMidiException(reasonCode);
    throw exception;
  }

  public static void ThrowLastError()
  {
    ThrowExceptionForReasonCode(Marshal.GetLastWin32Error());
  }
}