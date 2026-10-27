using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// El "director" del juego: coordina clientes, pedidos, cocina, caja y pantalla
public class Restaurante : MonoBehaviour
{
    // Etapas por las que pasa la mesa que se está atendiendo
    enum Estado { SinClientes, Sentados, EsperandoCocina, Cocinando, Listo }

    [Header("Referencias de Scripts y UI")]
    public Cocina cocinaScript;
    public TextMeshProUGUI textoPantalla;   // Estado de las mesas (fijo)
    public TextMeshProUGUI textoMensajes;   // Alertas y ticket
    public TextMeshProUGUI textoDinero;     // Caja del restaurante
    public GameObject fondoMensajes;        // Panel oscuro de los mensajes
    public float tiempoMensaje = 4f;        // Segundos que dura el mensaje

    [Header("Configuración de Mesas")]
    public List<MesaVisual> listaMesas;

    [Header("Configuración de Tiempos")]
    public float tiempoEntreClientesMin = 4f;
    public float tiempoEntreClientesMax = 8f;
    public int tiempoCoccion = 5;           // Segundos que tarda la cocina en preparar

    [Header("Economía")]
    public double dineroInicial = 100;

    private double dinero;
    private Estado estado = Estado.SinClientes;
    private MesaVisual mesaActual;
    private Pedido pedidoActual;
    private int tiempoRestante;
    private Coroutine rutinaMensaje;

    void Start()
    {
        dinero = dineroInicial;
        ActualizarDinero();
        OcultarMensaje();
        ActualizarReporteMesas();
        StartCoroutine(GenerarClientes());
    }

    // ---------- Clientes ----------

    IEnumerator GenerarClientes()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(tiempoEntreClientesMin, tiempoEntreClientesMax));

            if (estado == Estado.SinClientes) LlegadaDeClientes();
        }
    }

    void LlegadaDeClientes()
    {
        List<MesaVisual> libres = listaMesas.FindAll(m => !m.Ocupada);
        if (libres.Count == 0) return;

        mesaActual = libres[Random.Range(0, libres.Count)];
        mesaActual.OcuparMesa();
        estado = Estado.Sentados;

        ActualizarReporteMesas();
        Debug.Log("[RESTAURANTE] Llegaron clientes a una mesa.");
    }

    // ---------- Clics ----------

    public void ClicEnMesa(MesaVisual mesa)
    {
        if (mesa != mesaActual) return;

        switch (estado)
        {
            case Estado.Sentados:
                TomarPedido();
                break;
            case Estado.Cocinando:
                Mensaje("#FFFF00", "ALERTA", "La comida todavía se está cocinando. ¡Esperá un momento!");
                break;
            case Estado.Listo:
                EntregarYCobrar();
                break;
        }
    }

    public void ClicEnCocina()
    {
        switch (estado)
        {
            case Estado.SinClientes:
            case Estado.Sentados:
                Mensaje("#FF0000", "ALERTA", "No hay ningún pedido tomado para cocinar. ¡Tomá la orden primero!");
                return;
            case Estado.Cocinando:
                Mensaje("#FFFF00", "ALERTA", "La cocina ya está preparando este pedido.");
                return;
            case Estado.Listo:
                Mensaje("#FFFF00", "ALERTA", "La comida ya está preparada. ¡Hacé clic en la mesa para entregarla!");
                return;
        }

        // Estado.EsperandoCocina: el stock se descuenta al empezar a cocinar
        if (cocinaScript.PrepararPedido(pedidoActual)) StartCoroutine(Cocinar());
        else Mensaje("#FF0000", "SIN STOCK", cocinaScript.UltimoMotivoFallo);
    }

    // ---------- Flujo del pedido ----------

    void TomarPedido()
    {
        // El plato y la salsa salen del menú de la cocina (así los nombres nunca se desfasan)
        pedidoActual = new Pedido(cocinaScript.PlatoAlAzar(), cocinaScript.SalsaAlAzar(), Random.Range(1, 5));
        estado = Estado.EsperandoCocina;

        ActualizarReporteMesas();
        Debug.Log("[ORDEN TOMADA] Pedido: " + pedidoActual.ObtenerDescripcion());
    }

    IEnumerator Cocinar()
    {
        estado = Estado.Cocinando;

        for (tiempoRestante = tiempoCoccion; tiempoRestante > 0; tiempoRestante--)
        {
            ActualizarReporteMesas();
            yield return new WaitForSeconds(1f);
        }

        estado = Estado.Listo;
        ActualizarReporteMesas();
        Mensaje("#00FF00", "COCINA", "¡El pedido está listo! Llevalo a la mesa.");
    }

    void EntregarYCobrar()
    {
        double total = pedidoActual.CalcularTotal();
        dinero += total;
        ActualizarDinero();

        string ticket = "<b><size=120%>[TICKET FINAL DE PAGO]</size></b>\n";
        ticket += "<b>Plato:</b> " + pedidoActual.ObtenerDescripcion() + "\n";
        ticket += "<b>TOTAL COBRADO:</b> <color=#00FF00>$" + total.ToString("F2") + "</color>\n\n";
        ticket += "<i>¡Mesa cobrada y liberada con éxito!</i>";

        ActualizarMensaje(ticket);
        Debug.Log("[TICKET DE PAGO] Total: $" + total.ToString("F2"));

        mesaActual.LiberarMesa();
        mesaActual = null;
        pedidoActual = null;
        estado = Estado.SinClientes;

        ActualizarReporteMesas();
    }

    // ---------- Economía ----------

    // Devuelve false (y no cobra nada) si no alcanza la plata. Lo usa el Proveedor.
    public bool Gastar(double monto)
    {
        if (dinero < monto) return false;

        dinero -= monto;
        ActualizarDinero();
        return true;
    }

    void ActualizarDinero()
    {
        if (textoDinero != null) textoDinero.text = "Caja: <color=#00FF00>$" + dinero.ToString("F2") + "</color>";
    }

    // ---------- Pantalla ----------

    void ActualizarReporteMesas()
    {
        if (textoPantalla == null) return;

        string reporte = "<b>=== ESTADO DE LAS MESAS ===</b>\n";

        for (int i = 0; i < listaMesas.Count; i++)
        {
            reporte += "<b>[Mesa " + (i + 1) + "]</b> -> ";

            if (listaMesas[i] == mesaActual) reporte += TextoEstado();
            else reporte += "<color=#D0D0D0>Libre / Disponible</color>\n";
        }

        textoPantalla.text = reporte;
    }

    string TextoEstado()
    {
        switch (estado)
        {
            case Estado.Sentados:
                return "<color=#FFD700>CLIENTES SENTADOS</color>\n   <i>-> Toca la mesa para tomar la orden</i>\n";
            case Estado.EsperandoCocina:
                return "<color=#FF8C00>PEDIDO: " + pedidoActual.ObtenerDescripcion() + "</color>\n   <i>-> Toca la COCINA para cocinar</i>\n";
            case Estado.Cocinando:
                return "<color=#FF8C00>COCINANDO: " + pedidoActual.ObtenerDescripcion() + "</color>\n   <i>-> Listo en " + tiempoRestante + " s...</i>\n";
            case Estado.Listo:
                return "<color=#00FF00>COMIDA LISTA PARA ENTREGAR</color>\n   <i>-> Toca la mesa para dar el TICKET y cobrar</i>\n";
            default:
                return "";
        }
    }

    // Mensaje corto con color y etiqueta, ej: [ALERTA] texto
    void Mensaje(string color, string etiqueta, string texto)
    {
        ActualizarMensaje("<color=" + color + "><b>[" + etiqueta + "]</b> " + texto + "</color>");
    }

    // Muestra un mensaje y lo oculta solo pasados unos segundos
    void ActualizarMensaje(string mensaje)
    {
        if (textoMensajes == null) return;

        textoMensajes.text = mensaje;
        if (fondoMensajes != null) fondoMensajes.SetActive(true);

        if (rutinaMensaje != null) StopCoroutine(rutinaMensaje);
        rutinaMensaje = StartCoroutine(OcultarMensajeLuego());
    }

    IEnumerator OcultarMensajeLuego()
    {
        yield return new WaitForSeconds(tiempoMensaje);
        OcultarMensaje();
    }

    void OcultarMensaje()
    {
        if (textoMensajes != null) textoMensajes.text = "";
        if (fondoMensajes != null) fondoMensajes.SetActive(false);
    }
}
