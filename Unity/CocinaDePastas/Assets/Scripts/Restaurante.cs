using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Restaurante : MonoBehaviour
{
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
    private MesaVisual mesaActualConClientes = null;
    private Pedido pedidoActual = null;
    private bool pedidoCocinado = false;
    private bool cocinando = false;
    private int tiempoRestante = 0;
    private Coroutine rutinaMensaje;

    private string[] listaPlatos = { "Spaghetti", "Ravioli", "Lasagna", "Pizza Margherita", "Nnocchi" };
    private string[] listaSalsas = { "Boloñesa", "Pesto", "Carbonara", "Cuatro Quesos", "Filetto" };

    void Start()
    {
        dinero = dineroInicial;
        ActualizarDinero();

        if (fondoMensajes != null) fondoMensajes.SetActive(false);
        if (textoMensajes != null) textoMensajes.text = "";

        ActualizarReporteMesas();
        StartCoroutine(GenerarClientesPeriodicamente());
    }

    // ---------- Economía ----------

    public double Dinero
    {
        get { return dinero; }
    }

    // Devuelve false (y no cobra nada) si no alcanza la plata
    public bool Gastar(double monto)
    {
        if (dinero < monto) return false;

        dinero -= monto;
        ActualizarDinero();
        return true;
    }

    void ActualizarDinero()
    {
        if (textoDinero == null) return;

        textoDinero.text = "Caja: <color=#00FF00>$" + dinero.ToString("F2") + "</color>";
    }

    // ---------- Clientes ----------

    IEnumerator GenerarClientesPeriodicamente()
    {
        while (true)
        {
            float tiempoEspera = Random.Range(tiempoEntreClientesMin, tiempoEntreClientesMax);
            yield return new WaitForSeconds(tiempoEspera);

            if (mesaActualConClientes == null)
            {
                LlegadaDeClientes();
            }
        }
    }

    void LlegadaDeClientes()
    {
        List<MesaVisual> mesasLibres = new List<MesaVisual>();
        foreach (var mesa in listaMesas)
        {
            if (!mesa.Ocupada)
            {
                mesasLibres.Add(mesa);
            }
        }

        if (mesasLibres.Count > 0)
        {
            int indiceAzar = Random.Range(0, mesasLibres.Count);
            mesaActualConClientes = mesasLibres[indiceAzar];
            mesaActualConClientes.OcuparMesa();

            ActualizarReporteMesas();
            Debug.Log("[RESTAURANTE] Llegaron clientes a una mesa.");
        }
    }

    public void ClicEnMesa(MesaVisual mesaClickeada)
    {
        if (mesaClickeada == mesaActualConClientes && pedidoActual == null)
        {
            TomarPedido();
        }
        else if (mesaClickeada == mesaActualConClientes && cocinando)
        {
            ActualizarMensaje("<color=#FFFF00><b>[ALERTA]</b> La comida todavía se está cocinando. ¡Esperá un momento!</color>");
        }
        else if (mesaClickeada == mesaActualConClientes && pedidoCocinado)
        {
            EntregarYCobrar();
        }
        else
        {
            ActualizarReporteMesas();
        }
    }

    void TomarPedido()
    {
        string plato = listaPlatos[Random.Range(0, listaPlatos.Length)];
        string salsa = listaSalsas[Random.Range(0, listaSalsas.Length)];
        int cantidad = Random.Range(1, 5);

        pedidoActual = new Pedido(plato, salsa, cantidad);
        pedidoCocinado = false;
        cocinando = false;

        ActualizarReporteMesas();
        Debug.Log("[ORDEN TOMADA] Pedido: " + pedidoActual.ObtenerDescripcion());
    }

    public void ClicEnCocina()
    {
        if (pedidoActual == null)
        {
            ActualizarMensaje("<color=#FF0000><b>[ALERTA]</b> No hay ningún pedido tomado para cocinar. ¡Tomá la orden primero!</color>");
            return;
        }

        if (cocinando)
        {
            ActualizarMensaje("<color=#FFFF00><b>[ALERTA]</b> La cocina ya está preparando este pedido.</color>");
            return;
        }

        if (pedidoCocinado)
        {
            ActualizarMensaje("<color=#FFFF00><b>[ALERTA]</b> La comida ya está preparada. ¡Hacé clic en la mesa para entregarla!</color>");
            return;
        }

        if (cocinaScript != null)
        {
            // El stock se descuenta al empezar a cocinar
            bool exito = cocinaScript.PrepararPedido(pedidoActual);
            if (exito)
            {
                StartCoroutine(Cocinar());
            }
            else
            {
                ActualizarMensaje("<color=#FF0000><b>[SIN STOCK]</b> " + cocinaScript.UltimoMotivoFallo + "</color>");
            }
        }
    }

    IEnumerator Cocinar()
    {
        cocinando = true;
        tiempoRestante = tiempoCoccion;
        Debug.Log("[COCINA] Empezó la preparación.");

        while (tiempoRestante > 0)
        {
            ActualizarReporteMesas();
            yield return new WaitForSeconds(1f);
            tiempoRestante--;
        }

        cocinando = false;
        pedidoCocinado = true;
        ActualizarReporteMesas();
        ActualizarMensaje("<color=#00FF00><b>[COCINA]</b> ¡El pedido está listo! Llevalo a la mesa.</color>");
        Debug.Log("[COCINA] Pedido preparado exitosamente.");
    }

    void EntregarYCobrar()
    {
        double totalACobrar = pedidoActual.CalcularTotal();

        dinero += totalACobrar;
        ActualizarDinero();

        string mensajePago = "<b><size=120%>[TICKET FINAL DE PAGO]</size></b>\n" +
                             "<b>Plato:</b> " + pedidoActual.ObtenerDescripcion() + "\n" +
                             "<b>TOTAL COBRADO:</b> <color=#00FF00>$" + totalACobrar.ToString("F2") + "</color>\n\n" +
                             "<i>¡Mesa cobrada y liberada con éxito!</i>";

        ActualizarMensaje(mensajePago);
        Debug.Log("[TICKET DE PAGO] Total: $" + totalACobrar.ToString("F2"));

        if (mesaActualConClientes != null)
        {
            mesaActualConClientes.LiberarMesa();
            mesaActualConClientes = null;
        }

        pedidoActual = null;
        pedidoCocinado = false;
        cocinando = false;

        ActualizarReporteMesas();
    }

    // ---------- Interfaz ----------

    public void ActualizarReporteMesas()
    {
        string reporte = "<b>=== ESTADO DE LAS MESAS ===</b>\n";

        for (int i = 0; i < listaMesas.Count; i++)
        {
            MesaVisual mesa = listaMesas[i];
            string numMesa = "<b>[Mesa " + (i + 1) + "]</b>";

            if (!mesa.Ocupada)
            {
                reporte += numMesa + " -> <color=#D0D0D0>Libre / Disponible</color>\n";
            }
            else if (mesa == mesaActualConClientes)
            {
                if (pedidoActual == null)
                {
                    reporte += numMesa + " -> <color=#FFD700>CLIENTES SENTADOS</color>\n   <i>-> Toca la mesa para tomar la orden</i>\n";
                }
                else if (cocinando)
                {
                    reporte += numMesa + " -> <color=#FF8C00>COCINANDO: " + pedidoActual.ObtenerDescripcion() + "</color>\n   <i>-> Listo en " + tiempoRestante + " s...</i>\n";
                }
                else if (!pedidoCocinado)
                {
                    reporte += numMesa + " -> <color=#FF8C00>PEDIDO: " + pedidoActual.ObtenerDescripcion() + "</color>\n   <i>-> Toca la COCINA para cocinar</i>\n";
                }
                else
                {
                    reporte += numMesa + " -> <color=#00FF00>COMIDA LISTA PARA ENTREGAR</color>\n   <i>-> Toca la mesa para dar el TICKET y cobrar</i>\n";
                }
            }
        }

        if (textoPantalla != null)
        {
            textoPantalla.text = reporte;
        }
    }

    private void ActualizarMensaje(string mensaje)
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

        textoMensajes.text = "";
        if (fondoMensajes != null) fondoMensajes.SetActive(false);
        rutinaMensaje = null;
    }
}