using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

public struct PlayerInputs {
	public float moveHorizontal;
	public float moveVertical;
    public float lookHorizontal;
    public float lookVertical;
	public bool jump;
	public bool boost;
}

public enum PlayerState {
	Ground,
    Air,
    Wall,
    Boost
}

public class PlayerControllerV2 : BaseCharacterController
{
    public bool DebugLog = false;

	[Header("General Settings")]
	public PlayerState CurrentState = PlayerState.Ground;
	public List<Collider> IgnoredColliders = new List<Collider>();
	public float InputDelay = 0.1f;
    [Tooltip("The head of the player, will rotate vertically")]
    public GameObject Head;

    [Header("Movement Settings")]
    public float GroundMoveSpeed = 5f;
	public float GroundMoveMaxSpeed = 10f;
	public float AirMoveSpeed = 5f;
    public float AirMoveMaxSpeed = 25f;
	public float BoostCooldown = 2f;
	public float BoostSpeed = 10f;
    public float MoveSharpness = 10f;
    public float JumpCooldown = 0.5f;
    public float JumpHeight = 10f;
    public float Gravity = 20f;
    public float AirDrag = 0.1f;
    public float GroundDrag = 2f;
    public float InternalDrag = 2f;
    public float HitReduction = 0.5f;

    [Header("Aim Settings")]
    public float LookMinSensitivity = 5f;
    public float LookMaxSensitivity = 20f;
    public float LookSharpness = 10f;
    public float MinVerticalAngle = -85f;
    public float MaxVerticalAngle = 85f;

    //Input
	private Vector3 _moveInput = Vector3.zero;
	private Vector3 _lookInput = Vector3.zero;

    //Jumping
	private bool _jumpReq = false;
	private float _jumpReqTimer = 0f;
    private bool _jumpAvailable = true;
    private float _jumpCooldownTimer = 0f;

    //Boosting
	private bool _boostReq = false;
	private float _boostReqTimer = 0f;
	private float _boostCooldownTimer = 0f;
	private bool _boostPerformed = false;
	private bool _boostAvailable = true;
	private Vector3 _boostDirection = Vector3.zero;

    //Other
    private CharacterGroundingReport _lastGroundingReport;
    private Vector3 _lastVelocity;
    private Vector3 _internalVelocity = Vector3.zero;

	public void SetPlayerInputs (PlayerInputs inputs) {
        _moveInput = new Vector3(inputs.moveHorizontal, 0f, inputs.moveVertical);
        _lookInput = new Vector3(inputs.lookVertical, inputs.lookHorizontal, 0f);
		_jumpReq = inputs.jump;
		_boostReq = inputs.boost;
	}

	public void TransitionToState(PlayerState next) {
		CurrentState = next;
	}

	public override void AfterCharacterUpdate(float deltaTime)
	{
	}

	public override void BeforeCharacterUpdate(float deltaTime)
	{
        //Updating various timers
		if (_boostReq) {
			_boostReqTimer += deltaTime;
		} else {
			_boostReqTimer = 0f;
		}
		if (_boostPerformed) {
			_boostCooldownTimer += deltaTime;
			if (_boostCooldownTimer >= BoostCooldown) {
				_boostAvailable = true;
				_boostPerformed = false;
			}
		}

        if (_jumpReqTimer <= InputDelay) {
            _jumpReqTimer += deltaTime;
            if (_jumpReqTimer > InputDelay) {
                _jumpReq = false;
            }
        }

        if (_jumpCooldownTimer <= JumpCooldown) {
            _jumpCooldownTimer += deltaTime;
            if (_jumpCooldownTimer > JumpCooldown) {
                _jumpAvailable = true;
            } else {
                _jumpAvailable = false;
            }
        }

        //Boosting can happen during any state
		if (_boostReq) {
			if (_boostReqTimer <= InputDelay) {
				if (_boostAvailable) {
					//Perform boost
					_boostDirection = _moveInput;
					if (_moveInput.magnitude <= 0.1f)
						_boostDirection = Motor.CharacterForward;
					TransitionToState(PlayerState.Boost);
					_boostPerformed = true;
					_boostReq = false;
				} else {
					//Cooling down
                    //TODO Give audio/visual feedback for this
				}
			} else {
				_boostReq = false;
			}
		}
	}

	public override void HandleMovementProjection(ref Vector3 movement, Vector3 obstructionNormal, bool stableOnHit)
	{
	}

	public override bool IsColliderValidForCollisions(Collider coll)
	{
		return !IgnoredColliders.Contains(coll);
	}

	public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{
	}

	public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{
	}

	public override void PostGroundingUpdate(float deltaTime)
	{
        if (_lastGroundingReport.FoundAnyGround && !Motor.GroundingStatus.FoundAnyGround) {
            //Left ground
        }
        if (!_lastGroundingReport.FoundAnyGround && Motor.GroundingStatus.FoundAnyGround) {
            //Landed on ground

            //Preserve current velocity
            _internalVelocity = _lastVelocity;
            Vector3 groundNorm = Motor.GroundingStatus.GroundNormal;
            _internalVelocity = Vector3.ProjectOnPlane(_internalVelocity, groundNorm);
            _internalVelocity *= (_lastVelocity - _internalVelocity).magnitude * HitReduction;
        }

        _lastGroundingReport = Motor.GroundingStatus;
        _lastVelocity = Motor.Velocity;
	}

	public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
	{
	}

	public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
	{
        switch (CurrentState) {
            case PlayerState.Ground:
                {
                    NormalLook(ref currentRotation, deltaTime);
                    break;
                }
            case PlayerState.Air:
                {
                    NormalLook(ref currentRotation, deltaTime);
                    break;
                }
            case PlayerState.Wall:
                {
                    //Going to be slightly rotated to skew away from the wall
                    break;
                }
            case PlayerState.Boost:
                {
                    NormalLook(ref currentRotation, deltaTime);
                    break;
                }
        }
	}

	public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
	{
        UpdatePlayerState();

        //Drag internalVelocity
        if (Vector3.Dot(transform.TransformDirection(_moveInput).normalized, _internalVelocity.normalized) <= 0f && _moveInput.sqrMagnitude > 0f)
        {
            //Input and Internal are facing away
            _internalVelocity = Vector3.zero;
        }
        else
        {
            //Facing the same direction
            _internalVelocity *= (1f / (1f + InternalDrag));
        }
        if (_internalVelocity.magnitude <= 0.3f)
            _internalVelocity = Vector3.zero;

        //Jumping happens any time
        if (_jumpReq)
        {
            //Requested a jump
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                //Currently on ground
                if (_jumpAvailable)
                {
                    //Perform a jump
                    if (Motor.GroundingStatus.IsStableOnGround)
                    {
                        //Vertical jump
                        currentVelocity += Vector3.up * Mathf.Sqrt(2f * JumpHeight * Gravity);
                    }
                    else
                    {
                        //Sloped jump
                        currentVelocity += Motor.GroundingStatus.GroundNormal * Mathf.Sqrt(2f * JumpHeight * Gravity);
                    }

                    _jumpReq = false;
                    _jumpCooldownTimer = 0f;
                    TransitionToState(PlayerState.Air);
                    Motor.ForceUnground();
                }
            }
        }

		switch (CurrentState) {
			case PlayerState.Ground:
				{
					GroundMove(ref currentVelocity, deltaTime);
					break;               
				}
			case PlayerState.Air:
				{
					AirMove(ref currentVelocity, deltaTime);
					break;
				}
			case PlayerState.Wall:
				{

					break;
				}
			case PlayerState.Boost:
				{

					break;
				}
		}
	}

    private void UpdatePlayerState() 
    {
        //Update player state
        switch (CurrentState)
        {
            case PlayerState.Ground:
                {
                    if (!Motor.GroundingStatus.FoundAnyGround)
                    {
                        TransitionToState(PlayerState.Air);
                    }
                    break;
                }
            case PlayerState.Air:
                {
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        TransitionToState(PlayerState.Ground);
                    }
                    break;
                }
            case PlayerState.Wall:
                {

                    break;
                }
            case PlayerState.Boost:
                {

                    break;
                }
        }
    }

	private void GroundMove(ref Vector3 currentVelocity, float deltaTime)
	{
		Vector3 targetVelocity = Vector3.zero;

		//Reorient velocity to surface normal
		Vector3 norm = Motor.GroundingStatus.GroundNormal;
        if (currentVelocity.sqrMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
        {
            Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
            if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0) {
                norm = Motor.GroundingStatus.OuterGroundNormal;
            } else {
                norm = Motor.GroundingStatus.InnerGroundNormal;
            }
        }
        currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, norm) * currentVelocity.magnitude;

		targetVelocity = _moveInput * GroundMoveSpeed;
        targetVelocity = transform.TransformDirection(targetVelocity);
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-MoveSharpness * deltaTime));
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, GroundMoveMaxSpeed);

        //Add in internal velocity
        if (_internalVelocity.sqrMagnitude >= 0f)
        {
            currentVelocity += _internalVelocity * deltaTime;
        }
	}

	private void AirMove(ref Vector3 currentVelocity, float deltaTime) 
	{
        Vector3 targetVelocity = _moveInput * AirMoveSpeed;
        targetVelocity = transform.TransformDirection(targetVelocity);

        currentVelocity += targetVelocity * AirMoveSpeed * deltaTime;
        Vector3 velNoGrav = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        velNoGrav = Vector3.ClampMagnitude(velNoGrav, AirMoveMaxSpeed);
        velNoGrav.y += currentVelocity.y;
        currentVelocity = velNoGrav;
        currentVelocity += Vector3.down * Gravity * deltaTime;
        if (Motor.GroundingStatus.FoundAnyGround)
        {
            Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, perpendicularObstructionNormal);
        }
        currentVelocity *= (1f / (1f + (AirDrag * deltaTime)));

        if (_internalVelocity.sqrMagnitude >= 0f) {
            currentVelocity += _internalVelocity * deltaTime;
        }
	}

    private void NormalLook(ref Quaternion currentRotation, float deltaTime)
    {
        float inputHorz = _lookInput.y;

        //Left/Right aiming
        Quaternion bodyRot = currentRotation;
        Vector3 bodyRotEuler = bodyRot.eulerAngles;
        bodyRotEuler.y += inputHorz * Mathf.Lerp(LookMinSensitivity, LookMaxSensitivity, Mathf.Abs(inputHorz));
        Motor.RotateCharacter(Quaternion.Lerp(bodyRot, Quaternion.Euler(bodyRotEuler), deltaTime * LookSharpness));
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < 0f) angle = 360 + angle;
        if (angle > 180f) return Mathf.Max(angle, 360 + min);
        return Mathf.Min(angle, max);
    }

    public void OnGUI()
    {
        if (!DebugLog) return;

        GUILayout.Label(string.Format("Player State: {0}", CurrentState.ToString()));
        GUILayout.Label(string.Format("Internal Velocity : {0}", _internalVelocity));
        Debug.DrawLine(transform.position, transform.position + Motor.Velocity, Color.red, 10f);
        Debug.DrawLine(transform.position, transform.position + Motor.CharacterForward, Color.green, 10f);
    }

    //Only used for camera movement for smoothing purposes
    public void LateUpdate()
    {
        float inputVert = _lookInput.x;
        //Up/Down aiming
        Quaternion headRot = Head.transform.localRotation;
        Vector3 headRotEuler = headRot.eulerAngles;
        headRotEuler.x = ClampAngle(headRotEuler.x + inputVert * Mathf.Lerp(LookMinSensitivity, LookMaxSensitivity, Mathf.Abs(inputVert)),
                                    MinVerticalAngle,
                                    MaxVerticalAngle
                                   );
        Head.transform.localRotation = Quaternion.Lerp(headRot, Quaternion.Euler(headRotEuler), Time.deltaTime * LookSharpness);
    }
}
