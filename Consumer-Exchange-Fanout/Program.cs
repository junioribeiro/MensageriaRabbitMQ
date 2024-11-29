using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

// declare a server-named 

await channel.QueueDeclareAsync(queue: "fila-log", durable: true, exclusive: false, autoDelete: false);

await channel.QueueBindAsync(queue: "fila-log", exchange: "exchange-logs", routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for logs.");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] {message}");
    return Task.CompletedTask;
};

await channel.BasicConsumeAsync("log", autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();