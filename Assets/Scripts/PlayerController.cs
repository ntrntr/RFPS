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
    public bool run;
}

public enum PlayerState {
	Ground,
    Air,
    Wall,
    Boost
}

public class PlayerController : BaseCharacterController
{
    public bool DebugLog = false;

	[Header("General Settings")]
	public PlayerState CurrentState = PlayerState.Ground;
	public List<Collider> IgnoredColliders = new List<Collider>();
	public float InputDelay = 0.1f;
    [Tooltip("The head of the player, will rotate vertically")]
    public GameObject Head;

    [Header("Camera Settings")]
    public Camera PlayerCamera;
    public float BaseFOV;
    public float MinFOVAdd;
    public float MaxFOVAdd;

    [Header("Movement Settings")]
    public float GroundMoveSpeed = 5f;
	public float GroundMoveMaxSpeed = 10f;
    public float GroundRunSpeed = 10f;
	public float AirMoveSpeed = 5f;
    public float AirMoveMaxSpeed = 25f;
    public float AirMoveMaxEffectiveSpeed = 7f;
	public float BoostCooldown = 2f;
	public float BoostSpeed = 10f;
    public float MoveSharpness = 10f;
    public float JumpCooldown = 0.5f;
    public float JumpHeight = 10f;
    public float Gravity = 20f;
    public float AirDrag = 0.1f;
    public float GroundDrag = 2f;
    public float InternalDrag = 2f;
    public float LandingReduction = 0.5f;
    public float RunStartup = 5f;
    public float RunSlowdown = 10f;

    [Header("Aim Settings")]
    public float LookMinSensitivity = 5f;
    public float LookMaxSensitivity = 20f;
    public float LookSharpness = 10f;
    public float MinVerticalAngle = -85f;
    public float MaxVerticalAngle = 85f;

    //Input
	private Vector3 _moveInput = Vector3.zero;
	private Vector3 _lookInput = Vector3.zero;

    //Moving
    private float _targetMoveSpeed = 5f;
    private bool _runReq = false;
    private float _runReqTimer = 0f;
    private bool _runActive = false;
    private float _currentMoveSpeed = 5f;

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

    public void Start()
    {
        Debug.Assert(Head != null);
        Debug.Assert(PlayerCamera != null);
    }

    public void SetPlayerInputs (PlayerInputs inputs) {
        _moveInput = new Vector3(inputs.moveHorizontal, 0f, inputs.moveVertical);
        _lookInput = new Vector3(inputs.lookVertical, inputs.lookHorizontal, 0f);

        if (inputs.jump)
        {
            _jumpReq = inputs.jump;
            _jumpReqTimer = 0f;
        }

        if (inputs.boost)
        {
            _boostReq = inputs.boost;
            _boostReqTimer = 0f;
        }

        if (inputs.run)
        {
            _runReq = inputs.run;
            _runReqTimer = 0f;
        }

        if (_moveInput.sqrMagnitude <= 0f && _runActive)
            StopRunning();
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

        if (_runReqTimer <= InputDelay) {
            _runReqTimer += deltaTime;
            if (_runReqTimer > InputDelay) {
                _runReq = false;
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

        if (Mathf.Abs(Vector3.Dot(obstructionNormal, Vector3.up)) < 0.2f)
        {
            movement = Vector3.ProjectOnPlane(movement, obstructionNormal).normalized *
                  movement.magnitude *
                  (1f - Mathf.Clamp01(EasingFunction.EaseInOutSine(0f, 1f, Mathf.Abs(Vector3.Dot(transform.TransformDirection(_moveInput), obstructionNormal)))));
        }
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
            //TODO Maybe do some actual ground normal calculations for the internal velocity calculations
            Debug.Log("PostGroundingUpdate");
            _internalVelocity = _lastVelocity;
            Vector3 groundNorm = Motor.GroundingStatus.GroundNormal;
            _internalVelocity = Vector3.ProjectOnPlane(_internalVelocity, groundNorm).normalized;
            _internalVelocity *= (_lastVelocity - _internalVelocity).magnitude * LandingReduction * Mathf.Clamp01(Vector3.Dot(_lastVelocity, groundNorm) * -1f);
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
            _internalVelocity *= 0.5f;
        }
        else
        {
            //Facing the same direction
            _internalVelocity *= 1f/ (1f + InternalDrag * deltaTime);
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
                        currentVelocity += Motor.GroundingStatus.GroundNormal.normalized * Mathf.Sqrt(2f * JumpHeight * Gravity);
                    }

                    _jumpReq = false;
                    _jumpCooldownTimer = 0f;
                    TransitionToState(PlayerState.Air);
                    Motor.ForceUnground();
                }
            }
        }

        //Update Running
        if (_runActive && _currentMoveSpeed <= _targetMoveSpeed)
        {
            _currentMoveSpeed = Mathf.Lerp(_currentMoveSpeed, _targetMoveSpeed, deltaTime * RunStartup); //Startup slow to emphasize momentum
        }
        else if (_currentMoveSpeed > _targetMoveSpeed)
        {
            _currentMoveSpeed = Mathf.Lerp(_currentMoveSpeed, _targetMoveSpeed, deltaTime * RunSlowdown); //Slowdown quick to prevent too much sliding and run glitching
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
        //Update running
        if (_runReq) {
            if (Motor.GroundingStatus.IsStableOnGround) {
                //start running
                StartRunning();
            }
        }

		Vector3 targetVelocity = Vector3.zero;

		//Reorient velocity to surface normal
        Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
        if (currentVelocity.sqrMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
        {
            Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
            if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0) {
                effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
            } else {
                effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
            }
        }
        currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocity.magnitude;

        targetVelocity = transform.TransformDirection(_moveInput);
        Vector3 inputRight = Vector3.Cross(targetVelocity, Motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInput.magnitude;
        targetVelocity = reorientedInput * _currentMoveSpeed;

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-MoveSharpness * deltaTime));
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, GroundMoveMaxSpeed);

        //Add in internal velocity
        if (_internalVelocity.sqrMagnitude >= 0f)
        {
            currentVelocity += _internalVelocity;
        }
	}

    private void AirMove(ref Vector3 currentVelocity, float deltaTime)
    {
        Vector3 targetVelocity = transform.TransformDirection(_moveInput) * _moveInput.magnitude;
        //Only apply additional acceleration if below a certain threshold
        if (new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude < AirMoveMaxEffectiveSpeed) {
            currentVelocity += targetVelocity * AirMoveSpeed * deltaTime;
        } else if (Vector3.Dot(new Vector3(currentVelocity.x, 0f, currentVelocity.z), targetVelocity) < 0f && _moveInput.sqrMagnitude > 0f) {
            currentVelocity += targetVelocity * AirMoveSpeed * deltaTime;
        }

        if (_internalVelocity.sqrMagnitude >= 0f)
        {
            currentVelocity += _internalVelocity;
        }

        Vector3 velNoGrav = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        velNoGrav = Vector3.ClampMagnitude(velNoGrav, AirMoveMaxSpeed);
        velNoGrav.y = currentVelocity.y;
        currentVelocity = velNoGrav;
        currentVelocity += Vector3.down * Gravity * deltaTime;
        /*if (Motor.GroundingStatus.FoundAnyGround)
        {
            Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, perpendicularObstructionNormal);
        }*/
        currentVelocity *= (1f / (1f + (AirDrag * deltaTime)));
	}

    private void StartRunning() 
    {
        _runReq = false;
        _runActive = true;
        _targetMoveSpeed = GroundRunSpeed;
    }

    private void StopRunning() 
    {
        _runActive = false;
        _targetMoveSpeed = GroundMoveSpeed;

        //Transfer run momentum into internal velocity
        if (Motor.GroundingStatus.FoundAnyGround)
            _internalVelocity += _lastVelocity * LandingReduction;
        
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

    private List<Vector3> CheckSurroundings()
    {
        Debug.Log("Checking Surroundings");
        //Sphere cast surrounding the player and return all hit normals
        List<Vector3> hitNormals = new List<Vector3>();

        RaycastHit[] hits = Physics.SphereCastAll(transform.position + Motor.CharacterUp * Motor.Capsule.height,
                                                  Motor.Capsule.radius * 1.1f,
                                                  -Motor.CharacterUp,
                                                  Motor.Capsule.height
                                                 );
        if (hits.Length > 0) {
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.gameObject.layer.Equals(LayerMask.NameToLayer("Player"))) continue;
                if (DebugLog) Debug.DrawLine(hits[i].point, hits[i].point + hits[i].normal * 0.1f, Color.blue, 5f);
                if (hits[i].point != Vector3.zero) hitNormals.Add(hits[i].normal);
            }
        } else {
            Debug.Log("Hit Nothing");
        }

        return hitNormals;
    }

    public void OnGUI()
    {
        if (!DebugLog) return;

        GUILayout.Label(string.Format("Player State: {0}", CurrentState.ToString()));
        GUILayout.Label(string.Format("Velocity: {0}, Internal Velocity: {1}", Motor.Velocity, _internalVelocity));
        GUILayout.Label(string.Format("Current Move Speed: {0}, Target Move Speed: {1}", _currentMoveSpeed, _targetMoveSpeed));
        GUILayout.Label(string.Format("Running: {0}", _runActive));

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
        float percent = (_currentMoveSpeed - GroundMoveSpeed)/ (GroundRunSpeed - GroundMoveSpeed);
        PlayerCamera.fieldOfView = BaseFOV + Mathf.Lerp(MinFOVAdd, MaxFOVAdd, EasingFunction.EaseInOutQuad(0f, 1f, percent));
    }
}
