using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// variaveis de definição
string queueName = "fila-log";
string exchangeName = "exchange-logs";

//Cria o exchange caso não exista.
//ExchangeType.Fanout não existe routingKey
await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout);

// Cria a fila caso não exista
QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
queueName = queueDeclareResult.QueueName;
await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: string.Empty);



Console.WriteLine(" [*] Waiting for logs.");

// Worker real
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] {message}");
    return Task.CompletedTask;
};

await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();