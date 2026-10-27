using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Proveedor : MonoBehaviour
{
    [Header("Referencias")]
    public Cocina cocinaScript;
    public Restaurante restauranteScript;

    [Header("Configuración del pedido")]
    public int tiempoEntrega = 6;            // segundos hasta que llega la mercadería
    public int unidadesPorIngrediente = 15;  // cuánto se pide de cada ingrediente (si hay lugar)

    private bool enCamino = false;

    void OnMouseDown()
    {
        if (cocinaScript == null || restauranteScript == null)
        {
            Debug.LogError("[PROVEEDOR] Falta asignar la Cocina o el Restaurante en el Inspector.");
            return;
        }

        if (enCamino)
        {
            Debug.Log("[PROVEEDOR] Ya hay un pedido en camino.");
            return;
        }

        // Se arma el pedido: solo lo que entra en el depósito
        Dictionary<string, int> pedido = new Dictionary<string, int>();
        double costo = 0;

        foreach (IngredienteStock ing in cocinaScript.stock)
        {
            int espacio = ing.maximo - ing.cantidad;
            int unidades = Mathf.Min(unidadesPorIngrediente, espacio);

            if (unidades > 0)
            {
                pedido[ing.nombre] = unidades;
                costo += unidades * ing.precioUnitario;
            }
        }

        if (pedido.Count == 0)
        {
            StartCoroutine(AvisoTemporal("<color=#FFD700>El depósito está lleno. No hace falta pedir.</color>"));
            return;
        }

        // Se cobra al hacer el pedido
        if (!restauranteScript.Gastar(costo))
        {
            StartCoroutine(AvisoTemporal("<color=#FF3030>No alcanza la plata. Costo: $" + costo.ToString("F2") + "</color>"));
            return;
        }

        StartCoroutine(Entrega(pedido, costo));
    }

    IEnumerator AvisoTemporal(string aviso)
    {
        cocinaScript.SetAviso(aviso);
        yield return new WaitForSeconds(2.5f);

        // Solo se borra si no empezó un pedido mientras tanto
        if (!enCamino) cocinaScript.SetAviso("");
    }

    IEnumerator Entrega(Dictionary<string, int> pedido, double costo)
    {
        enCamino = true;
        Debug.Log("[PROVEEDOR] Pedido realizado. Costo: $" + costo.ToString("F2"));

        int restante = tiempoEntrega;
        while (restante > 0)
        {
            cocinaScript.SetAviso("<color=#FFD700>Proveedor: llega en " + restante + " s</color> <color=#FF3030>(-$" + costo.ToString("F2") + ")</color>");
            yield return new WaitForSeconds(1f);
            restante--;
        }

        foreach (KeyValuePair<string, int> par in pedido)
        {
            cocinaScript.Reponer(par.Key, par.Value);
        }

        cocinaScript.SetAviso("<color=#00FF00>¡Llegó la mercadería!</color>");
        Debug.Log("[PROVEEDOR] Mercadería entregada.");

        yield return new WaitForSeconds(3f);
        cocinaScript.SetAviso("");
        enCamino = false;
    }
}