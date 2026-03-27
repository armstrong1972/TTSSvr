using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System;
using System.Threading;
using System.Threading.Tasks;
using PortAudioSharp;
using SherpaOnnx;


class TTSPlayer
{
    private StreamParameters _sp;
    private OfflineTts _tts;
    public TTSPlayer(OfflineTts tts , StreamParameters streamP)
    {
        _tts = tts;
        _sp = streamP;
    }

    public void TTSPlay(int sid, string text, float speed = 1.2f)
    {
        string shortText = text.Substring(0, Math.Min(text.Length, 20));
        var dataItems = new BlockingCollection<float[]>();
        
        var MyCallback = (IntPtr samples, int n, float progress) =>
        {
            //Console.WriteLine($"  -- Progress {progress:P2} : {shortText}");
            float[] data = new float[n];
            Marshal.Copy(samples, data, 0, n);
            dataItems.Add(data);
            // 1 means to keep generating
            // 0 means to stop generating
            return 1;
        };

        var playFinished = false;

        float[]? lastSampleArray = null;
        int lastIndex = 0; // not played

        PortAudioSharp.Stream.Callback playCallback = (IntPtr input, IntPtr output,
            UInt32 frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            IntPtr userData
            ) =>
        {
            if (dataItems.IsCompleted && lastSampleArray == null && lastIndex == 0)
            {
                //Console.WriteLine($"Finished : {text}");
                playFinished = true;
                return StreamCallbackResult.Complete;
            }

            int expected = Convert.ToInt32(frameCount);
            int i = 0;

            while ((lastSampleArray != null || dataItems.Count != 0) && (i < expected))
            {
                int needed = expected - i;
                if (lastSampleArray != null)
                {
                    int remaining = lastSampleArray.Length - lastIndex;
                    if (remaining >= needed)
                    {
                        float[] this_block = lastSampleArray.Skip(lastIndex).Take(needed).ToArray();
                        lastIndex += needed;
                        if (lastIndex == lastSampleArray.Length)
                        {
                            lastSampleArray = null;
                            lastIndex = 0;
                        }

                        Marshal.Copy(this_block, 0, IntPtr.Add(output, i * sizeof(float)), needed);
                        return StreamCallbackResult.Continue;
                    }

                    float[] this_block2 = lastSampleArray.Skip(lastIndex).Take(remaining).ToArray();
                    lastIndex = 0;
                    lastSampleArray = null;

                    Marshal.Copy(this_block2, 0, IntPtr.Add(output, i * sizeof(float)), remaining);
                    i += remaining;
                    continue;
                }

                if (dataItems.Count != 0)
                {
                    lastSampleArray = dataItems.Take();
                    lastIndex = 0;
                }
            }

            if (i < expected)
            {
                int sizeInBytes = (expected - i) * 4;
                Marshal.Copy(new byte[sizeInBytes], 0, IntPtr.Add(output, i * sizeof(float)), sizeInBytes);
            }

            return StreamCallbackResult.Continue;
        };

        PortAudioSharp.Stream stream = new PortAudioSharp.Stream(inParams: null, outParams: _sp, sampleRate: _tts.SampleRate,
            framesPerBuffer: 0,
            streamFlags: StreamFlags.ClipOff,
            callback: playCallback,
            userData: IntPtr.Zero
            );

        stream.Start();
        var callback = new OfflineTtsCallbackProgress(MyCallback);
        //Console.WriteLine($" ### Start: {text}");
        _tts.GenerateWithCallbackProgress(text, speed, sid, callback);
        //Console.WriteLine($" $$$   End: {text}");
        dataItems.CompleteAdding();

        while (!playFinished)
        {
            Thread.Sleep(200); // 200ms
        }
    }

}

