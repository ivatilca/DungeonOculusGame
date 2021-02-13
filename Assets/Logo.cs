
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Logo : MonoBehaviour , IMixedRealityInputHandler 
{
    [Header("References")]

    [SerializeField]
    private Rigidbody rigidBody;

    [SerializeField]
    private MeshRenderer mesh;

    [SerializeField]
    private SolverHandler solverHandler;  

    [Header("Events")]
    public UnityEvent throwLogo = new UnityEvent();
    public UnityEvent getBackLogo = new UnityEvent();


    private LogoState state = LogoState.Free;
    private enum LogoState
    {
        Free,
        SourceTracked,
        PhysicsTracked,
    };
    private LogoState CurrentState
    {
        get => state;
        set
        {
            if (state != value)
            {
                state = value;
                LogoStateUpdate();
            }
        }
    }
    private void LogoStateUpdate()
    {
        IsGivingPower = false;
        solverHandler.UpdateSolvers = CurrentState != LogoState.PhysicsTracked;
        mesh.enabled = CurrentState != LogoState.Free;
        rigidBody.isKinematic = CurrentState != LogoState.PhysicsTracked;
    }
    private bool givePower = false;
    private float powerTimer;
    private bool IsGivingPower
    {
        get => givePower;
        set
        {
            if (givePower != value)
            {
                givePower = value;
                PowerUpUpdate();
            }
        }
    }
    private void PowerUpUpdate()
    {
        powerTimer = 0.0f;
    }
    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler>(this);
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler>(this);
    }
    private IMixedRealityController trackedController;
    private bool IsTrackingSource(uint sourceId)
    {
        return trackedController?.InputSource.SourceId == sourceId;
    }
    private static IMixedRealityController GetTrackedController(Handedness handedness)
    {
        foreach (IMixedRealityController c in CoreServices.InputSystem?.DetectedControllers)
        {
            if (c.ControllerHandedness.IsMatch(handedness))
            {
                return c;
            }
        }
        return null;
    }
    private static IMixedRealityPointer GetLinePointer(IMixedRealityController controller)
    {
        foreach (var pointer in controller?.InputSource?.Pointers)
        {
            if (pointer is LinePointer linePointer)
            {
                return linePointer;
            }
        }
        return null;
    }

[SerializeField]
    private float MaxPower = 1.15f;
    [SerializeField]
    private float ForceAmplifier = 3.0f;

    private float ThrowForce => Mathf.Clamp(powerTimer, 0.0f, MaxPower) * ForceAmplifier;
    private IMixedRealityPointer trackedLinePointer;
    private Vector3 vectorDirection =>
            trackedLinePointer != null ? trackedLinePointer.Rotation * Vector3.forward : Vector3.zero;
    private void Throw()
    {
        if (trackedLinePointer != null)
        {
            var forceVec = vectorDirection * ThrowForce;

            CurrentState = LogoState.PhysicsTracked;

            rigidBody.AddForce(forceVec, ForceMode.Impulse);

            throwLogo?.Invoke();
        }
    }



    public void OnInputUp(InputEventData eventData)
    {
         if (IsTrackingSource(eventData.SourceId)
&& eventData.MixedRealityInputAction.Description == "Select")
        {
            if (CurrentState == LogoState.SourceTracked)
            {
                Throw();
            }
            else
            {
                CurrentState = LogoState.SourceTracked;
                getBackLogo?.Invoke();
            }
        }
    }

    public void OnInputDown(InputEventData eventData)
    {
         if (IsTrackingSource(eventData.SourceId)
          && eventData.MixedRealityInputAction.Description == "Select")
        {
            if (CurrentState == LogoState.SourceTracked)
            {
                IsGivingPower = true;
                powerTimer = 0.0f;
            }

        }
    }
    private void Awake()
    {
        Debug.Assert(rigidBody != null);
        Debug.Assert(solverHandler != null);
        Debug.Assert(mesh != null);

        PowerUpUpdate();
        LogoStateUpdate();
    }

    public bool IsTracking => solverHandler.TransformTarget != null;
    private bool wasTracked = false;
    private void Update()
    {
        bool isTracked = IsTracking;
        if (wasTracked != IsTracking)
        {
            trackedController = isTracked ? GetTrackedController(solverHandler.CurrentTrackedHandedness) : null;
            trackedLinePointer = isTracked ? GetLinePointer(trackedController) : null;
            wasTracked = isTracked;
        }

        if (isTracked)
        {
            if (CurrentState == LogoState.Free)
            {
                CurrentState = LogoState.SourceTracked;
            }
            else if (CurrentState == LogoState.SourceTracked && IsGivingPower)
            {
                powerTimer += Time.deltaTime;
                UpdateThrowVisuals();
            }
        }
        else
        {
            CurrentState = LogoState.Free;
        }
    }
    private void UpdateThrowVisuals()
    {
        float velocity = ThrowForce;
    }

}
