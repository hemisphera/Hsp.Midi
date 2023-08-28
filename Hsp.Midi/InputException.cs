using System.Runtime.InteropServices;
using System.Text;

namespace Hsp.Midi;

/// <summary>
/// The exception that is thrown when a error occurs with the InputDevice
/// class.
/// </summary>
public class InputException : MidiDeviceException
{

  [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
  private static extern int midiInGetErrorText(int errCode, StringBuilder errMsg, int sizeOfErrMsg);


  private readonly StringBuilder errMsg = new StringBuilder(128);

  /// <summary>
  /// Initializes a new instance of the InputDeviceException class with
  /// the specified error code.
  /// </summary>
  /// <param name="errCode">
  /// The error code.
  /// </param>
  public InputException(int errCode) : base(errCode)
  {
    // Get error message.
    midiInGetErrorText(errCode, errMsg, errMsg.Capacity);
  }

  /// <summary>
  /// Gets a message that describes the current exception.
  /// </summary>
  public override string Message => errMsg.ToString();

}