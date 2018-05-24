using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

namespace Player
{
	[RequireComponent(typeof(PlayerController))]
	public class PlayerInput : MonoBehaviour
	{

		public enum InputSource
		{
			Controller,
			MouseKeyboard
		}

		public PlayerController PController;

		[SerializeField]
		private InputSource inputSource = InputSource.MouseKeyboard;

		public void Start()
		{
			if (PController == null) {
				PController = GetComponent<PlayerController>();
			}
			Debug.Assert(PController != null, "Failed to find player controller");
			//Double check to make sure input source is viable
			switch (inputSource) {
				case InputSource.Controller:
					Debug.Log("Controller Input active");
					break;
				case InputSource.MouseKeyboard:
					Debug.Log("Mouse/Keyboard Input active");
					break;
			}

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		public void Update()
		{
			PlayerInputs inputs = new PlayerInputs();

			switch (inputSource)
			{
				case InputSource.Controller:
					{
						//TODO add controller support
						break;
					}
				case InputSource.MouseKeyboard:
					{
						inputs.forwardMove = Input.GetAxisRaw("Vertical");
						inputs.rightMove = Input.GetAxisRaw("Horizontal");
						inputs.jump = Input.GetKeyDown(KeyCode.Space);
						inputs.boost = Input.GetKeyDown(KeyCode.LeftShift);
						inputs.horizontalLook = Input.GetAxis("Mouse X");
						inputs.verticalLook = Input.GetAxis("Mouse Y");
						break;
					}
			}

			PController.SetInputs(inputs);
		}
	}
}