using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


class TTS_UdpListener
{
    // UDP 监听端口
    private const int UdpPort = 11316;
    private UdpClient _udpClient = null!;
    private CancellationTokenSource _cts = null!;

    /// <summary>
    /// 启动 UDP 异步监听
    /// </summary>
    public async Task StartListeningAsync()
    {
        _cts = new CancellationTokenSource();
        _udpClient = new UdpClient(UdpPort);
        var receiveTask = Task.CompletedTask;

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // 异步接收 UDP 数据报（非阻塞）
                receiveTask = ReceiveUdpDataAsync(_cts.Token);
                await receiveTask;
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需处理
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"UDP 监听异常：{ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _cts?.Dispose();
        }
    }

    /// <summary>
    /// 停止 UDP 监听
    /// </summary>
    public void StopListening()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 异步接收并解析 UDP 数据
    /// </summary>
    private async Task ReceiveUdpDataAsync(CancellationToken ct)
    {
        try
        {
            // 接收 UDP 数据（包含发送端信息）
            var result = await _udpClient.ReceiveAsync(ct);
            var remoteIp = result.RemoteEndPoint;
            var jsonStr = Encoding.UTF8.GetString(result.Buffer);

            // 解析并校验 JSON 为 TTS_Item
            var ttsItem = ParseTtsItem(jsonStr);
            if (ttsItem != null)
            {
                // 解析成功：处理合法的 TTS_Item
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✅ 【{ttsItem.AppRole}】 : {ttsItem.Text.Substring(0, Math.Min(30, ttsItem.Text.Length))}..."); // 只显示前20字符
                Console.ForegroundColor = oc;

                // 这里可以添加你的业务逻辑（比如加入播放队列）
                TTSQueue.Append(ttsItem);
            }
            else
            {
                // 解析失败：输出非法数据日志
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n❌ 解析 TTS_Item 失败：JSON 格式不合法或字段缺失");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"接收 UDP 数据异常：{ex.Message}");
                Console.ResetColor();
            }
        }
    }

    /// <summary>
    /// 解析 JSON 字符串为 TTS_Item，包含合法性校验
    /// </summary>
    private TTS_Item ParseTtsItem(string jsonStr)
    {
        try
        {
            // JSON 反序列化配置（忽略大小写、允许缺失非必填字段）
            // 修改 ParseTtsItem 方法中的 JsonSerializerOptions
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // 忽略大小写
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };


            // 反序列化为 TTS_Item
            var item = JsonSerializer.Deserialize<TTS_Item>(jsonStr, options);

            // 合法性校验（必填字段：Sid、Text 不能为空/空字符串）
            if (item == null || item.Sid <= 0 || string.IsNullOrWhiteSpace(item.Text))
            {
                return null;
            }

            // 可选：校正 Speed 范围（比如 0.5~2.0）
            item.Speed = Math.Clamp(item.Speed, 0.5f, 2.0f);

            return item;
        }
        catch (JsonException)
        {
            // JSON 格式错误（比如语法错误、字段类型不匹配）
            return null;
        }
    }
}