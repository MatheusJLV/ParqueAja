using UnityEngine;

/* Este script refleja (espeja) la posición y rotación de un objeto respecto a una referencia
o plano de simetría.*/

public class CajaSimetria : MonoBehaviour
{
    // ----- CONFIGURACIÓN DE POSICIÓN -----
    [Header("Position axis inversion (1 = normal, -1 = mirrored)")]
    public int xAxis = 1;// Controla el eje x (si es 1 no se invierte, si es -1 se refleja)
    public int yAxis = 1;// Controla el eje y
    public int zAxis = 1;// Controla el eje Z

    // ----- CONFIGURACIÓN DE ROTACIÓN -----
    [Header("Rotation mirror")]
    public bool mirrorRotation = true;// Si está activo, también se refleja la rotación
    public Vector3 rotationMirrorNormal = Vector3.right; // Default mirror across X plane /Plano de espejo (por defecto plano perpendicular al eje X)

    // ----- REFERENCIAS -----
    [Header("References")]
    public Transform target; // Objeto que actuará como reflejo
    public Transform referencia; // Punto central de simetría (si no se asigna, usa el origen)
    public bool useGlobalSpace = true; // Si es true, trabaja con coordenadas globales; si no, con locales

    // ----- CONFIGURACIÓN AUTOMÁTICA -----
    [Header("Auto-assign target by name")]
    public string reflectGO; // Nombre del GameObject a reflejar si no se asigna manualmente

    // ----- VARIABLES INTERNAS -----
    private Vector3 _lastPosition; // Última posición registrada (para detectar cambios)
    private Quaternion _lastRotation; // Última rotación registrada

    void Start()
    {
        // Auto-assign referencia
        GameObject foundReferencia = GameObject.Find("ref simetria camp sim");
        if (foundReferencia != null)
        {
            referencia = foundReferencia.transform;
            Debug.Log("CajaSimetria: Referencia set to 'ref simetria camp sim'");
        }
        else
        {
            Debug.LogWarning("CajaSimetria: No GameObject found named 'ref simetria camp sim'");
        }

        // Solo actualiza el reflejo si el target está asignado
        if (target != null)
        {
            UpdateMirroring();
            _lastPosition = useGlobalSpace ? transform.position : transform.localPosition;
            _lastRotation = useGlobalSpace ? transform.rotation : transform.localRotation;
        }
    }

    void Update()
    {
        // Si no hay target asignado, no se hace nada
        if (target == null) return;

         // Obtiene posición y rotación actuales (global o local)
        Vector3 currentPosition = useGlobalSpace ? transform.position : transform.localPosition;
        Quaternion currentRotation = useGlobalSpace ? transform.rotation : transform.localRotation;

        // Only update if transform changed (approximate check)
         // Solo actualiza si hubo un cambio real en posición o rotación
        if (_lastPosition != currentPosition || Quaternion.Angle(currentRotation, _lastRotation) > 0.01f)
        {
            UpdateMirroring();
            _lastPosition = currentPosition;
            _lastRotation = currentRotation;
        }
    }

    // Actualiza la posición y rotación del objeto reflejado
    void UpdateMirroring()
    {
        // === POSITION ===
        if (useGlobalSpace)
        {
            // Determina el punto central de simetría (o usa el origen)
            Vector3 center = referencia != null ? referencia.position : Vector3.zero;
            // Calcula el vector desde el centro hasta este objeto
            Vector3 offset = transform.position - center;

            // Invierte los ejes según la configuración
            Vector3 mirroredOffset = new Vector3(
                offset.x * xAxis,
                offset.y * yAxis,
                offset.z * zAxis
            );
            // Asigna la nueva posición reflejada al target
            target.position = center + mirroredOffset;
        }
        else
        {
            // Si trabaja en espacio local
            Vector3 localPos = transform.localPosition;
            target.localPosition = new Vector3(
                localPos.x * xAxis,
                localPos.y * yAxis,
                localPos.z * zAxis
            );
        }

        // === ROTATION ===
        if (mirrorRotation)
        {
            // Obtiene la rotación original (global o local)
            Quaternion sourceRotation = useGlobalSpace ? transform.rotation : transform.localRotation;
            // Calcula la rotación reflejada respecto al plano
            Quaternion mirroredRotation = MirrorRotationAcrossPlane(sourceRotation, rotationMirrorNormal.normalized);
            // Aplica la nueva rotación al target
            if (useGlobalSpace)
                target.rotation = mirroredRotation;
            else
                target.localRotation = mirroredRotation;
        }

    }

    // Mirrors a rotation across a plane (defined by its normal vector)
    // Refleja una rotación respecto a un plano definido por su normal
    Quaternion MirrorRotationAcrossPlane(Quaternion original, Vector3 planeNormal)
    {
        // Convert rotation to forward & up vectors
        // Convierte la rotación en vectores de dirección (adelante y arriba)
        Vector3 fwd = original * Vector3.forward;
        Vector3 up = original * Vector3.up;

        // Reflect both vectors across the mirror plane
        // Refleja ambos vectores en el plano de simetría
        fwd = Vector3.Reflect(fwd, planeNormal);
        up = Vector3.Reflect(up, planeNormal);

        // Build the mirrored rotation from the reflected vectors
         // Crea una nueva rotación a partir de los vectores reflejados
        return Quaternion.LookRotation(fwd, up);
    }
} 