using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System;
using System.Threading;
using System.Threading.Tasks;
using PortAudioSharp;
using SherpaOnnx;


class NewClass
{
    private StreamParameters _sp;
    private OfflineTts _tts;
    public NewClass(OfflineTts tts , StreamParameters streamP)
    {
        _tts = tts;
        _sp = streamP;
    }

    public void Play(int sid, string text, float speed = 1.2f)
    {
        
    }

}

