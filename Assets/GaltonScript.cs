using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GaltonScript : MonoBehaviour
{
    // Variables públicas
    public GameObject bolas;
    public GameObject bolita;
    public float tiempo = 1f;
    public int cantidad = 0;
    public float escala = 1f; // Nueva variable para definir la escala

    // Variables de UI
    public Slider cantidadSlider;
    public Slider tiempoSlider;
    public Button boton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Validar referencias nulas
        if (bolas == null || bolita == null)
        {
            Debug.LogError("Las referencias a 'bolas' o 'bolita' no están asignadas.");
        }

        if (cantidadSlider == null || tiempoSlider == null || boton == null)
        {
            Debug.LogError("Las referencias a los elementos de UI no están asignadas.");
            return;
        }

        // Suscribirse al evento del botón
        boton.onClick.AddListener(OnBotonPresionado);
        Instanciar(5);
    }

    // Método llamado cuando se presiona el botón
    private void OnBotonPresionado()
    {
        // Leer el valor del slider de cantidad y transformarlo a entero
        int valorCantidad = Mathf.RoundToInt(cantidadSlider.value);

        // Llamar al método Instanciar con el valor obtenido
        Instanciar(valorCantidad);
    }

    // Método público para manejar la cantidad de bolitas a instanciar
    public void Instanciar(int valor)
    {
        // Validar el valor recibido
        if (valor <= 0)
        {
            valor = 1;
        }

        // Verificar el estado de la variable cantidad
        if (cantidad <= 0)
        {
            cantidad = valor;
            StartCoroutine(InstanciarBolitas());
        }
        else
        {
            cantidad += valor;
        }
    }

    // Corutina para instanciar bolitas
    private IEnumerator InstanciarBolitas()
    {
        while (cantidad > 0)
        {
            Debug.Log("Instanciando bolita: " + cantidad + " en " + bolas.transform.position);

            // Instanciar una nueva bolita en la posición de "bolas" y asignarla como hijo
            GameObject nuevaBolita = Instantiate(bolita, bolas.transform.position, Quaternion.identity);
            nuevaBolita.transform.SetParent(bolas.transform); // Usar SetParent para asignar el padre

            // Aplicar la escala al objeto instanciado
            nuevaBolita.transform.localScale = Vector3.one * escala;

            // Reducir la cantidad en 1
            cantidad--;

            // Esperar el tiempo especificado antes de la siguiente iteración
            float tiempoEspera = tiempo/ tiempoSlider.value;
            yield return new WaitForSeconds(tiempoEspera);
        }
    }

    /*public void RevisarBolas()
    {
        if (bolas == null)
        {
            Debug.LogError("El objeto 'bolas' no está asignado.");
            return;
        }

        int index = 1;
        foreach (Transform child in bolas.transform)
        {
            Vector3 position = child.position;
            Debug.Log($"Bola {index} en posición: {position.x}, {position.y}, {position.z}");
            index++;
        }

        if (index == 1)
        {
            Debug.Log("No hay bolitas como hijos del objeto 'bolas'.");
        }
    }*/

}
