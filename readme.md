# Hsp.Midi
A simple MIDI library for C#

This library is a fork (and rewrite) from Sanford.Multimedia.Midi (https://github.com/tebjan/Sanford.Multimedia) or rather the original at https://www.codeproject.com/Articles/6228/C-MIDI-Toolkit. The idea is to purge everything that is not strictly necessary for MIDI device and message handling.

This means:
- no MIDI file support
- no MIDI player
- no utilities and classes (collections) to handle them
- no timers

Everything has been broken down to the bare minimum. As such it supports:
- MIDI devices
- MIDI messages

