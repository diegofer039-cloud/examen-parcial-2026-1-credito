namespace CreditoPlataforma.Configuration;

public class RabbitMqOptions
{
    public const string Seccion = "RabbitMq";

    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "solicitudes.notificaciones";

    public bool ConsumerEnabled { get; set; } = true;

    public ushort PrefetchCount { get; set; } = 10;
}
