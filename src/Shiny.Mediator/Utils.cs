using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator;


public static class Utils
{
    /// <summary>
    /// Fire & Forget task pattern
    /// </summary>
    /// <param name="task"></param>
    /// <param name="onError"></param>
    public static void RunInBackground(this Task task, Action<Exception> onError)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                onError(x.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    /// <summary>
    /// Fire & Forget task pattern that logs errors
    /// </summary>
    /// <param name="task"></param>
    /// <param name="errorLogger"></param>
    public static void RunInBackground(this Task task, ILogger errorLogger)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                errorLogger.LogError(x.Exception, "Fire & Forget trapped error");
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    
    public static IConfigurationSection? GetHandlerSection(
        this IConfiguration config, 
        string module, 
        object request, 
        object? handler
    )
    {
        var moduleCfg = config.GetSection("Mediator:" + module);
        if (!moduleCfg.Exists())
            return null;
        
        var ct = request.GetType();
        if (handler != null)
        {
            var ht = handler.GetType();

            var cfg = moduleCfg.GetHandlerSubSectionOrdered(
                $"{ct.Namespace}.{ct.Name}",
                $"{ht.Namespace}.{ht.Name}",
                $"{ct.Namespace}.*",
                $"{ht.Namespace}.*",
                "*"
            );
            return cfg;
        }

        return null;
        // TODO: sub namespaces
        // ORDER: config, attribute contract, attribute handler
        // config contract order: full type, exact namespace, sub namespace, *
        // config handler order: full type, exact namespace, sub namespace, *
    }


    static IConfigurationSection? GetHandlerSubSectionOrdered(this IConfigurationSection section, params string[] keys)
    {
        IConfigurationSection? result = null;
        var index = 0;

        while (result == null && index != keys.Length)
        {
            var nextKey = keys[index++];
            var tmp = section.GetSection(nextKey);
            if (tmp.Exists())
                result = tmp;
        }
        return result;
    }
    

    public static TAttribute? GetHandlerHandleMethodAttribute<TCommand, TAttribute>(this ICommandHandler<TCommand> handler) 
        where TAttribute : Attribute 
        where TCommand : ICommand
        => GetMethodInfo<ICommandHandler<TCommand>>(x => x.Handle(default!, null!, default!)).GetCustomAttribute<TAttribute>();

    
    public static TAttribute? GetHandlerHandleMethodAttribute<TRequest, TResult, TAttribute>(this IRequestHandler<TRequest, TResult> handler) 
            where TAttribute : Attribute
            where TRequest : IRequest<TResult>
        => GetMethodInfo<IRequestHandler<TRequest, TResult>>(x => x.Handle(default!, null!, default!)).GetCustomAttribute<TAttribute>();
    
    
    public static TAttribute? GetHandlerHandleMethodAttribute<TRequest, TResult, TAttribute>(this IStreamRequestHandler<TRequest, TResult> handler) 
        where TAttribute : Attribute
        where TRequest : IStreamRequest<TResult>
        => GetMethodInfo<IStreamRequestHandler<TRequest, TResult>>(x => x.Handle(default!, null!, default!)).GetCustomAttribute<TAttribute>();
   
   
    public static TAttribute? GetHandlerHandleMethodAttribute<TEvent, TAttribute>(this IEventHandler<TEvent> handler)
        where TEvent : IEvent
        where TAttribute : Attribute
        => GetMethodInfo<IEventHandler<TEvent>>(x => x.Handle(default!, null!, default)).GetCustomAttribute<TAttribute>();
    
    
    static MethodInfo GetMethodInfo<TResult>(Expression<Func<TResult>> expression)
    {
        if (expression.Body is MethodCallExpression methodCall)
            return methodCall.Method;

        throw new ArgumentException("Expression must be a method call.", nameof(expression));
    }
    
    static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
    {
        if (expression.Body is MethodCallExpression methodCall)
            return methodCall.Method;

        throw new ArgumentException("Expression must be a method call.", nameof(expression));
    }
}

/*
 
   using System;
   using System.Linq.Expressions;
   using System.Reflection;
   
   public class MyClass
   {
       public void MyMethod(int value)
       {
           Console.WriteLine($"MyMethod called with: {value}");
       }
   
       public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
       {
           if (expression.Body is MethodCallExpression methodCall)
           {
               return methodCall.Method;
           }
           throw new ArgumentException("Expression must be a method call.", nameof(expression));
       }
   
       public static MethodInfo GetMethodInfo(Expression<Action> expression)
       {
           if (expression.Body is MethodCallExpression methodCall)
           {
               return methodCall.Method;
           }
           throw new ArgumentException("Expression must be a method call.", nameof(expression));
       }
   
       public static MethodInfo GetMethodInfo<TResult>(Expression<Func<TResult>> expression)
       {
           if (expression.Body is MethodCallExpression methodCall)
           {
               return methodCall.Method;
           }
           throw new ArgumentException("Expression must be a method call.", nameof(expression));
       }
   
       public static void Main(string[] args)
       {
           // For an instance method
           var instance = new MyClass();
           MethodInfo methodInfo1 = GetMethodInfo<MyClass>(x => x.MyMethod(default));
           Console.WriteLine($"Method Name (instance): {methodInfo1.Name}");
   
           // For a static method (if you had one)
           // MethodInfo methodInfo2 = GetMethodInfo(() => StaticMethod());
           // Console.WriteLine($"Method Name (static): {methodInfo2.Name}");
       }
   }
 */