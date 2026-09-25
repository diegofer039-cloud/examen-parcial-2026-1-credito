using System.Text;
using System.Text.Json;
using CreditoPlataforma.Configuration;
using CreditoPlataforma.Data;
using CreditoPlataforma.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CreditoPlataforma.Services;

public class RabbitMqNotificacionConsumidor(
    IRabbitMqConexionProvider conexion,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> opciones,
    ILogger<RabbitMqNotificacionConsumidor> logger) : BackgroundService
{
    private const int EsperaReconexionSegundos = 5;

    private readonly RabbitMqOptions _opciones = opciones.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!conexion.Habilitado)
        {
            logger.LogWarning(
                "RabbitMq:ConnectionString no está configurado; el consumidor queda inactivo. " +
                "Cola esperada: {Cola}.",
                _opciones.QueueName);
            return;
        }

        if (!_opciones.ConsumerEnabled)
        {
            logger.LogWarning(
                "Consumidor deshabilitado (RabbitMq__ConsumerEnabled=false). Los mensajes quedan pendientes en la cola {Cola}.",
                _opciones.QueueName);
            return;
        }

        logger.LogInformation(
            "Consumidor de RabbitMQ iniciado. Cola: {Cola}, prefijo de calidad de servicio: {Prefetch}.",
            _opciones.QueueName, _opciones.PrefetchCount);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumirCicloAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ciclo del consumidor RabbitMQ terminado; se reintenta en {Segundos}s.", EsperaReconexionSegundos);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(EsperaReconexionSegundos), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ConsumirCicloAsync(CancellationToken stoppingToken)
    {
        var connection = await conexion.ObtenerConexionAsync(stoppingToken);
        if (connection is null)
        {
            await Task.Delay(TimeSpan.FromSeconds(EsperaReconexionSegundos), stoppingToken);
            return;
        }

        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _opciones.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _opciones.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumidor = new AsyncEventingBasicConsumer(channel);
        consumidor.ReceivedAsync += (_, args) => ProcesarMensajeAsync(channel, args, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: _opciones.QueueName,
            autoAck: false,
            consumer: consumidor,
            cancellationToken: stoppingToken);

        logger.LogInformation("Escuchando la cola {Cola} con ACK manual...", _opciones.QueueName);

        while (!stoppingToken.IsCancellationRequested && channel.IsOpen && connection.IsOpen)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        logger.LogWarning("Se perdió la conexión con la cola {Cola}; se restablece el consumidor.", _opciones.QueueName);
    }

    private async Task ProcesarMensajeAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken stoppingToken)
    {
        var deliveryTag = args.DeliveryTag;
        var contenido = Encoding.UTF8.GetString(args.Body.ToArray());
        var messageIdPropiedades = args.BasicProperties?.MessageId;

        SolicitudRegistrada? mensaje = null;
        try
        {
            mensaje = JsonSerializer.Deserialize<SolicitudRegistrada>(contenido);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Mensaje inválido (JSON ilegible) rechazado sin reencolar. DeliveryTag={Tag}. Cuerpo: {Cuerpo}", deliveryTag, contenido);
        }

        if (mensaje is null
            || string.IsNullOrWhiteSpace(mensaje.MessageId)
            || mensaje.SolicitudId <= 0
            || string.IsNullOrWhiteSpace(mensaje.UsuarioId))
        {
            logger.LogError(
                "Mensaje inválido rechazado sin reencolar (evidencia en log). DeliveryTag={Tag}, MessageId={MessageId}, Cuerpo: {Cuerpo}",
                deliveryTag, mensaje?.MessageId ?? messageIdPropiedades, contenido);
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var yaProcesada = await context.Notificaciones
                .AnyAsync(n => n.MessageId == mensaje.MessageId, stoppingToken);

            if (yaProcesada)
            {
                logger.LogWarning(
                    "Redelivery detectado: {MessageId} ya estaba procesada. Se confirma sin insertar duplicado.",
                    mensaje.MessageId);
                await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                return;
            }

            var notificacion = new Notificacion
            {
                MessageId = mensaje.MessageId,
                SolicitudId = mensaje.SolicitudId,
                UsuarioId = mensaje.UsuarioId,
                Texto = "Recibimos tu solicitud de crédito y está pendiente de evaluación",
                FechaProcesamientoUtc = DateTime.UtcNow
            };

            context.Notificaciones.Add(notificacion);
            await context.SaveChangesAsync(stoppingToken);

            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);

            logger.LogInformation(
                "Notificación {Id} guardada y confirmada (ACK manual) para MessageId {MessageId}, solicitud {SolicitudId}.",
                notificacion.Id, mensaje.MessageId, mensaje.SolicitudId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Fallo al procesar MessageId {MessageId}: no se confirma el mensaje y no se reencola. " +
                "Reenviar manualmente desde CloudAMQP con el mismo MessageId.",
                mensaje.MessageId);

            try
            {
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
            catch (Exception nackEx)
            {
                logger.LogError(nackEx, "No se pudo rechazar el mensaje {MessageId} tras el fallo de procesamiento.", mensaje.MessageId);
            }
        }
    }
}
