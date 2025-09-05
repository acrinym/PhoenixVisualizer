using System;
using System.Reflection;
using LibVLCSharp.Shared;

var libVLC = new LibVLC();
var mediaPlayer = new MediaPlayer(libVLC);

Console.WriteLine("Audio cleanup callback delegate type:");
var audioCleanupCbType = typeof(MediaPlayer).GetMethod("SetAudioFormatCallback")?.GetParameters()[1].ParameterType;
Console.WriteLine($"LibVLCAudioCleanupCb: {audioCleanupCbType}");

if (audioCleanupCbType != null)
{
    var invokeMethod = audioCleanupCbType.GetMethod("Invoke");
    if (invokeMethod != null)
    {
        var parameters = invokeMethod.GetParameters();
        Console.WriteLine($"Parameters: {string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"))}");
    }
}