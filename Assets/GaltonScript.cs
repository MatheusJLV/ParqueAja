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

    // Nuevas variables de posiciones referencia
    public Transform referencia1;
    public Transform referencia2;

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
        //Instanciar(5);
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
            Debug.Log("Instanciando bolitas: " + cantidad);

            // Validar si 'bolas' es null y buscarlo si es necesario
            if (bolas == null)
            {
                Debug.LogWarning("'bolas' es null. Buscando el GameObject 'Bolas' como hijo del GameObject actual.");
                bolas = transform.Find("Bolas")?.gameObject;

                if (bolas == null)
                {
                    Debug.LogError("No se encontró un GameObject llamado 'Bolas' como hijo del GameObject actual.");
                    yield break; // Salir de la corutina si no se encuentra 'bolas'
                }
            }

            // Instanciar la bolita en la posición de "bolas"
            GameObject bolita1 = Instantiate(bolita, bolas.transform.position, Quaternion.identity);
            bolita1.transform.SetParent(bolas.transform); // Asignar como hijo de "bolas"
            bolita1.transform.localScale = Vector3.one * escala; // Aplicar escala

            // Instanciar la bolita en la posición de "referencia1"
            if (referencia1 != null)
            {
                GameObject bolita2 = Instantiate(bolita, referencia1.position, Quaternion.identity);
                bolita2.transform.SetParent(bolas.transform); // Asignar como hijo de "bolas"
                bolita2.transform.localScale = Vector3.one * escala; // Aplicar escala
            }

            // Instanciar la bolita en la posición de "referencia2"
            if (referencia2 != null)
            {
                GameObject bolita3 = Instantiate(bolita, referencia2.position, Quaternion.identity);
                bolita3.transform.SetParent(bolas.transform); // Asignar como hijo de "bolas"
                bolita3.transform.localScale = Vector3.one * escala; // Aplicar escala
            }

            // Reducir la cantidad en 1
            cantidad--;

            // Esperar el tiempo especificado antes de la siguiente iteración
            float tiempoEspera = tiempo / tiempoSlider.value;
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
