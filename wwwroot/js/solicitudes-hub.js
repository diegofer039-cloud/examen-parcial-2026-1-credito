(function () {
    "use strict";

    if (typeof signalR === "undefined") {
        return;
    }

    var statusEl = document.getElementById("estado-conexion");
    var alertaEl = document.getElementById("alerta-websocket");
    var detalleEl = document.getElementById("detalle-solicitud");

    function setStatus(texto, clase) {
        if (!statusEl) return;
        statusEl.textContent = texto;
        statusEl.className = "badge " + clase;
    }

    function badgeParaEstado(estado) {
        if (estado === "Aprobado") return '<span class="badge bg-success">Aprobado</span>';
        if (estado === "Rechazado") return '<span class="badge bg-danger">Rechazado</span>';
        return '<span class="badge bg-warning text-dark">Pendiente</span>';
    }

    function mostrarAviso(mensaje) {
        if (!alertaEl) return;
        alertaEl.innerHTML =
            '<strong>Actualización en tiempo real:</strong> ' + mensaje +
            ' <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>';
        alertaEl.classList.remove("d-none");
        alertaEl.classList.add("alert-success");
        alertaEl.classList.remove("alert-secondary");
    }

    function actualizarFila(solicitud) {
        var fila = document.querySelector('tr[data-solicitud-id="' + solicitud.solicitudId + '"]');
        if (!fila) return false;
        var celda = fila.querySelector(".celda-estado");
        if (celda) celda.innerHTML = badgeParaEstado(solicitud.estado);
        var motivo = fila.querySelector(".celda-motivo");
        if (motivo) motivo.textContent = solicitud.motivoRechazo || "-";
        return true;
    }

    function actualizarDetalle(solicitud) {
        if (!detalleEl) return false;
        var id = parseInt(detalleEl.getAttribute("data-solicitud-id"), 10);
        if (id !== solicitud.solicitudId) return false;

        var estado = document.getElementById("estado-actual");
        if (estado) estado.innerHTML = badgeParaEstado(solicitud.estado);

        var motivo = document.getElementById("motivo-rechazo");
        if (motivo) motivo.textContent = solicitud.motivoRechazo || "-";

        return true;
    }

    function aplicar(solicitud, origen) {
        actualizarFila(solicitud);
        actualizarDetalle(solicitud);
        mostrarAviso(
            "La solicitud #" + solicitud.solicitudId + " ahora está en estado <strong>" +
            solicitud.estado + "</strong>" +
            (solicitud.motivoRechazo ? " (" + solicitud.motivoRechazo + ")" : "") +
            ". [" + origen + "]"
        );
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/solicitudes", {
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .build();

    connection.on("SolicitudEstadoActualizado", function (solicitud) {
        aplicar(solicitud, "evento");
    });

    connection.onreconnecting(function () {
        setStatus("Reconectando...", "bg-warning text-dark");
    });

    connection.onreconnected(async function () {
        setStatus("Conectado (reconectado)", "bg-success");
        try {
            var estados = await connection.invoke("ObtenerMisEstados");
            estados.forEach(function (s) { actualizarFila(s); actualizarDetalle(s); });
            mostrarAviso("Reconectado: se recuperó el estado vigente desde el servidor (" + estados.length + " solicitudes).");
        } catch (e) {
            console.error("No se pudo recuperar el estado tras la reconexión", e);
        }
    });

    connection.onclose(function () {
        setStatus("Desconectado", "bg-danger");
    });

    connection
        .start()
        .then(function () {
            setStatus("Conectado (WebSocket)", "bg-success");
        })
        .catch(function (err) {
            setStatus("Error de conexión", "bg-danger");
            console.error("No se pudo conectar al hub /hubs/solicitudes", err);
        });

    window.solicitudesHubConnection = connection;
})();
