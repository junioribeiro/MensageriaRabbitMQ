using Business.Models;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Producer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublisherController : ControllerBase
    {
        private readonly ConnectionFactory factory;
        public PublisherController(IOptions<ConnectionFactorySetting> connectionFactorySetting)
        {
            factory = new ConnectionFactory
            {
                HostName = connectionFactorySetting.Value.HostName,
                Port = connectionFactorySetting.Value.Port,
                UserName = connectionFactorySetting.Value.UserName,
                Password = connectionFactorySetting.Value.Password
            };
        }
        [HttpPost()]
        [Route("exchange-direct/order/invoice")]
        [
             ProducesResponseType(StatusCodes.Status202Accepted),
             ProducesResponseType(StatusCodes.Status400BadRequest),
             ProducesResponseType(StatusCodes.Status500InternalServerError)
         ]
        public async Task<IActionResult> Invoice(Invoice invoice)
        {
            if (invoice == null) return BadRequest();
            try
            {
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                #region Criação da Fila caso não tenha declarado
                // variaveis de definição
                string queueName = "fila-nfe";
                string exchangeName = "exchange-order";
                string bindRoutingKey = "nfe";
                //cria a fila, caso não exista no servidor
                await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);
                #endregion

                await channel.ExchangeDeclareAsync(exchange: exchangeName, durable: true, type: ExchangeType.Direct);
                var message = JsonSerializer.Serialize(invoice, options: new JsonSerializerOptions { WriteIndented = true });

                var body = Encoding.UTF8.GetBytes(message);
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: bindRoutingKey, body: body);
                return Accepted(invoice);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: 500);
            }

        }

        // GET api/<PublisherController>/5
        [HttpPost()]
        [Route("exchange-direct/order/payment")]
        [
            ProducesResponseType(StatusCodes.Status202Accepted),
            ProducesResponseType(StatusCodes.Status400BadRequest),
            ProducesResponseType(StatusCodes.Status500InternalServerError)
        ]
        public async Task<IActionResult> Payment(Payment payment)
        {
            if (payment == null) return BadRequest();
            try
            {
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                #region Setup
                // variaveis de definição
                string queueName = "fila-payments";
                string queueAudit = "fila-audit";
                string exchangeName = "exchange-order";
                string bindRoutingKey = "payment";

                // pode mandar um rota pra 2 filas
                // pode mandar 2 rotas pra uma fila

                // se não existir ele criar o exchange, caso já existe não precisa criar. a fila tambem cria
                await channel.ExchangeDeclareAsync(exchange: exchangeName, durable: true, type: ExchangeType.Direct);

                //cria a fila, caso não exista no servidor
                await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);
                await channel.QueueDeclareAsync(queue: queueAudit, durable: true, exclusive: false, autoDelete: false);
                // Binding conecta a fila ao exchange
                await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: bindRoutingKey);
                await channel.QueueBindAsync(queue: queueAudit, exchange: exchangeName, routingKey: bindRoutingKey);
                #endregion

                #region Setup Message
                var message = JsonSerializer.Serialize(payment, options: new JsonSerializerOptions { WriteIndented = true });
                var body = Encoding.UTF8.GetBytes(message);
                #endregion

                #region Publish Message
                // o routingKey diz para o exchange para qual fila vai a mensagem com o bind
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: bindRoutingKey, body: body);
                #endregion

                return Accepted(payment);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: 500);
            }
        }

        [HttpPost]
        [Route("exchange-fanout")]
        public async Task<IActionResult> Log([FromBody] Log log)
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            #region Setup
            // variaveis de definição
            string queueA = "fila-A";
            string queueB = "fila-B";
            string queueC = "fila-C";
            string exchangeName = "exchange-logs";

            //Cria o Exchange caso não exista no servidor
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout);
            //cria a fila, caso não exista no servidor
            await channel.QueueDeclareAsync(queue: queueA, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: queueB, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: queueC, durable: true, exclusive: false, autoDelete: false);
            // Binding conecta a fila ao exchange
            await channel.QueueBindAsync(queue: queueA, exchange: exchangeName, routingKey: string.Empty);
            await channel.QueueBindAsync(queue: queueB, exchange: exchangeName, routingKey: string.Empty);
            await channel.QueueBindAsync(queue: queueC, exchange: exchangeName, routingKey: string.Empty);
            #endregion

            #region Setup Message
            var message = JsonSerializer.Serialize(log, options: new JsonSerializerOptions { WriteIndented = true });
            var body = Encoding.UTF8.GetBytes(message);
            #endregion

            #region Publish Message
            // ExchangeType.Fanout não possui routingKey, entrega pra todas as fila com Bind no exchange
            await channel.BasicPublishAsync(exchange: exchangeName, routingKey: string.Empty, body: body);
            #endregion

            return Accepted(log);
        }

        [HttpPost]
        [Route("exchange-topic")]
        public async Task<IActionResult> Log([FromBody] string message)
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            #region Criação da Fila caso não tenha declarado
            // variaveis de definição
            string queueName = "fila-nfe";
            string exchangeName = "exchange-order";
            string bindRoutingKey = "nfe";
            //cria a fila, caso não exista no servidor
            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);
            #endregion

            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic);

            //var message = JsonSerializer.Serialize(message, options: new JsonSerializerOptions { WriteIndented = true });
            var body = Encoding.UTF8.GetBytes(message);

            // ExchangeType.Fanout não possui routingKey, entrega pra todas as fila com Bind no exchange
            await channel.BasicPublishAsync(exchange: exchangeName, routingKey: bindRoutingKey, body: body);
            return Accepted(message);
        }

        [HttpPost]
        [Route("exchange-deadleter")]
        public async Task<IActionResult> Totalizador(string message)
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            #region Setup
            // variaveis de definição
            string queueName = "Totalizador";

            //Declarando a Estrategia de deadletter
            await channel.ExchangeDeclareAsync(exchange: "DeadLetterExchange", type: ExchangeType.Fanout);
            await channel.QueueDeclareAsync(queue: "DeadLetterQueue", durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queue: "DeadLetterQueue", exchange: "DeadLetterExchange", routingKey: string.Empty);
            var arguments = new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange","DeadLetterExchange"}
            };
            
            //cria a fila, caso não exista no servidor
            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
            #endregion

            #region Publicador
            var body = Encoding.UTF8.GetBytes(message);
            // Publica no Exchange padrão
            await channel.BasicPublishAsync(exchange: "", routingKey: "Totalizador", body: body);
            #endregion

            return Accepted(message);
        }

    }
}
