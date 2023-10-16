using System;

namespace Hsp.Midi
{

  internal readonly struct MidiInParams
  {

    public readonly IntPtr Param1;

    public readonly IntPtr Param2;


    public MidiInParams(IntPtr param1, IntPtr param2)
    {
      Param1 = param1;
      Param2 = param2;
    }

  }

}