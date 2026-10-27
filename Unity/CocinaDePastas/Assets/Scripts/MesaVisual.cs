using UnityEngine;

// Una mesa: sabe si está ocupada y avisa al Restaurante cuando le hacen clic
public class MesaVisual : MonoBehaviour
{
    [Header("Configuración Visual")]
    public GameObject imagenClientes;
    public Restaurante restauranteScript;

    public bool Ocupada { get; private set; }

    public void OcuparMesa()
    {
        CambiarEstado(true);
    }

    public void LiberarMesa()
    {
        CambiarEstado(false);
    }

    void CambiarEstado(bool ocupada)
    {
        Ocupada = ocupada;
        if (imagenClientes != null) imagenClientes.SetActive(ocupada);
    }

    void OnMouseDown()
    {
        if (restauranteScript != null) restauranteScript.ClicEnMesa(this);
        else Debug.LogError("[" + gameObject.name + "] Falta asignar el RestauranteScript en el Inspector.");
    }
}
