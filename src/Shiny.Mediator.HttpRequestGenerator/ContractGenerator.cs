using Microsoft.OpenApi.Readers;

namespace Shiny.Mediator.HttpRequestGenerator;

public class ContractGenerator
{
    public void Test(Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        var reader = new OpenApiStreamReader();
        var document = reader.Read(streamReader.BaseStream, out var diagnostic);
        
        foreach (var path in document.Paths)
        {
            // path.Key
            foreach (var op in path.Value.Operations)
            {
                foreach (var p in op.Value.Parameters)
                {

                }
            }
        }

    }
}