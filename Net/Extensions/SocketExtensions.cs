using Net.Core.Messages;
using System.Net.Sockets;

using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using Pastel;
using System.Drawing;

namespace Net.Extensions;

public static class SocketExtensions
{
    public static async Task SendNetMessage(this Socket socket, INetMessage message)
    {
        if (!socket.Connected)
        {
            System.Console.WriteLine($"[{"ERROR".Pastel(Color.Red)}] failed to send request due to socket being dead...");
            return;
        }

        var serialized = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(serialized);

        await socket.SendAsync(bytes, SocketFlags.None);
    }

    public static async Task<T?> ReadNetMessage<T>(this Socket socket) where T : INetMessage
    {
        if (!socket.Connected)
            return default;

        SpinWait.SpinUntil(() => socket.Available > 0);

        var result = await Task.Run(() =>
        {
            byte[] buff = new byte[1024];
            socket.Receive(buff);
            return buff;
        });

        if (result.Length <= 0)
            return default;

        return JsonConvert.DeserializeObject<T>(
            Encoding.Default.GetString(result)
        );
    }
}