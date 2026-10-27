using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class IngredienteStock
{
    public string nombre;
    public int cantidad;
    public int minimo;
    public int maximo;   // capacidad máxima del depósito para este ingrediente
    public double precioUnitario;   // lo que cuesta comprar 1 unidad al proveedor

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

public class Cocina : MonoBehaviour
{
    [Header("Referencias")]
    public Restaurante restauranteScript;
    public TextMeshProUGUI textoStock;

    [Header("Inventario (editable en el Inspector)")]
    public List<IngredienteStock> stock = new List<IngredienteStock>();

    [Header("Aviso de descuento")]
    public float duracionDescuento = 2f;   // segundos que se ve el "-N" rojo

    public string UltimoMotivoFallo { get; private set; } = "";
    private string avisoProveedor = "";

    private Dictionary<string, int> descuentosRecientes = new Dictionary<string, int>();
    private Dictionary<string, Coroutine> rutinasDescuento = new Dictionary<string, Coroutine>();

    private Dictionary<string, Dictionary<string, int>> recetasPlatos;
    private Dictionary<string, Dictionary<string, int>> recetasSalsas;

    void Awake()
    {
        if (stock.Count == 0) CargarStockInicial();
        CargarRecetas();
        ActualizarTextoStock();
    }

    void CargarStockInicial()
    {
        // nombre, cantidad inicial, mínimo (aviso amarillo), máximo (capacidad), precio por unidad
        stock.Add(new IngredienteStock("Harina", 60, 12, 80, 0.5));
        stock.Add(new IngredienteStock("Huevos", 30, 6, 40, 1.0));
        stock.Add(new IngredienteStock("Tomate", 30, 6, 40, 0.8));
        stock.Add(new IngredienteStock("Carne", 20, 4, 30, 2.0));
        stock.Add(new IngredienteStock("Albahaca", 15, 4, 25, 1.0));
        stock.Add(new IngredienteStock("Queso", 30, 6, 40, 1.5));
        stock.Add(new IngredienteStock("Crema", 15, 4, 25, 1.2));
        stock.Add(new IngredienteStock("Panceta", 12, 3, 20, 1.8));
    }

    void CargarRecetas()
    {
        // Los nombres tienen que ser IGUALES a los de Restaurante.cs
        recetasPlatos = new Dictionary<string, Dictionary<string, int>>();
        recetasPlatos["Spaghetti"] = Receta("Harina", 2, "Huevos", 1);
        recetasPlatos["Ravioli"] = Receta("Harina", 2, "Huevos", 1, "Queso", 1);
        recetasPlatos["Lasagna"] = Receta("Harina", 2, "Carne", 1, "Queso", 1);
        recetasPlatos["Pizza Margherita"] = Receta("Harina", 3, "Queso", 2, "Tomate", 1);
        recetasPlatos["Nnocchi"] = Receta("Harina", 2, "Huevos", 1);

        recetasSalsas = new Dictionary<string, Dictionary<string, int>>();
        recetasSalsas["Boloñesa"] = Receta("Tomate", 2, "Carne", 1);
        recetasSalsas["Pesto"] = Receta("Albahaca", 2, "Queso", 1);
        recetasSalsas["Carbonara"] = Receta("Crema", 1, "Panceta", 1, "Huevos", 1);
        recetasSalsas["Cuatro Quesos"] = Receta("Queso", 3, "Crema", 1);
        recetasSalsas["Filetto"] = Receta("Tomate", 2, "Albahaca", 1);
    }

    // Arma una receta a partir de pares: nombre, cantidad, nombre, cantidad...
    Dictionary<string, int> Receta(string n1, int c1, string n2 = null, int c2 = 0, string n3 = null, int c3 = 0)
    {
        Dictionary<string, int> r = new Dictionary<string, int>();
        r[n1] = c1;
        if (n2 != null) r[n2] = c2;
        if (n3 != null) r[n3] = c3;
        return r;
    }

    IngredienteStock BuscarIngrediente(string nombre)
    {
        foreach (IngredienteStock ing in stock)
        {
            if (ing.nombre == nombre) return ing;
        }
        return null;
    }

    Dictionary<string, int> CalcularNecesario(Pedido pedido)
    {
        Dictionary<string, int> necesario = new Dictionary<string, int>();

        if (recetasPlatos.ContainsKey(pedido.plato))
        {
            SumarReceta(necesario, recetasPlatos[pedido.plato], pedido.cantidad);
        }
        else
        {
            Debug.LogWarning("[Cocina] No hay receta para el plato: " + pedido.plato);
        }

        if (recetasSalsas.ContainsKey(pedido.salsa))
        {
            SumarReceta(necesario, recetasSalsas[pedido.salsa], pedido.cantidad);
        }
        else
        {
            Debug.LogWarning("[Cocina] No hay receta para la salsa: " + pedido.salsa);
        }

        return necesario;
    }

    void SumarReceta(Dictionary<string, int> destino, Dictionary<string, int> receta, int porciones)
    {
        foreach (KeyValuePair<string, int> par in receta)
        {
            int total = par.Value * porciones;
            if (destino.ContainsKey(par.Key)) destino[par.Key] += total;
            else destino[par.Key] = total;
        }
    }

    public bool PrepararPedido(Pedido pedido)
    {
        Dictionary<string, int> necesario = CalcularNecesario(pedido);

        // 1) Verificar que alcance TODO antes de descontar nada
        foreach (KeyValuePair<string, int> par in necesario)
        {
            IngredienteStock ing = BuscarIngrediente(par.Key);
            int disponible = 0;
            if (ing != null) disponible = ing.cantidad;

            if (disponible < par.Value)
            {
                UltimoMotivoFallo = "Falta " + par.Key + ": necesitás " + par.Value + " y quedan " + disponible + ".";
                Debug.LogWarning("[Cocina] " + UltimoMotivoFallo);
                return false;
            }
        }

        // 2) Descontar
        foreach (KeyValuePair<string, int> par in necesario)
        {
            BuscarIngrediente(par.Key).cantidad -= par.Value;
            MostrarDescuento(par.Key, par.Value);
        }

        UltimoMotivoFallo = "";
        ActualizarTextoStock();
        Debug.Log("[Cocina] Recibiendo comanda: " + pedido.ObtenerDescripcion());
        return true;
    }

    public void ActualizarTextoStock()
    {
        if (textoStock == null) return;

        string texto = "<b>=== STOCK ===</b>\n";

        foreach (IngredienteStock ing in stock)
        {
            string color = "#00FF00";
            if (ing.cantidad <= 0) color = "#FF0000";
            else if (ing.cantidad <= ing.minimo) color = "#FFD700";

            texto += ing.nombre + ": <color=" + color + ">" + ing.cantidad + "</color><color=#AAAAAA>/" + ing.maximo + "</color>";

            if (descuentosRecientes.ContainsKey(ing.nombre))
            {
                texto += " <color=#FF3030>-" + descuentosRecientes[ing.nombre] + "</color>";
            }

            texto += "\n";
        }

        if (avisoProveedor != "")
        {
            texto += "\n" + avisoProveedor;
        }

        textoStock.text = texto;
    }

    // Lo usa el Proveedor para sumar mercadería
    public void Reponer(string nombre, int cantidad)
    {
        IngredienteStock ing = BuscarIngrediente(nombre);
        if (ing == null) return;

        ing.cantidad += cantidad;

        // No se puede superar la capacidad del depósito
        if (ing.maximo > 0 && ing.cantidad > ing.maximo)
        {
            ing.cantidad = ing.maximo;
        }

        ActualizarTextoStock();
    }

    // ¿Hay al menos un ingrediente que no esté en su máximo?
    public bool HayEspacioParaReponer()
    {
        foreach (IngredienteStock ing in stock)
        {
            if (ing.cantidad < ing.maximo) return true;
        }
        return false;
    }

    // Línea de aviso al pie del panel de stock (ej: "Proveedor: llega en 3 s")
    public void SetAviso(string aviso)
    {
        avisoProveedor = aviso;
        ActualizarTextoStock();
    }

    void MostrarDescuento(string nombre, int cantidad)
    {
        descuentosRecientes[nombre] = cantidad;

        // Si ya había un aviso de este ingrediente, se reinicia el tiempo
        if (rutinasDescuento.ContainsKey(nombre) && rutinasDescuento[nombre] != null)
        {
            StopCoroutine(rutinasDescuento[nombre]);
        }

        rutinasDescuento[nombre] = StartCoroutine(QuitarDescuento(nombre));
    }

    IEnumerator QuitarDescuento(string nombre)
    {
        yield return new WaitForSeconds(duracionDescuento);

        descuentosRecientes.Remove(nombre);
        rutinasDescuento.Remove(nombre);
        ActualizarTextoStock();
    }

    void OnMouseDown()
    {
        if (restauranteScript != null)
        {
            restauranteScript.ClicEnCocina();
        }
        else
        {
            Debug.LogError("[COCINA] No se asignó RestauranteScript en el Inspector.");
        }
    }
}