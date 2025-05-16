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
        public async Task<IActionResult> Topic([FromBody] string message, string routingKey)
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            #region Setup
            // variaveis de definição
            string finance = "finance";
            string finance_sp = "finance_sp";
            string finance_sp_1 = "finance_sp_1";
            string finance_sp_2 = "finance_sp_2";
            string finance_rj = "finance_rj";
            string finance_rj_1 = "finance_rj_1";
            string finance_rj_2 = "finance_rj_2";

            string finance_exchange = "finance_exchange";
            //Cria o Exchange caso não exista no servidor
            await channel.ExchangeDeclareAsync(exchange: finance_exchange, type: ExchangeType.Topic);

            //Define o TTL direto na declaração da fila ** Tempo de duração da message na fila **
            var args = new Dictionary<string, object?>()
            {
                {"x-message-ttl", 20000 }
            };

            //cria a fila, caso não exista no servidor
            await channel.QueueDeclareAsync(queue: finance, durable: true, exclusive: false, autoDelete: false, arguments: args);
            await channel.QueueDeclareAsync(queue: finance_sp, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: finance_sp_1, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: finance_sp_2, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: finance_rj, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: finance_rj_1, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: finance_rj_2, durable: true, exclusive: false, autoDelete: false);

            // Binding conecta a fila ao exchange
            await channel.QueueBindAsync(queue: finance, exchange: finance_exchange, routingKey: "finance");
            await channel.QueueBindAsync(queue: finance, exchange: finance_exchange, routingKey: "finance.#");
            await channel.QueueBindAsync(queue: finance_sp, exchange: finance_exchange, routingKey: "finance.sp");
            await channel.QueueBindAsync(queue: finance_sp, exchange: finance_exchange, routingKey: "finance.sp.*");
            await channel.QueueBindAsync(queue: finance_sp_1, exchange: finance_exchange, routingKey: "finance.sp.1");
            await channel.QueueBindAsync(queue: finance_sp_2, exchange: finance_exchange, routingKey: "finance.sp.2");
            await channel.QueueBindAsync(queue: finance_rj, exchange: finance_exchange, routingKey: "finance.rj");
            await channel.QueueBindAsync(queue: finance_rj, exchange: finance_exchange, routingKey: "finance.rj.*");
            await channel.QueueBindAsync(queue: finance_rj_1, exchange: finance_exchange, routingKey: "finance.rj.1");
            await channel.QueueBindAsync(queue: finance_rj_2, exchange: finance_exchange, routingKey: "finance.rj.2");
            #endregion

            #region Setup Message
            var basicProperties = new BasicProperties();
            // Define que a mensagem deve permanecer no servidor após reinicialização ou parada, fila fica lenta, não recomendavel
            basicProperties.Persistent = true;

            var body = Encoding.UTF8.GetBytes(message);
            #endregion

            #region Publish Message
            await channel.BasicPublishAsync(exchange: finance_exchange, routingKey: routingKey, mandatory: true, basicProperties: basicProperties, body: body);
            #endregion

            return Accepted(message);
        }

        [HttpPost]
        [Route("exchange-header")]
        public async Task<IActionResult> Header([FromBody] string message, [FromHeader] string header)
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            #region Setup
            // variaveis de definição
            string contabilidade = "contabilidade";
            string financeiro = "financeiro";
            string logistica = "logistica";

            string finance_exchange = "header_exchange";
            //Cria o Exchange caso não exista no servidor
            await channel.ExchangeDeclareAsync(exchange: finance_exchange, type: ExchangeType.Headers);

            //cria a fila, caso não exista no servidor
            await channel.QueueDeclareAsync(queue: contabilidade, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: financeiro, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueDeclareAsync(queue: logistica, durable: true, exclusive: false, autoDelete: false);

            // Bind fila contabilidade
            await channel.QueueBindAsync(
                queue: contabilidade,
                exchange: finance_exchange,
                routingKey: string.Empty,
                arguments: new Dictionary<string, object?>()
                           {
                               { "setor", "contabilidade" }
                           }
            );
            // Bind fila financeiro
            await channel.QueueBindAsync(
                queue: financeiro,
                exchange: finance_exchange,
                routingKey: string.Empty,
                arguments: new Dictionary<string, object?>()
                           {
                               { "setor", "financeiro" }
                           }
            );
            // Bind fila logistica
            await channel.QueueBindAsync(
                queue: logistica,
                exchange: finance_exchange,
                routingKey: string.Empty,
                arguments: new Dictionary<string, object?>()
                           {
                               { "setor", "logistica" }
                           }
            );

            #endregion

            #region Setup Message
            var body = Encoding.UTF8.GetBytes(message);
            #endregion

            #region Publish Message
            var basicProperties = new BasicProperties();
            // TTL tempo de duração da mensagem na fila em milisegundos
            basicProperties.Expiration = "10000";
            // Headers da mensagem
            basicProperties.Headers = new Dictionary<string, object?>()
                           {
                               { "setor", header }
                           };
            await channel.BasicPublishAsync(exchange: finance_exchange, routingKey: string.Empty, mandatory: true, basicProperties: basicProperties, body);
            #endregion

            return Accepted(message);
        }

        [HttpPost]
        [Route("exchange-deadleter")]
        public async Task<IActionResult> Totalizador(string message)
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            channel.BasicAcksAsync += Channel_BasicAcksAsync;
            channel.BasicNacksAsync += Channel_BasicNacksAsync;
            channel.BasicReturnAsync += Channel_BasicReturnAsync;

            #region Setup
            // variaveis de definição
            string queueName = "Totalizador";

            //Declarando a Estrategia de deadletter
            await channel.ExchangeDeclareAsync(exchange: "DeadLetterExchange", type: ExchangeType.Fanout);
            await channel.QueueDeclareAsync(queue: "DeadLetterQueue", durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queue: "DeadLetterQueue", exchange: "DeadLetterExchange", routingKey: string.Empty);
            var arguments = new Dictionary<string, object?>()
            {
                { "x-dead-letter-exchange","DeadLetterExchange"}
            };

            //cria a fila, caso não exista no servidor
            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
            #endregion

            #region Publicador
            var body = Encoding.UTF8.GetBytes(message);
            // Publica no Exchange padrão
            await channel.BasicPublishAsync(exchange: "", routingKey: "Totalizador", body: body, mandatory: true);
            #endregion

            return Accepted(message);
        }

        private Task Channel_BasicReturnAsync(object sender, RabbitMQ.Client.Events.BasicReturnEventArgs @event)
        {
            // neste caso não encontrou o exchange ou fila declarada para o envio e retornou a messagem
            // deve criar regra de negocios para tratar o caso
            var result = @event.Body.ToArray();
            return Task.CompletedTask;
        }

        private Task Channel_BasicNacksAsync(object sender, RabbitMQ.Client.Events.BasicNackEventArgs @event)
        {
            // neste caso a entrega não foi bem sucedida, deve criar regra de negocio para tratar a mensagem
            var result = @event.DeliveryTag;
            return Task.CompletedTask;
        }

        private Task Channel_BasicAcksAsync(object sender, RabbitMQ.Client.Events.BasicAckEventArgs @event)
        {
            // Confirma se a mensagem foi entregue para o Exchange sem erros
            // neste caso esta conffirmado a entrega da mensagem ao exchange
            var result = @event.DeliveryTag;
            return Task.CompletedTask;
        }
    }
}
