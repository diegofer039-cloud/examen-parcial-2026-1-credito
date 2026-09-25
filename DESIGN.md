# Design System — Plataforma de Créditos (WebSocket + Cloud MQ)

Registro: **product** | Personalidad: **Moderno y atrevido**

## Principios
1. **Contraste AA**: todos los textos sobre fondos de color cumplen WCAG 2.1 AA (≥4.5:1).
2. **Foco visible**: anillo de 2px en todo elemento interactivo al recibir foco (`outline` rosa).
3. **Movimiento mínimo**: transiciones 160ms; todo desactivable con `prefers-reduced-motion`.
4. **Estado con color + ícono/texto**: nunca color como único canal de información.
5. **Escala rem fija**: tipografía y espaciado en unidades `rem` (base 16px).

## Tokens (OKLCH en `wwwroot/css/site.css`)
| Token | Valor | Uso |
| --- | --- | --- |
| `--graphite` | `oklch(0.21 0.01 300)` | Navbar y footer (superficie oscura) |
| `--on-graphite` | `oklch(0.96 0.01 100)` | Texto sobre grafito |
| `--primary` | `oklch(0.5 0.21 343)` | Acento rosa: acciones primarias, enlaces, badges |
| `--primary-soft` | `oklch(0.95 0.03 343)` | Fondos tintados de acento |
| `--lira` | `oklch(0.16 0.02 300)` | Texto principal sobre superficie clara |
| `--surface` | `oklch(0.97 0.005 100)` | Paneles/filtros sobre blanco puro |

Estados semánticos: `--success`, `--warning`, `--danger`, `--info` (cada uno con variante `-soft` para fondos tintados y `-on` para texto con contraste AA).

## Componentes clave
- **Navbar grafito** con `.brand-mark` rosa, enlaces claros con hover y fondo activo.
- **Botones** con todos los estados (`:hover`, `:active`, `:focus-visible`, `disabled`) y variantes `outline` en rosa/rojo (reemplazan el azul Bootstrap).
- **Tablas**: thead sobre superficie, filas con `tabular-nums`, hover rosa suave.
- **Badges de estado** tintados con punto indicador (`::before`), p. ej. pendiente/activo/conexión.
- **Alertas** con entrada animada (`aviso-entrada`) y variantes de color semántico.
- **Flujo de 3 pasos** numerados en la Home (círculos con fondo rosa).
- **Formularios**: labels siempre visibles, focus ring en inputs, mensajes de validación en rojo con contraste AA.

## Anti-referencias
- Sin degradados, sin glassmorphism (blur/frosted), sin eyebrows en versalitas.
- Sin azul por defecto de Bootstrap: todo botón/link/acento se sobrescribe con la paleta propia.
- Sin plantilla genérica: se eliminaron los estilos de `_Layout.cshtml.css` (fusionados en `site.css`) y el hero de plantilla fue reemplazado por contenido propio del producto.

## Accesibilidad
- Foco visible siempre; `:focus-visible` no oculta navegación por teclado.
- `prefers-reduced-motion` desactiva animaciones (alertas, transiciones).
- Contraste ≥4.5:1 en textos; estados con texto/ícono además de color.
- SCSS/RTL no requerido; HTML semántico con `aria` en componentes con rol.