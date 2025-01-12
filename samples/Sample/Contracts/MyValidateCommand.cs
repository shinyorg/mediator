using System.ComponentModel.DataAnnotations;
using ICommand = Shiny.Mediator.ICommand;

namespace Sample.Contracts;


[Validate]
public class MyValidateCommand : ICommand
{
    [Required][Url] public string Url { get; set; }
}