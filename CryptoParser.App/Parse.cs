using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Parser;

public class Parse
{

    private readonly string _url = "wss://push.coinmarketcap.com/ws?device=web&client_source=home_page";
    private readonly int[] _assetIds;
    private int count = 1;

    public event Action<int, double>? OnPriceUpdate;

    public Parse(int[] assetIds)
    {
        _assetIds = assetIds;
    }

    private async Task SubscribeAsync(ClientWebSocket ws, CancellationToken ct)
    {
        string ids = string.Join(",", _assetIds);
        string[] subscriptions = {
            $"{{\"method\":\"RSUBSCRIPTION\",\"params\":[\"main-site@crypto_price_15s@{{}}@detail\",\"{ids}\"]}}",
            $"{{\"method\":\"RSUBSCRIPTION\",\"params\":[\"main-site@crypto_price_5s@{{}}@normal\",\"{ids}\"]}}"
        };

        foreach (var sub in subscriptions)
        {
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(sub)), WebSocketMessageType.Text, true, ct);
        }
    }
    private void ProcessMessage(string message)
    {
        try
        {
            var update = JsonSerializer.Deserialize<CryptoMessage>(message);
            if (update?.d != null)
            {
                OnPriceUpdate?.Invoke(update.d.id, update.d.p); 
            }
            
        }
        catch (Exception ex) { Console.WriteLine(ex); }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                ws.Options.SetRequestHeader("Origin", "https://coinmarketcap.com");
                ws.Options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");

                await ws.ConnectAsync(new Uri(_url), ct);
                count = 1;

                await SubscribeAsync(ws, ct);

                

                var buffer = new byte[1024 * 8];

                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {

                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessMessage(message);
                    }
                }

            }
            catch (Exception ex)
            { 
                Console.WriteLine(ex);
                await Task.Delay(5000*count, ct);
                if (count < 12) count++;
            }
        }
    }
}    



public class CryptoMessage
{
    public CryptoData? d { get; set; }
}
public class CryptoData
{
    public int id { get; set; }
    public double p { get; set; }
}


