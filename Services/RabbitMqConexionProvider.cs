using CreditoPlataforma.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CreditoPlataforma.Services;

public interface IRabbitMqConexionProvider
{
    bool Habilitado { get; }

    Task<IConnection?> ObtenerConexionAsync(CancellationToken cancellationToken = default);
}

public sealed class RabbitMqConexionProvider(
    IOptions<RabbitMqOptions> opciones,
    ILogger<RabbitMqConexionProvider> logger) : IRabbitMqConexionProvider, IDisposable
{
    private readonly RabbitMqOptions _opciones = opciones.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConnectionFactory _factory = CrearFactory(opciones.Value);
    private IConnection? _conexion;

    public bool Habilitado => !string.IsNullOrWhiteSpace(_opciones.ConnectionString);

    private static ConnectionFactory CrearFactory(RabbitMqOptions opciones)
    {
        var factory = new ConnectionFactory
        {
            ClientProvidedName = "credito-plataforma"
        };

        if (Uri.TryCreate(opciones.ConnectionString, UriKind.Absolute, out var uri))
        {
            factory.Uri = uri;
        }

        return factory;
    }

    public async Task<IConnection?> ObtenerConexionAsync(CancellationToken cancellationToken = default)
    {
        if (!Habilitado)
        {
            return null;
        }

        if (_conexion is { IsOpen: true })
        {
            return _conexion;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_conexion is { IsOpen: true })
            {
                return _conexion;
            }

            if (_conexion is not null)
            {
                try
                {
                    await _conexion.DisposeAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "No se pudo liberar la conexión RabbitMQ anterior.");
                }

                _conexion = null;
            }

            _conexion = await _factory.CreateConnectionAsync(cancellationToken);
            logger.LogInformation("Conexión AMQPS con RabbitMQ establecida ({Cola}).", _opciones.QueueName);
            return _conexion;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo conectar a RabbitMQ/CloudAMQP por AMQPS.");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        try
        {
            _conexion?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al cerrar la conexión RabbitMQ.");
        }
    }
}
