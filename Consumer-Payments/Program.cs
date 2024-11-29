using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// configura os dados de servidor
var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };

// cria um conexão com o servidor
using var connection = await factory.CreateConnectionAsync();

// cria um canal com o servidor
using var channel = await connection.CreateChannelAsync();

// variaveis de definição
string queueName = "fila-payments";
string exchangeName = "exchange-order";
string bindRoutingKey = "payment";

// declara o exchange caso não exista
await channel.ExchangeDeclareAsync(exchange: exchangeName, durable: true, type: ExchangeType.Direct);

//declara a fila
await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

// cria um bind entre a fila e o exchange nomeando uma Routing-key como payment
await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: bindRoutingKey);

Console.WriteLine(" [*] Waiting for logs.");

// Criando o consumer para ficar excuando o envento
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] {message}");
    return Task.CompletedTask;
};

// informa ao exchange que deu tudo certo no consumo da mensagem
await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
