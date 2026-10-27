using System;

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
        
        // Genera un precio aleatorio por plato entre 10 y 25
        Random rng = new Random();
        this.precioUnitario = rng.Next(10, 26);
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