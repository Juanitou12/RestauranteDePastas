using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public List<Producto> ListaPastas { get; private set; }
    public List<Producto> ListaSalsas { get; private set; }

    void Awake()
    {
        ListaPastas = new List<Producto>();
        ListaSalsas = new List<Producto>();
        CargarMenuInicial();
    }

    private void CargarMenuInicial()
    {
        // Pastas
        ListaPastas.Add(new Producto("Spaghetti", 1200f));
        ListaPastas.Add(new Producto("Ravioli", 1500f));
        ListaPastas.Add(new Producto("Macaroni", 1100f));
        ListaPastas.Add(new Producto("Tortellini", 1600f));

        // Salsas
        ListaSalsas.Add(new Producto("Salsa Boloñesa", 500f));
        ListaSalsas.Add(new Producto("Salsa Carbonara", 600f));
        ListaSalsas.Add(new Producto("Salsa Pesto", 450f));
        ListaSalsas.Add(new Producto("Salsa de Setas", 550f));
    }
}