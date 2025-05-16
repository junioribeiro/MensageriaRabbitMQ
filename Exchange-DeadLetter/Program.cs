using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

#region Setup
// variaveis de definição
string task_queue = "Totalizador";


//Declarando a Estrategia de deadletter
await channel.ExchangeDeclareAsync(exchange: "DeadLetterExchange", type: ExchangeType.Fanout);
await channel.QueueDeclareAsync(queue: "DeadLetterQueue", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync(queue: "DeadLetterQueue", exchange: "DeadLetterExchange", routingKey: string.Empty);
var arguments = new Dictionary<string, object?>()
{
    { "x-dead-letter-exchange","DeadLetterExchange"}
};

//cria a fila, caso não exista no servidor
await channel.QueueDeclareAsync(queue: task_queue, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
await channel.BasicQosAsync(0, 1, false);

#endregion


Console.WriteLine(" [*] Waiting for logs.");

#region Setup Consumer
// Worker real
var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (model, ea) =>
{
    try
    {
        byte[] body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var total = int.Parse(message);
        Console.WriteLine($" [x] {total}");
        await channel.BasicAckAsync(ea.DeliveryTag, false);
    }
    catch (Exception)
    {
        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
    }
    await Task.CompletedTask;
};
#endregion

await channel.BasicConsumeAsync(task_queue, autoAck: false, consumer: consumer);


Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();