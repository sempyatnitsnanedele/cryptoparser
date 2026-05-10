using Parser;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var assets = new Dictionary<int, (string Name, double Price, double Change)>
{
    { 1, ("Bitcoin", 0, 0) },
    { 1027, ("Ethereum", 0, 0) },
    { 1839, ("BNB", 0, 0) },
    { 2010, ("Cardano", 0, 0) }
};

var table = new Table().RoundedBorder();
table.AddColumn("[yellow]Актив[/]");
table.AddColumn("[yellow]Цена (USD)[/]");
table.AddColumn("[yellow]Тенденция[/]");

var parser = new Parse(assets.Keys.ToArray());

parser.OnPriceUpdate += (id, newPrice) =>
{
    if (assets.ContainsKey(id))
    {
        var oldPrice = assets[id].Price;
        assets[id] = (assets[id].Name, newPrice, newPrice - oldPrice);
    }
};

await AnsiConsole.Live(table).StartAsync(async ctx =>
{
    _ = parser.RunAsync(CancellationToken.None);

    while (true)
    {
        table.Rows.Clear();
        foreach (var asset in assets.Values)
        {
            string color = asset.Change > 0 ? "green" : asset.Change < 0 ? "red" : "white";
            string icon = asset.Change > 0 ? "↑" : asset.Change < 0 ? "↓" : "•";
            table.AddRow(asset.Name, $"[bold]{asset.Price:N4}[/]", $"[{color}]{icon}[/]");
        }
        ctx.Refresh();
        await Task.Delay(500);
    }
});