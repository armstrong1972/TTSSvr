import socket
import json
import time

# 配置UDP服务端信息（对应你的C#控制台程序）
UDP_SERVER_IP = "127.0.0.1"  # 本地测试用，若跨机器则改为C#程序所在IP
UDP_SERVER_PORT = 11316      # 需和C#程序中监听的端口一致
BUFFER_SIZE = 1024           # 缓冲区大小

def send_tts_udp(app_role, sid, text, speed=1.2, exclusive=True):
    """
    向UDP服务端发送TTS_Item格式的JSON数据
    :param app_role: 应用标识（区分不同程序）
    :param sid: 音色ID
    :param text: 合成文本
    :param speed: 语速（默认1.2）
    :param exclusive: 是否独占播放（默认True）
    """
    # 构造和C# TTS_Item一致的字典结构
    tts_item = {
        "AppRole": app_role,
        "Sid": sid,
        "Text": text,
        "Speed": speed,
        "Exclusive": exclusive
    }

    # 将字典转为JSON字符串（确保编码为UTF-8）
    json_data = json.dumps(tts_item, ensure_ascii=False)
    data_bytes = json_data.encode("utf-8")

    # 创建UDP客户端socket
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_socket:
        try:
            # 发送数据到服务端
            udp_socket.sendto(data_bytes, (UDP_SERVER_IP, UDP_SERVER_PORT))
            print(f"✅ 发送成功（AppRole: {app_role}）：")
            print(f"   JSON数据：{json_data}")
        except Exception as e:
            print(f"❌ 发送失败：{str(e)}")

if __name__ == "__main__":
    print("=== Python UDP TTS 测试客户端 ===")
    print(f"目标服务端：{UDP_SERVER_IP}:{UDP_SERVER_PORT}\n")
    input("按下回车键开始发送测试数据...")

    # 测试场景1：模拟"音乐程序"发送TTS数据
    send_tts_udp(
        app_role="MusicApp",
        sid=55,
        text="这是音乐程序发送的语音合成测试,音乐是人类最美的语言，跨越了种族、阶级、出生，音乐面前人人生而平等。",
        speed=1.0,
        exclusive=False
    )

    # 测试场景2：模拟"办公程序"发送TTS数据
    send_tts_udp(
        app_role="OfficeApp",
        sid=33,
        text="这是办公程序发送的语音，语速更快",
        speed=1.5,
        exclusive=True
    )

    # 测试场景3：模拟"默认程序"发送极简数据（使用默认值）
    send_tts_udp(
        app_role="DefaultApp",
        sid=85,
        text="默认程序的测试文本"
    )

    print("\n所有测试数据发送完成！")