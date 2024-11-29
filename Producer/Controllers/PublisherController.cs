using Business.Models;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
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
        [HttpPost()]
        [Route("exchange-direct/order/invoice")]
        [
             ProducesResponseType(StatusCodes.Status200OK),
             ProducesResponseType(StatusCodes.Status400BadRequest),
             ProducesResponseType(StatusCodes.Status500InternalServerError)
         ]
        public async Task<IActionResult> Payment(Invoice invoice)
        {
            if (invoice == null) return BadRequest();
            try
            {
                var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                string exchangeName = "exchange-order";
                string bindRoutingKey = "nfe";

                await channel.ExchangeDeclareAsync(exchange: exchangeName, durable: true, type: ExchangeType.Direct);
                var message = JsonSerializer.Serialize(invoice, options: new JsonSerializerOptions { WriteIndented = true });

                var body = Encoding.UTF8.GetBytes(message);
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: bindRoutingKey, body: body);
                return Ok();
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
            ProducesResponseType(StatusCodes.Status200OK),
            ProducesResponseType(StatusCodes.Status400BadRequest),
            ProducesResponseType(StatusCodes.Status500InternalServerError)
        ]
        public async Task<IActionResult> Payment(Payment payment)
        {
            if (payment == null) return BadRequest();
            try
            {
                var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                string exchangeName = "exchange-order";
                string bindRoutingKey = "payment";

                await channel.ExchangeDeclareAsync(exchange: exchangeName, durable: true, type: ExchangeType.Direct);
                var message = JsonSerializer.Serialize(payment, options: new JsonSerializerOptions { WriteIndented = true });

                var body = Encoding.UTF8.GetBytes(message);
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: bindRoutingKey, body: body);
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: 500);
            }

        }

        // POST api/<PublisherController>
        [HttpPost]
        [Route("exchange-fanout")]
        public async Task Log([FromBody] Log log)
        {
            var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: "exchange-logs", type: ExchangeType.Fanout);

            var message = JsonSerializer.Serialize(log, options: new JsonSerializerOptions { WriteIndented = true });

            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "exchange-logs", routingKey: string.Empty, body: body);
        }

        // PUT api/<PublisherController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<PublisherController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
