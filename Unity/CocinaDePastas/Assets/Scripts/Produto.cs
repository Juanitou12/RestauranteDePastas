using UnityEngine;

public class Producto
{
    public string Nombre { get; private set; }
    public float PrecioBase { get; private set; }

    public Producto(string nombre, float precioBase)
    {
        Nombre = nombre;
        PrecioBase = precioBase;
    }

    public virtual float CalcularPrecio()
    {
        return PrecioBase;
    }
}