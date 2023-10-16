namespace Hsp.Midi
{

    /// <summary>
    /// Defines constants representing the various system realtime message types.
    /// </summary>
    public enum SysRealtimeType
    {

        /// <summary>
        /// Represents the clock system realtime type.
        /// </summary>
        Clock = 0xF8,

        /// <summary>
        /// Represents the start system realtime type.
        /// </summary>
        Start = 0xFA,

        /// <summary>
        /// Represents the continue system realtime type.
        /// </summary>
        Continue = 0xFB,

        /// <summary>
        /// Represents the stop system realtime type.
        /// </summary>
        Stop = 0xFC,

        /// <summary>
        /// Represents the active sense system realtime type.
        /// </summary>
        ActiveSense = 0xFE,

        /// <summary>
        /// Represents the reset system realtime type.
        /// </summary>
        Reset = 0xFF

    }

}