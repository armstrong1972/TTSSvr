using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System;
using System.Threading;
using System.Threading.Tasks;
using PortAudioSharp;
using SherpaOnnx;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Serialization;
using System.Security.Cryptography;


// 1. 定义TTS_Item类，包含sid、text、speed、bExclusive四个参数
public class TTS_Item
{
    public string AppRole { get; set; }
    public int Sid { get; set; }
    public string Text { get; set; }
    public float Speed { get; set; }
    public bool Exclusive { get; set; }

    // 无参构造（必须保留）
    public TTS_Item()
    {
        Sid = 0;
        Text = string.Empty;
        Speed = 1.2f;
        Exclusive = true;
        AppRole = string.Empty;
    }

    // 补全AppRole参数，并添加[JsonConstructor]特性
    [JsonConstructor] // 关键：指定JSON反序列化用这个构造函数
    public TTS_Item(string appRole, int sid, string text, float speed = 1.2f, bool exclusive = true)
    {
        AppRole = appRole;   // 新增AppRole参数绑定
        Sid = sid;
        Text = text;
        Speed = speed;
        Exclusive = exclusive;
    }
}

class TTSQueue
{
    // 2. 定义TTS_Item的线程安全队列（推荐使用ConcurrentQueue保证多线程安全）
    private static ConcurrentQueue<TTS_Item> TTS_PlayList = new ConcurrentQueue<TTS_Item>();
    private static StreamParameters _sp ;
    private static OfflineTts _tts = null!;

    public static void Init(OfflineTts tts, StreamParameters streamP)
    {
        _tts = tts;
        _sp = streamP;
        TTS_PlayList.Clear();
    }

    public static async Task StartMonitor()
    {
        _ = StartMonitorTask();
    }



    public static void Clear()
    {
        TTS_PlayList.Clear();
    }

    private static async Task TTSPlay(int sid, string text, float speed = 1.2f)
    {
        var player = new TTSPlayer(_tts, _sp);
        await Task.Run(() => player.TTSPlay(sid, text, speed));
    }

    public static async Task StartTTSPlay(int sid, string text, float speed = 1.2f, bool bExclusive = true)
    {
        if (bExclusive)
        {
            await TTSPlay(sid, text, speed);
        }
        else
        {
            _ = TTSPlay(sid, text, speed);
        }
    }
    public static async Task StartTTSPlay(TTS_Item item)
    {
        if (item.Exclusive)
        {
            await TTSPlay(item.Sid,item.Text,item.Speed);
        }
        else
        {
            _ = TTSPlay(item.Sid, item.Text, item.Speed);
        }
    }

    public static void Append(TTS_Item item)
    {
        string shortText = item.Text.Substring(0, Math.Min(item.Text.Length, 30));
        ConsoleColor oc = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow; 
        Console.WriteLine($"\n ###<- Queue Add Item : [{item.AppRole}]{shortText}...");
        Console.ForegroundColor = oc;

        TTS_PlayList.Enqueue(item);
    }

    private static async Task StartMonitorTask()
    {
        Console.WriteLine("\n\n");
        while (true)
        {
            Console.Write($"\r ###== Queue Pool: {TTS_PlayList.Count}");
            if (TTS_PlayList.Count > 0)
            {
                if (TTS_PlayList.TryDequeue(out TTS_Item? item))
                {
                    if (item != null)
                    {
                        ConsoleColor oc = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;
                        string shortText = item.Text.Substring(0, Math.Min(item.Text.Length, 30));
                        Console.WriteLine($"\n ###-> Queue Get Item : [{item.AppRole}]{shortText}...");
                        Console.ForegroundColor = oc;

                        await StartTTSPlay(item);
                    }
                }
            }
            else
            {
                await Task.Delay(100); // 等待一段时间后继续检查队列
            }
        }
    }

}

