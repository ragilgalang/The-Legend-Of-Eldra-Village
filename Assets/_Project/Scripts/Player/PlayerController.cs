using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float walkSpeed = 4f, runSpeed = 7f, turnSpeed = 12f, gravity = -20f;
    [SerializeField] Animator anim;

    public bool inputLocked;
    CharacterController cc;
    float vy;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        Vector2 move = Vector2.zero;
        bool run = false;
        var kb = Keyboard.current;

        if (!inputLocked && kb != null)
        {
            if (kb.wKey.isPressed) move.y += 1;
            if (kb.sKey.isPressed) move.y -= 1;
            if (kb.dKey.isPressed) move.x += 1;
            if (kb.aKey.isPressed) move.x -= 1;
            run = kb.leftShiftKey.isPressed;
        }

        // Arah gerak berdasarkan yaw pandangan, bukan transform kamera
        float yaw = CameraRig.Instance.AimYaw;
        Quaternion yawRot = Quaternion.Euler(0, yaw, 0);
        Vector3 f = yawRot * Vector3.forward;
        Vector3 r = yawRot * Vector3.right;
        Vector3 dir = Vector3.ClampMagnitude(f * move.y + r * move.x, 1f);

        if (CameraRig.Instance.IsFirstPerson)
        {
            // Orang pertama: badan selalu searah pandangan mouse
            transform.rotation = yawRot;
        }
        else if (dir.sqrMagnitude > 0.01f)
        {
            // Orang ketiga: badan menghadap arah jalan
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        }

        vy = cc.isGrounded ? -1f : vy + gravity * Time.deltaTime;
        Vector3 vel = dir * (run ? runSpeed : walkSpeed);
        vel.y = vy;
        cc.Move(vel * Time.deltaTime);

        if (anim) anim.SetFloat("Speed", dir.magnitude * (run ? 1f : 0.5f));
    }
}
