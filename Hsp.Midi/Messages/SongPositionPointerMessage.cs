namespace Hsp.Midi.Messages;

public sealed class SongPositionPointerMessage : IMidiMessage
{
  public int Status => (byte)SysCommonType.SongPositionPointer;

  public int Position { get; }


  public SongPositionPointerMessage(int position)
  {
    Position = position;
  }


  public static SongPositionPointerMessage Parse(int data1, int data2)
  {
    var position = data2;
    position <<= 7;
    position |= (byte)data1;
    return new SongPositionPointerMessage(position);
  }

  public byte[] GetBytes()
  {
    var data1 = (byte)(Position & 0x7F);
    var data2 = (byte)((Position >> 7) & 0x7F);
    return [(byte)Status, data1, data2];
  }
}