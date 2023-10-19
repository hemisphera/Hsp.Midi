using System;
using System.Linq;

namespace Hsp.Midi.Messages;

public interface IMidiMessage
{
  byte[] GetBytes();

  int Status { get; }

  public string ToHexString()
  {
    return String.Join(" ", GetBytes().Select(b => b.ToString("X2")));
  }
}