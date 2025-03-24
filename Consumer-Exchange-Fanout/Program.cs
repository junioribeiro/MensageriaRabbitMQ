using Business.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// variaveis de definição
string queueA = "fila-A";
string exchangeName = "exchange-logs";

//Cria o exchange caso não exista.
//ExchangeType.Fanout não existe routingKey
await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout);

// Cria a fila Temporaria, sera excluida depois de desconecta do servidor
// ** QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
// ** queueName = queueDeclareResult.QueueName;

//cria a fila, caso não exista no servidor
await channel.QueueDeclareAsync(queue: queueA, durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync(queue: queueA, exchange: exchangeName, routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for logs.");

// Worker real
var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (model, ea) =>
{
    try
    {
        byte[] body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Log log = JsonSerializer.Deserialize<Log>(message)!;
        Console.WriteLine($" [x] {message}");
        await channel.BasicAckAsync(ea.DeliveryTag, false);
    }
    catch (Exception)
    {
        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
    }
    await Task.CompletedTask;
};

await channel.BasicConsumeAsync(queueA, autoAck: false, consumer: consumer);


Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();