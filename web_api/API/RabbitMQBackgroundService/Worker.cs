// File: Worker.cs
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private IConnection _connection;
    private IModel _channel;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        InitializeRabbitMQListener();
    }

    private void InitializeRabbitMQListener()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "hello",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        _logger.LogInformation("RabbitMQ listener initialized.");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _logger.LogInformation("Stopping RabbitMQ listener..."));

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"Received message: {message}");

            var order = System.Text.Json.JsonSerializer.Deserialize<Product>(message);
            SaveToRedis(order);

        };
            return Task.CompletedTask;


        _channel.BasicConsume(queue: "hello",
                             autoAck: true,
                             consumer: consumer);

        return Task.CompletedTask;
    }
    private void SaveToRedis(Product order)
    {
        // ≈‰‘«¡ « ’«· „⁄ Redis
        var redis = ConnectionMultiplexer.Connect("localhost");
        var db = redis.GetDatabase();

        //  Œ“Ì‰ «·ﬂ«∆‰ ›Ì Redis »«” Œœ«„ „⁄—› «·„‰ Ã ﬂ«·„› «Õ
        db.StringSet(order.Name, System.Text.Json.JsonSerializer.Serialize(order));
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }

    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
    }
}
