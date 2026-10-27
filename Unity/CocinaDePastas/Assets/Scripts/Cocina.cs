using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Un ingrediente del depósito
[System.Serializable]
public class IngredienteStock
{
    public string nombre;
    public int cantidad;
    public int minimo;             // por debajo de este valor se muestra en amarillo
    public int maximo;             // capacidad del depósito
    public double precioUnitario;  // lo que cuesta comprar 1 unidad al proveedor

    public IngredienteStock() { }

    public IngredienteStock(string nombre, int cantidad, int minimo, int maximo, double precioUnitario)
    {
        this.nombre = nombre;
        this.cantidad = cantidad;
        this.minimo = minimo;
        this.maximo = maximo;
        this.precioUnitario = precioUnitario;
    }
}

// La cocina: guarda el stock y las recetas, y descuenta ingredientes al preparar un pedido
public class Cocina : MonoBehaviour
{
    [Header("Referencias")]
    public Restaurante restauranteScript;
    public TextMeshProUGUI textoStock;

    [Header("Inventario (se llena solo al dar Play si está vacío)")]
    public List<IngredienteStock> stock = new List<IngredienteStock>();

    [Header("Aviso de descuento")]
    public float duracionDescuento = 2f;   // segundos que se ve el "-N" rojo

    public string UltimoMotivoFallo { get; private set; }

    private string avisoProveedor = "";
    private Dictionary<string, int> descuentos = new Dictionary<string, int>();
    private Coroutine rutinaDescuentos;

    // Receta de cada plato y cada salsa (por porción): ingrediente -> cantidad
    private Dictionary<string, Dictionary<string, int>> recetasPlatos = new Dictionary<string, Dictionary<string, int>>();
    private Dictionary<string, Dictionary<string, int>> recetasSalsas = new Dictionary<string, Dictionary<string, int>>();

    void Awake()
    {
        if (stock.Count == 0) CargarStockInicial();
        CargarRecetas();
        ActualizarTextoStock();
    }

    void CargarStockInicial()
    {
        // nombre, cantidad inicial, mínimo (amarillo), máximo (capacidad), precio por unidad
        stock.Add(new IngredienteStock("Harina", 60, 12, 80, 0.5));
        stock.Add(new IngredienteStock("Huevos", 30, 6, 40, 1.0));
        stock.Add(new IngredienteStock("Tomate", 30, 6, 40, 0.8));
        stock.Add(new IngredienteStock("Carne", 20, 4, 30, 2.0));
        stock.Add(new IngredienteStock("Albahaca", 15, 4, 25, 1.0));
        stock.Add(new IngredienteStock("Queso", 30, 6, 40, 1.5));
        stock.Add(new IngredienteStock("Crema", 15, 4, 25, 1.2));
        stock.Add(new IngredienteStock("Panceta", 12, 3, 20, 1.8));
    }

    // Los platos y salsas del juego salen de estas recetas: para cambiar el menú se edita SOLO acá
    void CargarRecetas()
    {
        recetasPlatos["Spaghetti"] = Receta("Harina", 2, "Huevos", 1);
        recetasPlatos["Ravioli"] = Receta("Harina", 2, "Huevos", 1, "Queso", 1);
        recetasPlatos["Lasagna"] = Receta("Harina", 2, "Carne", 1, "Queso", 1);
        recetasPlatos["Pizza Margherita"] = Receta("Harina", 3, "Queso", 2, "Tomate", 1);
        recetasPlatos["Nnocchi"] = Receta("Harina", 2, "Huevos", 1);

        recetasSalsas["Boloñesa"] = Receta("Tomate", 2, "Carne", 1);
        recetasSalsas["Pesto"] = Receta("Albahaca", 2, "Queso", 1);
        recetasSalsas["Carbonara"] = Receta("Crema", 1, "Panceta", 1, "Huevos", 1);
        recetasSalsas["Cuatro Quesos"] = Receta("Queso", 3, "Crema", 1);
        recetasSalsas["Filetto"] = Receta("Tomate", 2, "Albahaca", 1);
    }

    // Arma una receta a partir de pares: ingrediente, cantidad, ingrediente, cantidad...
    Dictionary<string, int> Receta(string n1, int c1, string n2 = null, int c2 = 0, string n3 = null, int c3 = 0)
    {
        Dictionary<string, int> r = new Dictionary<string, int>();
        r[n1] = c1;
        if (n2 != null) r[n2] = c2;
        if (n3 != null) r[n3] = c3;
        return r;
    }

    // ---------- Menú (lo usa el Restaurante para armar pedidos) ----------

    public string PlatoAlAzar()
    {
        return NombreAlAzar(recetasPlatos);
    }

    public string SalsaAlAzar()
    {
        return NombreAlAzar(recetasSalsas);
    }

    string NombreAlAzar(Dictionary<string, Dictionary<string, int>> recetas)
    {
        List<string> nombres = new List<string>(recetas.Keys);
        return nombres[Random.Range(0, nombres.Count)];
    }

    // ---------- Preparar pedidos ----------

    IngredienteStock Buscar(string nombre)
    {
        return stock.Find(i => i.nombre == nombre);
    }

    // Suma los ingredientes de plato + salsa, multiplicados por la cantidad pedida
    Dictionary<string, int> CalcularNecesario(Pedido pedido)
    {
        Dictionary<string, int> necesario = new Dictionary<string, int>();
        Sumar(necesario, recetasPlatos[pedido.plato], pedido.cantidad);
        Sumar(necesario, recetasSalsas[pedido.salsa], pedido.cantidad);
        return necesario;
    }

    void Sumar(Dictionary<string, int> destino, Dictionary<string, int> receta, int porciones)
    {
        foreach (KeyValuePair<string, int> par in receta)
        {
            if (!destino.ContainsKey(par.Key)) destino[par.Key] = 0;
            destino[par.Key] += par.Value * porciones;
        }
    }

    public bool PrepararPedido(Pedido pedido)
    {
        Dictionary<string, int> necesario = CalcularNecesario(pedido);

        // 1) Verificar que alcance TODO antes de descontar nada
        foreach (KeyValuePair<string, int> par in necesario)
        {
            IngredienteStock ing = Buscar(par.Key);
            int disponible = ing != null ? ing.cantidad : 0;

            if (disponible < par.Value)
            {
                UltimoMotivoFallo = "Falta " + par.Key + ": necesitás " + par.Value + " y quedan " + disponible + ".";
                return false;
            }
        }

        // 2) Descontar y marcar el "-N" rojo al lado de cada ingrediente usado
        descuentos.Clear();
        foreach (KeyValuePair<string, int> par in necesario)
        {
            Buscar(par.Key).cantidad -= par.Value;
            descuentos[par.Key] = par.Value;
        }

        if (rutinaDescuentos != null) StopCoroutine(rutinaDescuentos);
        rutinaDescuentos = StartCoroutine(LimpiarDescuentos());

        ActualizarTextoStock();
        Debug.Log("[Cocina] Recibiendo comanda: " + pedido.ObtenerDescripcion());
        return true;
    }

    IEnumerator LimpiarDescuentos()
    {
        yield return new WaitForSeconds(duracionDescuento);

        descuentos.Clear();
        ActualizarTextoStock();
    }

    // ---------- Lo usa el Proveedor ----------

    public void Reponer(string nombre, int cantidad)
    {
        IngredienteStock ing = Buscar(nombre);
        if (ing == null) return;

        ing.cantidad = Mathf.Min(ing.cantidad + cantidad, ing.maximo);   // no supera la capacidad
        ActualizarTextoStock();
    }

    // Línea de aviso al pie del panel de stock (ej: "Proveedor: llega en 3 s")
    public void SetAviso(string aviso)
    {
        avisoProveedor = aviso;
        ActualizarTextoStock();
    }

    // ---------- Pantalla ----------

    public void ActualizarTextoStock()
    {
        if (textoStock == null) return;

        string texto = "<b>=== STOCK ===</b>\n";

        foreach (IngredienteStock ing in stock)
        {
            // rojo: agotado, amarillo: poco, verde: bien
            string color = ing.cantidad <= 0 ? "#FF0000" : ing.cantidad <= ing.minimo ? "#FFD700" : "#00FF00";

            texto += ing.nombre + ": <color=" + color + ">" + ing.cantidad + "</color>";
            texto += "<color=#AAAAAA>/" + ing.maximo + "</color>";

            if (descuentos.ContainsKey(ing.nombre))
            {
                texto += " <color=#FF3030>-" + descuentos[ing.nombre] + "</color>";
            }

            texto += "\n";
        }

        if (avisoProveedor != "") texto += "\n" + avisoProveedor;

        textoStock.text = texto;
    }

    void OnMouseDown()
    {
        if (restauranteScript != null) restauranteScript.ClicEnCocina();
        else Debug.LogError("[COCINA] No se asignó RestauranteScript en el Inspector.");
    }
}
