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

                string exchangeName = "exchange-order";
                string bindRoutingKey = "nfe";

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

                string exchangeName = "exchange-order";
                string bindRoutingKey = "payment";

                // se não existir ele criar o exchange, caso já existe não precisa criar. a fila tambem cria
                await channel.ExchangeDeclareAsync(exchange: exchangeName, durable: true, type: ExchangeType.Direct);
                var message = JsonSerializer.Serialize(payment, options: new JsonSerializerOptions { WriteIndented = true });

                var body = Encoding.UTF8.GetBytes(message);
                // o routingKey diz para o exchange para qual fila vai a mensagem
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: bindRoutingKey, body: body);
                return Accepted(payment);
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: 500);
            }

        }

        [HttpPost]
        [Route("exchange-fanout")]
        public async Task Log([FromBody] Log log)
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // variaveis de definição
            string exchangeName = "exchange-logs";
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout);

            var message = JsonSerializer.Serialize(log, options: new JsonSerializerOptions { WriteIndented = true });
            var body = Encoding.UTF8.GetBytes(message);

            // ExchangeType.Fanout não possui routingKey, entrga pra todas as fila do exchange
            await channel.BasicPublishAsync(exchange: exchangeName, routingKey: string.Empty, body: body);
        }

    }
}
