using RabbitMQ.Client;

namespace Shiny.Mediator.RabbitMq;


//  https://www.rabbitmq.com/client-libraries/dotnet-api-guide#connecting
public class RabbitMqEventExecutor : IEvent
{
    ConnectionFactory connectionFactory = new();
    IConnection? connection;
    // TODO: channel is NOT thread safe
    
    /*
ConnectionFactory factory = new ConnectionFactory();
       factory.UserName = "username";
       factory.Password = "s3Kre7";
       
       var endpoints = new System.Collections.Generic.List<AmqpTcpEndpoint> {
         new AmqpTcpEndpoint("hostname"),
         new AmqpTcpEndpoint("localhost")
       };
     */

    public async Task Start()
    {
        this.connectionFactory = new ConnectionFactory();
        this.connectionFactory.Uri = new Uri("amqp://user:pass@hostName:port/vhost");
        //factory.ClientProvidedName = "app:audit component:event-consumer";

        this.connection = await this.connectionFactory.CreateConnectionAsync();

        // var channel = await this.connection.CreateChannelAsync()
        // await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct);
        // await channel.QueueDeclareAsync(queueName, false, false, false, null);
        // await channel.QueueBindAsync(queueName, exchangeName, routingKey, null);
        
        // await channel.QueueDeleteAsync("queue-name", false, false);
        
        
        
        /*
        byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes("Hello, world!");
           
       var props = new BasicProperties();
       props.ContentType = "text/plain";
       props.DeliveryMode = 2;
       props.Expiration = "36000000";
       props.Headers = new Dictionary<string, object>();
       props.Headers.Add("latitude",  51.5252949);
       props.Headers.Add("longitude", -0.0905493);
       
       await channel.BasicPublishAsync(exchangeName, routingKey, true, props, messageBodyBytes);
         */
        
        
        /*
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (ch, ea) =>
       {
           var body = ea.Body.ToArray();
           // copy or deserialise the payload
           // and process the message
           // ...
           await channel.BasicAckAsync(ea.DeliveryTag, false);
       };
           // this consumer tag identifies the subscription
           // when it has to be cancelled
           string consumerTag = await channel.BasicConsumeAsync(queueName, false, consumer);
         */
    }
}