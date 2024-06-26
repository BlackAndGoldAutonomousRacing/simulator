/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;
using Simulator.Api;

public class VehicleController : AgentController
{
    private IVehicleDynamics Dynamics;
    private VehicleActions Actions;
    private IAgentController Controller;
    private List<IVehicleInputs> Inputs = new List<IVehicleInputs>();

    private Vector3 InitialPosition;
    private Quaternion InitialRotation;
    private Vector3 LastRBPosition;
    private Vector3 SimpleVelocity;
    private Vector3 SimpleAcceleration;

    public override Vector3 Velocity => SimpleVelocity;
    public override Vector3 Acceleration => SimpleAcceleration;

    private float TurnSignalTriggerThreshold = 0.2f;
    private float TurnSignalOffThreshold = 0.1f;
    private bool ResetTurnIndicator = false;

    // api do not remove
    private bool Sticky = false;
    private float StickySteering;
    private float StickyAcceleration;
    private float StickyBraking;

    public void Update()
    {
        UpdateInput();
        UpdateLights();
    }

    public void FixedUpdate()
    {
        UpdateInputAPI();

        if (Time.fixedDeltaTime > 0)
        {
            var previousVelocity = SimpleVelocity;
            var position = transform.position;
            SimpleVelocity = (position - LastRBPosition) / Time.fixedDeltaTime;
            SimpleAcceleration = SimpleVelocity - previousVelocity;
            LastRBPosition = position;
        }
    }

    public override void Init()
    {
        Dynamics = GetComponent<IVehicleDynamics>();
        Actions = GetComponent<VehicleActions>();
        Controller = GetComponent<IAgentController>();
        Inputs.AddRange(GetComponentsInChildren<IVehicleInputs>());
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
    }

    private void UpdateInput()
    {
        if (Sticky)
        {
            return;
        }

        SteerInput = AccelInput = BrakeInput = 0f;

        // get all inputs
        foreach (var input in Inputs)
        {
            SteerInput += input.SteerInput;
            AccelInput += input.AccelInput;
            BrakeInput += input.BrakeInput;

            if (input.TargetGear != null)
            {
                TargetGear = (int)input.TargetGear;
            }
        }

        // clamp if over
        SteerInput = Mathf.Clamp(SteerInput, -1f, 1f);
        AccelInput = Mathf.Clamp01(AccelInput);
        BrakeInput = Mathf.Clamp01(BrakeInput);
    }

    private void UpdateInputAPI()
    {
        if (!Sticky)
        {
            return;
        }

        SteerInput = StickySteering;
        AccelInput = StickyAcceleration;
        BrakeInput = StickyBraking;
    }

    private void UpdateLights()
    {
        if (Actions == null)
        {
            return;
        }

        // brakes
        if (AccelInput < 0 || BrakeInput > 0)
        {
            Actions.BrakeLights = true;
        }
        else
        {
            Actions.BrakeLights = false;
        }

        // reverse
        Actions.ReverseLights = Dynamics.Reverse;

        // turn indicator reset on turn
        if (Actions.LeftTurnSignal)
        {
            if (ResetTurnIndicator)
            {
                if (SteerInput > -TurnSignalOffThreshold)
                {
                    Actions.LeftTurnSignal = ResetTurnIndicator = false;
                }

            }
            else
            {
                if (SteerInput < -TurnSignalTriggerThreshold)
                {
                    ResetTurnIndicator = true;
                }
            }
        }

        if (Actions.RightTurnSignal)
        {
            if (ResetTurnIndicator)
            {
                if (SteerInput < TurnSignalOffThreshold)
                {
                    Actions.RightTurnSignal = ResetTurnIndicator = false;
                }
            }
            else
            {
                if (SteerInput > TurnSignalTriggerThreshold)
                {
                    ResetTurnIndicator = true;
                }
            }
        }
    }

    public override void ResetPosition()
    {
        if (Dynamics == null)
        {
            return;
        }

        Dynamics.ForceReset(InitialPosition, InitialRotation);
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        if (Dynamics == null)
        {
            return;
        }

        Dynamics.ForceReset(pos, rot);
    }

    // api
    public override void ApplyControl(bool sticky, float steering, float acceleration, float braking)
    {
        this.Sticky = sticky;
        StickySteering = steering;
        StickyAcceleration = acceleration;
        StickyBraking = braking;
    }

    public override void ApplyControl(bool sticky, float steering, float acceleration)
    {
        this.Sticky = sticky;
        StickySteering = steering;
        if (acceleration > 0f)
        {
            StickyAcceleration = acceleration;
            StickyBraking = 0f;
        } else {
            StickyAcceleration = 0f;
            StickyBraking = -acceleration;
        }
    }

    public void ResetStickyControl()
    {
        Sticky = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        int layerMask = LayerMask.GetMask("Obstacle", "Agent", "Pedestrian", "NPC");
        int layer = collision.gameObject.layer;
        string otherLayer = LayerMask.LayerToName(layer);
        var otherRB = collision.gameObject.GetComponent<IAgentController>();
        var otherVel = otherRB != null ? otherRB.Velocity : Vector3.zero;
        if ((layerMask & (1 << layer)) != 0)
        {
            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject, collision);
            SimulatorManager.Instance.AnalysisManager.IncrementEgoCollision(Controller.GTID, transform.position, Dynamics.Velocity, otherVel, otherLayer);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        int layerMask = LayerMask.GetMask("Obstacle", "Agent", "Pedestrian", "NPC");
        int layer = collision.gameObject.layer;
        string otherLayer = LayerMask.LayerToName(layer);
        var otherRB = collision.gameObject.GetComponent<IAgentController>();
        var otherVel = otherRB != null ? otherRB.Velocity : Vector3.zero;
        if ((layerMask & (1 << layer)) != 0)
        {
            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject);
            SimulatorManager.Instance.AnalysisManager.IncrementEgoCollision(Controller.GTID, transform.position, Dynamics.Velocity, otherVel, otherLayer);
        }
    }
}
