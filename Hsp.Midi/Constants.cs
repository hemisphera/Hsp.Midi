using System.Runtime.InteropServices;

namespace Hsp.Midi;

internal static class Constants
{
  public const int CallbackFunction = 0x30000;

  public const int CallbackEvent = 0x50000;

  public static readonly int SizeOfMidiHeader = Marshal.SizeOf(typeof(MidiHeader));
}