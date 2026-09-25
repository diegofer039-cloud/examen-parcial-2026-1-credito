# Product

## Register

product

## Users

- **Cliente** (persona demo `cliente1@demo.com`, `cliente2@demo.com`): persona natural que solicita un crédito; entra a "Nueva solicitud", consulta "Mis solicitudes" y espera el estado en vivo (Pendiente → Aprobado/Rechazado) sin recargar.
- **Analista** (`analista@demo.com`): evaluador que trabaja sobre la cola "Panel Analista", decide Aprobar/Rechazar y necesita ver de un vistazo monto, ingresos y la regla 5x antes de decidir.

Contexto: sesión corta, tarea repetitiva, decisión con consecuencia real (otorgar o negar crédito). La pantalla sirve a esa tarea: claridad, densidad controlada y feedback inmediato.

## Product Purpose

Plataforma de evaluación de créditos construida como examen parcial (.NET 10 MVC + Identity + EF + SignalR + Redis + RabbitMQ). Éxito = el cliente comprende el estado de su solicitud al instante y el analista decide correctamente en menos tiempo del que tarda en leer la fila. Los estados en tiempo real (WebSocket) y la cola durable de notificaciones son el producto, no un adorno.

## Brand Personality

Moderno, atrevido, preciso. Voz directa en español (tuteo), verbos de acción ("Aprobar", "Filtrar"), cero jerga bancaria inflada. El contraste y el color saturado aportan la personalidad; la tipografía y la densidad aportan la confianza.

## Anti-references

- Plantilla ASP.NET con Bootstrap azul por defecto: barra blanca, enlaces azules genéricos y tablas sin jerarquía.
- Portales bancarios tradicionales: azul corporativo apagado, tarjetas con sombras fuertes y formularios densos sin foco.
- SaaS dashboard cliché: hero-métrica con degradados, tarjetas idénticas con íconos y eyebrows en versalitas sobre cada sección.

## Design Principles

1. **La tarea manda**: cada elemento justifica su presencia evaluando una solicitud; nada decorativo compite con la tabla o el botón de decisión.
2. **Un solo vocabulario**: mismos botones, campos, badges y estados en cliente y panel del analista; si "Aprobar" se ve de una forma, "Rechazar" se ve de la misma familia.
3. **Contraste alto, no ruido**: texto de cuerpo sobre 4.5:1, color saturado reservado a acciones primarias, estados y selección.
4. **Feedback en vivo**: cambio de estado, aviso de conexión y notificación siempre visibles sin recargar; el movimiento sólo comunica estado.
5. **Densidad con respeto**: filas compactas y tabular-nums para montos, pero objetivos táctiles y foco visible para teclado.

## Accessibility & Inclusion

- WCAG 2.1 AA: contraste ≥4.5:1 en texto, ≥3:1 en texto grande y controles; foco visible con anillo de 2px en todo elemento interactivo.
- Estados nunca comunicados sólo por color: los badges llevan texto (Pendiente/Aprobado/Rechazado) y el aviso en vivo usa `role="alert"`.
- `prefers-reduced-motion: reduce` desactiva transiciones y entradas.
- Navegación completa por teclado; inputs con `<label>` asociado; tablas con encabezados semánticos.
