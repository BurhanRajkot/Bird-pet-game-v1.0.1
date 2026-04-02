using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class BirdController : MonoBehaviour
{
    public enum BirdState
    {
        Nesting,
        FlyingToWindow,
        Perching,
        FlyingToNest
    }

    [Header("References")]
    public Transform nestTransform;
    public WindowTracker windowTracker;

    [Header("Movement")]
    public float flySpeed = 4f;
    public float bobAmount = 0.05f;
    public float bobSpeed = 2f;
    public float perchFollowThreshold = 0.15f;

    [Header("Flight Arc")]
    public float arcHeight = 1.2f;

    [Header("Sprite Flipping")]
    public bool flipWhenMovingLeft = true;

    [Header("Debug")]
    public BirdState currentState;

    SpriteRenderer sr;
    Animator animator;

    Vector3 flightOrigin;
    Vector3 flightTarget;
    float flightT;

    Vector3 bobBase;
    bool bobSet;

    Vector3 lastPerch;

    bool wasFlying = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (nestTransform != null)
            transform.position = nestTransform.position;

        EnterState(BirdState.Nesting);
    }

    void Update()
    {
        if (windowTracker == null || nestTransform == null)
            return;

        switch (currentState)
        {
            case BirdState.Nesting:
                UpdateNesting();
                break;

            case BirdState.FlyingToWindow:
                UpdateFlight(BirdState.Perching);
                break;

            case BirdState.Perching:
                UpdatePerching();
                break;

            case BirdState.FlyingToNest:
                UpdateFlight(BirdState.Nesting);
                break;
        }

        HandleTransitions();
    }

    void HandleTransitions()
    {
        bool hasWindow = windowTracker.HasValidWindow;

        switch (currentState)
        {
            case BirdState.Nesting:
                if (hasWindow)
                {
                    lastPerch = windowTracker.CurrentPerchWorld;
                    BeginFlight(lastPerch, BirdState.FlyingToWindow);
                }
                break;

            case BirdState.Perching:
                if (!hasWindow)
                {
                    BeginFlight(nestTransform.position, BirdState.FlyingToNest);
                }
                else
                {
                    Vector3 newPerch = windowTracker.CurrentPerchWorld;
                    if (Vector3.Distance(lastPerch, newPerch) > perchFollowThreshold)
                    {
                        lastPerch = newPerch;
                        BeginFlight(lastPerch, BirdState.FlyingToWindow);
                    }
                }
                break;
        }
    }

    void EnterState(BirdState newState)
    {
        currentState = newState;
        bobSet = false;

        bool shouldFly = (newState == BirdState.FlyingToWindow || newState == BirdState.FlyingToNest);
        SetFlying(shouldFly);
    }

    void SetFlying(bool isFlying)
    {
        if (animator == null) return;

        animator.SetBool("IsFlying", isFlying);

        // Force immediate state play — bypasses exit time and blend duration
        if (isFlying && !wasFlying)
        {
            animator.Play("Fly", 0, 0f);
        }
        else if (!isFlying && wasFlying)
        {
            // Match the exact animation clip name — check your Animator states!
            // Default Idle state is named "Ideal" in your setup (from the screenshot)
            animator.Play("Ideal", 0, 0f);
        }

        wasFlying = isFlying;
    }

    void BeginFlight(Vector3 target, BirdState flightState)
    {
        flightOrigin = transform.position;
        flightTarget = target;
        flightT = 0f;
        EnterState(flightState);
    }

    void UpdateFlight(BirdState onArrive)
    {
        if (currentState == BirdState.FlyingToWindow && windowTracker.HasValidWindow)
            flightTarget = windowTracker.CurrentPerchWorld;

        float dist = Vector3.Distance(flightOrigin, flightTarget);
        float duration = Mathf.Max(dist / flySpeed, 0.1f);

        flightT += Time.deltaTime / duration;
        float t = Mathf.Clamp01(flightT);

        float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;

        Vector3 newPos = Vector3.Lerp(flightOrigin, flightTarget, t)
                         + new Vector3(0f, arc, 0f);

        // Flip sprite based on horizontal movement direction
        if (flipWhenMovingLeft)
        {
            float dx = newPos.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.001f)
                sr.flipX = dx < 0f;
        }

        transform.position = newPos;

        if (t >= 1f)
            EnterState(onArrive);
    }

    void UpdateNesting()
    {
        Bob(nestTransform.position);
    }

    void UpdatePerching()
    {
        if (windowTracker.HasValidWindow)
        {
            lastPerch = windowTracker.CurrentPerchWorld;
            bobBase = lastPerch;
            bobSet = true;
        }

        Bob(bobBase);
    }

    void Bob(Vector3 basePos)
    {
        if (!bobSet)
        {
            bobBase = basePos;
            bobSet = true;
        }

        transform.position = bobBase + new Vector3(
            0f,
            Mathf.Sin(Time.time * bobSpeed) * bobAmount,
            0f
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (nestTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(nestTransform.position, 0.2f);
        }

        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(flightTarget, 0.15f);
            Gizmos.DrawLine(transform.position, flightTarget);
        }
    }
#endif
}