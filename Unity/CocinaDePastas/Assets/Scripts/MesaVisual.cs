using UnityEngine;

public class MesaVisual : MonoBehaviour
{
    [Header("Configuración Visual")]
    public GameObject imagenClientes;
    public Restaurante restauranteScript;

    public bool Ocupada { get; private set; } = false;

    public void OcuparMesa()
    {
        Ocupada = true;
        if (imagenClientes != null)
        {
            imagenClientes.SetActive(true);
        }
    }

    public void LiberarMesa()
    {
        Ocupada = false;
        if (imagenClientes != null)
        {
            imagenClientes.SetActive(false);
        }
    }

    void OnMouseDown()
    {
        if (restauranteScript != null)
        {
            restauranteScript.ClicEnMesa(this);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Falta asignar el RestauranteScript en el Inspector.");
        }
    }
}