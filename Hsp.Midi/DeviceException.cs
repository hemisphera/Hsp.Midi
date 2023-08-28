using System;

namespace Hsp.Midi
{

  public abstract class DeviceException : ApplicationException
  {

    public const int MmSysErrNoerror = 0;  /* no error */

    public int ErrorCode { get; }

    protected DeviceException(int errorCode)
    {
      ErrorCode = errorCode;
    }

  }

}