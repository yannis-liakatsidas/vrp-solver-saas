using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQns.Packages;
public class Component
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string ConnectionPort { get; set; }
    public string ExchangeName { get; set; }
    public string RoutingKey { get; set; }
    public string QueueName { get; set; }

    public Component(string username, string password, string connectionPort, string exchangeName, string routingKey, string queueName)
    {
        Username = username;
        Password = password;
        ConnectionPort = connectionPort;
        ExchangeName = exchangeName;
        RoutingKey = routingKey;
        QueueName = queueName;
    }

    public (IConnection Connection, IModel Channel) Initialize(string clientProvidedName)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://{Username}:{Password}@{ConnectionPort}"),
            ClientProvidedName = clientProvidedName
        };

        IConnection connection = factory.CreateConnection();
        IModel channel = connection.CreateModel();

        channel.ExchangeDeclare(ExchangeName.ToString(), ExchangeType.Direct);

        channel.QueueDeclare(QueueName.ToString(),
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);
        
        channel.QueueBind(QueueName.ToString(), ExchangeName.ToString(), RoutingKey.ToString(), null);
        //var body = Encoding.UTF8.GetBytes(message);
        return (connection, channel);
    }
}
