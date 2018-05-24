using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace Player
{
	public enum PlayerState
	{
		Default,
		OnWall,
		Slide,
		Clamber,
	}

	public struct PlayerInputs
	{
		public float forwardMove;
		public float rightMove;
		public float horizontalLook;
		public float verticalLook;
		public bool jump;
		public bool boost;
		public bool slide;
	}

	public class PlayerController : BaseCharacterController
	{
		public PlayerState CurrentState;
		public bool DebugStats;

		[Header("GameObejct References")]
		public GameObject Head;

		[Header("Movement Settings")]
		public float GroundMaxMoveSpeed = 5f;
		public float GroundMinMoveSpeed = 3f;
		public float AirMaxMoveSpeed = 3f;
		public float AirMinAccelerationSpeed = 5f;
		public float AirMaxAccelerationSpeed = 5f;
		public float FluiditySlowDownTime = 3f;
		public float FluiditySpeedUpTime = 1f;
		public float StableMovementSharpness = 15f;
		public float MinWallRunSpeed = 2f;
		public float MaxWallDisconnectTime = 1f;
		public Vector3 Gravity = Vector3.down * 30f;
		public float Drag = 0.1f;
		public float JumpSpeed = 5f;
		public float WallJumpSpeed = 10f;
		public float WallJumpExpiryTime = 0.5f;
		public float JumpExpiryTime = 0.5f;
		public float JumpCooldown = 0.5f;
		public float HorizontalLookSpeed = 5f;
		public float VerticalLookSpeed = 10f;
		public float LookSharpness = 15f;
		public float VerticalMinAngle = -80f;
		public float VerticalMaxAngle = 80f;
		public float FOVMin = 0f;
		public float FOVMax = 20f;
		public float WallRunMinMoveSpeed;
		public float WallRunMaxMoveSpeed;

		[Header("Easing Functions")]
		public EasingFunction.Ease FOVEasing;
		public EasingFunction.Ease MoveSpeedEasing;
		public EasingFunction.Ease AirSpeedEasing;

		[Header("Boost Settings")]
		public float BoostCooldown;
		public float BoostSpeed;
		public bool BoostAdditive;
		public bool BoostCancelVelocity;

		private Vector3 _lookInputVector;
		private Vector3 _moveInputVector;
		private bool _jumpReq;
		private float _jumpReqTimer;
		private float _jumpCooldownTimer;
		private bool _canWallJump;
		private float _canWallJumpTimer;
		private Vector3 _wallNormal;
		private float _wallContactTimer;
		private bool _boostReq;
		private float _boostTimer;
		private Vector3 _internalVelocityAdd;
		private float _currentFluidity; //Value between 0 and 1 representing fluidity
		private float _fluiditySpeedUpFreq;
		private float _fluiditySlowDownFreq;
		private Camera _camera;
		private float _baseFOV;

		public void StateTransition(PlayerState state)
		{
			Debug.Log("Transitioning to " + state.ToString());
			PlayerState prvState = CurrentState;
			OnStateExit(prvState, state);
			CurrentState = state;
			OnStateEnter(state, prvState);
		}

		public void OnStateEnter(PlayerState state, PlayerState fromState)
		{
			switch (state)
			{
				case PlayerState.Default:
					{
						break;
					}
			}
		}

		public void OnStateExit(PlayerState state, PlayerState toState)
		{
			switch (state)
			{
				case PlayerState.Default:
					{
						break;
					}
			}
		}

		public void SetInputs(PlayerInputs inputs)
		{
			// Movement
			Vector3 movementInput = Vector3.ClampMagnitude(new Vector3(inputs.rightMove, 0f, inputs.forwardMove), 1f);

			// Aiming
			Vector3 lookInput = new Vector3(0f, inputs.horizontalLook, 0f);

			//Vertical aim goes straight to camera
			Vector3 headEuler = Head.transform.localRotation.eulerAngles;
			headEuler += new Vector3(inputs.verticalLook, 0f, 0f) * VerticalLookSpeed;
			headEuler.x = ClampAngle(headEuler.x, VerticalMinAngle, VerticalMaxAngle);
			Head.transform.localRotation = Quaternion.Euler(headEuler);

			// Boost can happen in any state
			if (inputs.boost)
			{
				_boostReq = true;
			}

            //Jumping
			if (inputs.jump)
            {
                _jumpReq = true;
                _jumpReqTimer = 0f;
            }

			switch (CurrentState)
			{
				case PlayerState.Default:
					{
						
						break;
					}
			}

			_moveInputVector = movementInput;
			_moveInputVector = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f) * _moveInputVector;
			_lookInputVector = lookInput;
		}

		private static float ClampAngle(float angle, float min, float max)
		{
			if (angle > 180f)
				angle -= 360f;
			return Mathf.Clamp(angle, min, max);
		}

		public void Start()
		{
			//Start in default state
			StateTransition(PlayerState.Default);

			Debug.Assert(Head != null, "Couldn't find head");
			_camera = GameObject.Find("Main Camera").GetComponent<Camera>();
			Debug.Assert(_camera != null);
			_baseFOV = _camera.fieldOfView;

			//Some sanity checks
			Debug.Assert(FluiditySpeedUpTime > 0f);
			Debug.Assert(FluiditySlowDownTime > 0f);

			_fluiditySpeedUpFreq = 1f / FluiditySpeedUpTime;
			_fluiditySlowDownFreq = 1f / FluiditySlowDownTime;
		}

		public void Update()
		{
			_camera.fieldOfView = _baseFOV + Mathf.Lerp(FOVMin, FOVMax, EasingFunction.GetEasingFunction(FOVEasing).Invoke(0f, 1f, _currentFluidity));
		}

		public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
		{
			Quaternion targetRotation = Quaternion.identity;
			switch (CurrentState)
			{
				case PlayerState.Default:
					{
						Vector3 euler = currentRotation.eulerAngles;
						euler += _lookInputVector * HorizontalLookSpeed;
						targetRotation = Quaternion.Euler(euler);
						break;
					}
				case PlayerState.Clamber:
					{

						break;
					}
				case PlayerState.OnWall:
					{
						Vector3 euler = currentRotation.eulerAngles;
						euler += _lookInputVector * HorizontalLookSpeed;
						targetRotation = Quaternion.Euler(euler);
						break;
					}
				case PlayerState.Slide:
					{

						break;
					}
			}

			currentRotation = Quaternion.Lerp(currentRotation, targetRotation, 1 - Mathf.Exp(-LookSharpness * deltaTime));
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			UpdateFluidity(deltaTime);

			// Boost can be performed in any state
			if (_boostReq)
			{
				if (_boostTimer >= 0f)
				{
					//Boost is available
					if (_moveInputVector.sqrMagnitude > 0.01f)
					{
						// Move dir
						currentVelocity += _moveInputVector.normalized * BoostSpeed - Vector3.Project(currentVelocity, Motor.CharacterUp);
					}
					else
					{
						// Look dir
						currentVelocity += Motor.CharacterForward * BoostSpeed - Vector3.Project(currentVelocity, Motor.CharacterUp);
					}
					_boostTimer = -BoostCooldown;
				}
				else
				{
					//Boost is cooling down
					//TODO: feedback for boost cooldown
				}
				_boostReq = false;
			}

			switch (CurrentState)
			{
				case PlayerState.Default:
					{
						Vector3 targetMovementVelocity = Vector3.zero;

						//Handle movement
						if (Motor.GroundingStatus.IsStableOnGround)
						{
							//Grounded movement
							Vector3 groundNorm = Motor.GroundingStatus.GroundNormal;
							if (currentVelocity.sqrMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
							{
								Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
								if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
								{
									groundNorm = Motor.GroundingStatus.OuterGroundNormal;
								}
								else
								{
									groundNorm = Motor.GroundingStatus.InnerGroundNormal;
								}
							}

							//Reorient to ground normal for sloped movement
							currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, groundNorm) * currentVelocity.magnitude;

							EasingFunction.Function func = EasingFunction.GetEasingFunction(MoveSpeedEasing);
							float groundMoveSpeed = Mathf.Lerp(GroundMinMoveSpeed, GroundMaxMoveSpeed, func(0f, 1f, _currentFluidity));
							Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
							Vector3 reorientedInput = Vector3.Cross(groundNorm, inputRight).normalized * _moveInputVector.magnitude;
							targetMovementVelocity = reorientedInput * groundMoveSpeed;

							currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
						}
						else
						{
							//Air movement
							EasingFunction.Function func = EasingFunction.GetEasingFunction(AirSpeedEasing);
							float airMoveSpeed = Mathf.Lerp(AirMinAccelerationSpeed, AirMaxAccelerationSpeed, func(0f, 1f, _currentFluidity));
							targetMovementVelocity = _moveInputVector * AirMaxMoveSpeed;

							if (Motor.GroundingStatus.FoundAnyGround)
							{
								Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
								targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
							}

							Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
							currentVelocity += velocityDiff * airMoveSpeed * deltaTime;
							currentVelocity += Gravity * deltaTime;
							currentVelocity *= (1f / (1f + (Drag * deltaTime)));
						}

						//Handle jumping
						if (_jumpReq && _jumpCooldownTimer >= JumpCooldown)
						{
							if (_jumpReqTimer >= JumpExpiryTime)
								_jumpReq = false;
							else
							{
								if (Motor.GroundingStatus.IsStableOnGround)
								{ //Perform a normal jump
									Vector3 jumpDirection = Vector3.up;
									Motor.ForceUnground();
									currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
									_jumpReq = false;
									_canWallJump = false;
								}
								else if (Motor.GroundingStatus.FoundAnyGround)
								{ //Perform a sloped jump
									Vector3 jumpDirection = (Motor.GroundingStatus.GroundNormal + Vector3.up).normalized;
									Motor.ForceUnground();
									currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
									_jumpReq = false;
									_canWallJump = false;
								}
								else if (_canWallJump)
								{ //Perform a wall jump
									Vector3 jumpDirection = (Vector3.up + _wallNormal).normalized;
									currentVelocity += (jumpDirection * WallJumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
									_jumpReq = false;
									_canWallJump = false;
								}

								if (!_jumpReq)
									_jumpCooldownTimer = 0f;
							}
						}

						if (_internalVelocityAdd.sqrMagnitude > 0f)
						{
							currentVelocity += _internalVelocityAdd;
							_internalVelocityAdd = Vector3.zero;
						}

						break;
					}
				case PlayerState.Clamber:
					{
						break;
					}
				case PlayerState.OnWall:
					{
						Vector3 targetMovementVelocity = Vector3.zero;
						if (currentVelocity.sqrMagnitude <= MinWallRunSpeed * MinWallRunSpeed)
						{ //Square space
						  //Don't have enough velocity, transition to falling
							StateTransition(PlayerState.Default);
							return;
						}

						Vector3 planeNorm = _wallNormal;
						currentVelocity = currentVelocity - (Vector3.Dot(currentVelocity, planeNorm) * planeNorm);

						EasingFunction.Function func = EasingFunction.GetEasingFunction(MoveSpeedEasing);
						float wallMoveSpeed = Mathf.Lerp(WallRunMinMoveSpeed, WallRunMaxMoveSpeed, func(0f, 1f, _currentFluidity));
						Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
						Vector3 reorientedInput = Vector3.Cross(Vector3.up, inputRight).normalized * _moveInputVector.magnitude;
						targetMovementVelocity = currentVelocity.normalized * reorientedInput.magnitude * wallMoveSpeed;
						targetMovementVelocity = Quaternion.Euler(Head.transform.localRotation.eulerAngles.x, 0f, 0f) * targetMovementVelocity;

						Debug.Log(targetMovementVelocity);
						Debug.DrawLine(Head.transform.position, Head.transform.position + targetMovementVelocity * 10f, Color.red, 2f);

						currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, deltaTime);

						if (_jumpReq && _jumpCooldownTimer >= JumpCooldown)
						{
							if (_jumpReqTimer >= JumpExpiryTime)
								_jumpReq = false;
							else
							{
								//Perform a wall jump
                                StateTransition(PlayerState.Default);
								Vector3 jumpDirection = (Vector3.up + _wallNormal).normalized;
								currentVelocity += (jumpDirection * WallJumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
								_jumpReq = false;
								_canWallJump = false;

								if (!_jumpReq)
									_jumpCooldownTimer = 0f;
							}
						}
						break;
					}
				case PlayerState.Slide:
					{

						break;
					}
			}
		}

		public override void AfterCharacterUpdate(float deltaTime)
		{
			//Updating jump req timer
			_jumpReqTimer += deltaTime;
			_jumpCooldownTimer += deltaTime;
			_boostTimer += deltaTime;

			//Wall run timer
			_wallContactTimer += deltaTime;
			if (_wallContactTimer >= MaxWallDisconnectTime && CurrentState == PlayerState.OnWall)
			{
				StateTransition(PlayerState.Default); //Fell off wall
			}

			//Updating wall jump timers
			if (_canWallJump)
			{
				_canWallJumpTimer += deltaTime;
				if (_canWallJumpTimer >= WallJumpExpiryTime)
				{
					_canWallJump = false;
				}
			}
		}

		public override void BeforeCharacterUpdate(float deltaTime)
		{
		}

		public override void HandleMovementProjection(ref Vector3 movement, Vector3 obstructionNormal, bool stableOnHit)
		{
			base.HandleMovementProjection(ref movement, obstructionNormal, stableOnHit);
		}

		public override bool IsColliderValidForCollisions(Collider coll)
		{
			return true;
		}

		public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
		}

		public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
			//Wall jumping
			if (Vector3.Dot(hitNormal, Vector3.up) > 0f)
			{
				_canWallJump = true;
				_canWallJumpTimer = 0f;
				_wallNormal = hitNormal;
			}

			//Wall running
			float wallCheck = Vector3.Dot(hitNormal, Vector3.up);
			if (wallCheck >= 0f && wallCheck <= 0.4f)
			{ //Hit valid wall
				_wallContactTimer = 0f; //Reset timer
				if (CurrentState != PlayerState.OnWall)
				{ //Not currently wall running
					if (!Motor.GroundingStatus.FoundAnyGround)
					{
						float wallDot = Vector3.Dot(hitNormal, Motor.CharacterForward);
						if (wallDot < 0.2f && wallDot > -0.4f)
						{
							//We have run into a wall at an angle and weren't already wall running
							StateTransition(PlayerState.OnWall);
						}
					}
				}
			}
		}

		public override void PostGroundingUpdate(float deltaTime)
		{
		}

		public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
		{
		}

		public void UpdateFluidity(float deltaTime)
		{
			switch (CurrentState)
			{
				case PlayerState.Default:
					{
						//Only punish fluidity if on solid ground
						if (Motor.GroundingStatus.IsStableOnGround)
						{
							//Speed up
							_currentFluidity = Mathf.Clamp01(_currentFluidity + deltaTime * _fluiditySpeedUpFreq);

							if (_moveInputVector.magnitude < 0.1f)
							{ //Slow
								_currentFluidity = Mathf.Clamp01(_currentFluidity - deltaTime * _fluiditySlowDownFreq);
							}
						}
						else
						{
							if (_moveInputVector.magnitude > 0.1f)
							{
								_currentFluidity = Mathf.Clamp01(_currentFluidity + deltaTime * _fluiditySpeedUpFreq);
							}
						}
						break;
					}
			}

		}

		public void OnGUI()
		{
			if (!DebugStats)
				return;

			GUILayout.Label(string.Format("Fluidity: {0}", _currentFluidity));
			GUILayout.Label(string.Format("Current State: {0}", CurrentState.ToString()));
		}
	}
}