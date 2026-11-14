using UnityEngine;

/// <summary>
/// Memutar objek secara kontinu pada poros tertentu (misalnya kincir angin atau turbin).
/// </summary>
[DisallowMultipleComponent]
public class VillageSpinner : MonoBehaviour
{
    [Tooltip("Sumbu rotasi lokal.")]
    public Vector3 axis = Vector3.right;

    [Tooltip("Kecepatan rotasi dalam derajat per detik.")]
    public float degreesPerSecond = 14f;

    void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
