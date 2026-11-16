using UnityEngine;

public class FarmAnimalMotion : MonoBehaviour
{
    public Transform[] legs;
    public Transform tail;
    public Transform jaw;
    public float gaitSpeed = 4f;
    public float tailSpeed = 2f;
    public float tailAngle = 18f;
    public float jawAngle = 3f;

    Quaternion[] legRestRotations;
    Quaternion tailRestRotation;
    Quaternion jawRestRotation;

    void Awake()
    {
        if (legs != null)
        {
            legRestRotations = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                if (legs[i]) legRestRotations[i] = legs[i].localRotation;
        }

        if (tail) tailRestRotation = tail.localRotation;
        if (jaw) jawRestRotation = jaw.localRotation;
    }

    void Update()
    {
        float gait = Time.time * gaitSpeed;
        if (legs != null)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                if (!legs[i]) continue;
                float phase = (i == 0 || i == 3) ? 0f : Mathf.PI;
                float swing = Mathf.Sin(gait + phase) * 5f;
                legs[i].localRotation = legRestRotations[i] * Quaternion.Euler(swing, 0f, 0f);
            }
        }

        if (tail)
        {
            float wag = Mathf.Sin(Time.time * tailSpeed) * tailAngle;
            tail.localRotation = tailRestRotation * Quaternion.Euler(0f, wag, 0f);
        }

        if (jaw)
        {
            float open = (Mathf.Sin(Time.time * 2.3f) + 1f) * 0.5f * jawAngle;
            jaw.localRotation = jawRestRotation * Quaternion.Euler(open, 0f, 0f);
        }
    }
}
