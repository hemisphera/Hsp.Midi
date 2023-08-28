using System.Runtime.InteropServices;
using System.Text;

namespace Hsp.Midi;

/// <summary>
/// The exception that is thrown when a error occurs with the OutputDevice
/// class.
/// </summary>
public class OutputException : MidiDeviceException
{

  [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
  private static extern int midiOutGetErrorText(int errCode, StringBuilder message, int sizeOfMessage);

  // The error message.
  private readonly StringBuilder _message = new StringBuilder(128);

  /// <summary>
  /// Initializes a new instance of the OutputDeviceException class with
  /// the specified error code.
  /// </summary>
  /// <param name="errCode">
  /// The error code.
  /// </param>
  public OutputException(int errCode) : base(errCode)
  {
    // Get error message.
    midiOutGetErrorText(errCode, _message, _message.Capacity);
  }

  /// <summary>
  /// Gets a message that describes the current exception.
  /// </summary>
  public override string Message => _message.ToString();

}