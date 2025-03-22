using Business.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

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

#region Criação do Exchange, Queue e o Bind somente uma vez
// Cria o exchange caso não exista no servidor
// exchange é quem recebe a mensagem do produtor
await channel.ExchangeDeclareAsync(exchange: exchangeName, durable: true, type: ExchangeType.Direct);

//cria a fila, caso não exista no servidor
await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

// cria um bind entre a fila e o Exchange nomeando uma Routing-key como payment
// QueueBindAsync diz para Exchange qual fila pertence a routingKey
// routingKey e o nome desta conexão do exchange com a fila
await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: bindRoutingKey);
#endregion


Console.WriteLine(" [*] Waiting for logs.");

// Criando o consumer para ficar excuando o envento
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
	try
	{
        byte[] body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Payment payment = JsonSerializer.Deserialize<Payment>(message)!;
        Console.WriteLine($" [x] {message}");
        channel.BasicAckAsync(ea.DeliveryTag,false);
        return Task.CompletedTask;
    }
	catch (Exception)
	{
        channel.BasicNackAsync(ea.DeliveryTag, false, true);		
	}
  
};

// informa ao exchange que deu tudo certo no consumo da mensagem
await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
