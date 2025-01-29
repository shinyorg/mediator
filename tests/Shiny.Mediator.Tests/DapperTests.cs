using Dapper;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class DapperTests(ITestOutputHelper output)
{
    const string CONN_STRING = "Data Source=:memory:";
    
    // [Fact]
    // public async Task Scrutor_EndToEnd()
    // {
    //     var services = new ServiceCollection();
    //     services.AddShinyMediator(cfg => cfg
    //         // .AddDapper<SqliteConnection>(CONN_STRING)
    //         .AddDapper<NpgsqlConnection>(CONN_STRING)
    //     );
    //
    //     await RunDbHits(services.BuildServiceProvider());
    // }
    
    [Fact]
    public async Task DryIoc_EndToEnd()
    {
        using (var conn = new SqliteConnection(CONN_STRING))
        {
            conn.Open();
            conn.Execute("CREATE TABLE Users(Id INTEGER PRIMARY KEY, Email TEXT)");
        }
        var host = Host
            .CreateDefaultBuilder()
            .ConfigureLogging(x => x.AddXUnit(output))
            .UseServiceProviderFactory(new DryIocServiceProviderFactory())
            .ConfigureServices(services =>
            {
                services.AddShinyMediator(cfg => cfg
                    .AddDapper<SqliteConnection>(CONN_STRING)
                );
            })
            .Build();

        await RunDbHits(host.Services);
    }


    [Fact]
    public void RequestKeyTest()
    {
        var email = "allan%";
        var query = new DapperFirstQuery<User>(
            $"select * from \"Users\" where \"Email\" like {email}"
        );

        var requestKey = query.GetKey();
        output.WriteLine(requestKey);
        requestKey.ShouldBe("Shiny.Mediator.Tests.User_select * from \"Users\" where \"Email\" like allan%");
    }


    static async Task RunDbHits(IServiceProvider sp)
    {
        var mediator = sp.GetRequiredService<IMediator>();

        var email = "allan%";
        var result = await mediator.Request(new DapperFirstQuery<User>(
            $"select * from \"Users\" where \"Email\" like {email}"
        ));
        
        var results = await mediator.Request(new DapperQuery<User>(
            $"select * from \"Users\" where \"Email\" like {email}"
        ));
        
        var count = await mediator.Request(new DapperScalar(
            $"select count(*) from \"Users\" where \"Email\" like {email}"
        ));
    }
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
}