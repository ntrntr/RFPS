using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XboxCtrlrInput;

[RequireComponent(typeof(PlayerControllerV2))]
public class PlayerInputV2 : MonoBehaviour {

	public PlayerControllerV2 Controller;

    [Header("Input Settings")]
    public bool invertVertical = true;

	public void Start()
	{
		Debug.Assert(Controller != null);
	}

	public void Update()
	{
		PlayerInputs inputs;

		inputs.moveHorizontal = XCI.GetAxis(XboxAxis.LeftStickX);
		inputs.moveVertical = XCI.GetAxis(XboxAxis.LeftStickY);

		inputs.jump = XCI.GetButtonDown(XboxButton.A);
		inputs.boost = XCI.GetButtonDown(XboxButton.B);

        inputs.lookVertical = XCI.GetAxis(XboxAxis.RightStickY);
        inputs.lookVertical *= invertVertical ? -1f : 1f;
        inputs.lookHorizontal = XCI.GetAxis(XboxAxis.RightStickX);

		Controller.SetPlayerInputs(inputs);
	}
}
