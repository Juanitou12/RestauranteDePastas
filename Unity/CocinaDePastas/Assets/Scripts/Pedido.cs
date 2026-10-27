using UnityEngine;

// Clase de datos pura (no es MonoBehaviour): guarda un pedido y calcula su total
public class Pedido
{
    public string plato { get; private set; }
    public string salsa { get; private set; }
    public int cantidad { get; private set; }
    public double precioUnitario { get; private set; }

    public Pedido(string plato, string salsa, int cantidad)
    {
        this.plato = plato;
        this.salsa = salsa;
        this.cantidad = cantidad;
        this.precioUnitario = Random.Range(10, 26);   // precio aleatorio por plato entre 10 y 25
    }

    public string ObtenerDescripcion()
    {
        return $"{cantidad}x {plato} con Salsa {salsa}";
    }

    public double CalcularTotal()
    {
        return cantidad * precioUnitario;
    }
}
