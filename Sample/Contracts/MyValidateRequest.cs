using System.ComponentModel.DataAnnotations;

namespace Sample.Contracts;


[Validate]
public class MyValidateRequest : IRequest
{
    [Required][Url] public string Url { get; set; }
}