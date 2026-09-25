using System.Text;
using System.Text.Json;
using CreditoPlataforma.Configuration;
using CreditoPlataforma.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CreditoPlataforma.Services;

public interface INotificacionPublicador
{
    Task<bool> PublicarSolicitudRegistradaAsync(SolicitudRegistrada mensaje, CancellationToken cancellationToken = default);
}

public class RabbitMqNotificacionPublicador(
    IRabbitMqConexionProvider conexion,
    IOptions<RabbitMqOptions> opciones,
    ILogger<RabbitMqNotificacionPublicador> logger) : INotificacionPublicador
{
    private readonly RabbitMqOptions _opciones = opciones.Value;

    public async Task<bool> PublicarSolicitudRegistradaAsync(
        SolicitudRegistrada mensaje,
        CancellationToken cancellationToken = default)
    {
        if (!conexion.Habilitado)
        {
            logger.LogError(
                "RabbitMq:ConnectionString no está configurado. La notificación {MessageId} no pudo encolarse; " +
                "reintenta el envío con el mismo MessageId.",
                mensaje.MessageId);
            return false;
        }

        try
        {
            var connection = await conexion.ObtenerConexionAsync(cancellationToken);
            if (connection is null)
            {
                logger.LogError(
                    "Sin conexión con el broker. La notificación {MessageId} no pudo encolarse; " +
                    "reintenta el envío con el mismo MessageId.",
                    mensaje.MessageId);
                return false;
            }

            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                cancellationToken);

            await channel.QueueDeclareAsync(
                queue: _opciones.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var cuerpo = JsonSerializer.Serialize(mensaje);
            var body = Encoding.UTF8.GetBytes(cuerpo);

            var propiedades = new BasicProperties
            {
                Persistent = true,
                MessageId = mensaje.MessageId,
                ContentType = "application/json",
                Type = typeof(SolicitudRegistrada).Name
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _opciones.QueueName,
                mandatory: true,
                basicProperties: propiedades,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Mensaje {Type} {MessageId} publicado y confirmado por el broker en la cola {Cola}: {Payload}",
                propiedades.Type, mensaje.MessageId, _opciones.QueueName, cuerpo);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falló la publicación de {MessageId} en {Cola}. La solicitud se conserva en SQLite. " +
                "Procedimiento de reenvío: publicar el mismo JSON con el MessageId original " +
                "(la unicidad del MessageId evita duplicados en el consumidor).",
                mensaje.MessageId, _opciones.QueueName);
            return false;
        }
    }
}
