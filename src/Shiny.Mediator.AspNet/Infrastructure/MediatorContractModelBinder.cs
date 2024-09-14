using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Shiny.Mediator.AspNet.Infrastructure;

public class MediatorContractModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var type = bindingContext.Model!.GetType();
        if (type.IsValueType)
        {
            var ctor = type.GetConstructors().Single();
            var parameters = ctor.GetParameters();
            foreach (var p in parameters)
            {
                var result = bindingContext.ValueProvider.GetValue(p.Name);
            }
            //Activator.CreateInstance()
        }
        else
        {
            var props = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .ToList();

            foreach (var prop in props)
            {
                var result = bindingContext.ValueProvider.GetValue(prop.Name);
            }
        }
        // bindingContext.ActionContext.RouteData.Values
        return Task.CompletedTask;
    }
}