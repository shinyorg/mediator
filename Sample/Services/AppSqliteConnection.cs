using SQLite;

namespace Sample.Services;


public class AppSqliteConnection : SQLiteAsyncConnection
{
    public AppSqliteConnection(IPlatform platform) : base(Path.Combine(platform.AppData.FullName, "app.db"), true)
    {
        var data = this.GetConnection();
        data.CreateTable<LogModel>();
    }

    public Task Log(string area, MyMessageEvent @event) => this.InsertAsync(new LogModel
    {
        Area = area,
        Arg = @event.Arg,
        FireAndForget = @event.FireAndForgetEvents,
        Timestamp = DateTimeOffset.UtcNow
    });
    public AsyncTableQuery<LogModel> Logs => this.Table<LogModel>();
}

public class LogModel
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }

    public string Area { get; set; }
    public string Arg { get; set; }
    public bool FireAndForget { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}