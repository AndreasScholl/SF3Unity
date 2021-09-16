using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputData
{
    public float Horizontal;
    public float Vertical;
    public float LookHorizontal;
    public float LookVertical;
    public bool Button1;
    public bool Button2;
    public bool Button3;
    public bool Button4;
    public bool Button5;
    public bool Button6;
    public bool Button1Down;
    public bool Button2Down;
    public bool Button3Down;
    public bool Button4Down;
    public bool Button5Down;
    public bool Button6Down;
    public float CameraAngle;

    public void Reset()
    {
        Horizontal = 0f;
        Vertical = 0f;
        LookHorizontal = 0f;
        LookVertical = 0f;
        Button1 = false;
        Button2 = false;
        Button3 = false;
        Button4 = false;
        Button5 = false;
        Button6 = false;
        Button1Down = false;
        Button2Down = false;
        Button3Down = false;
        Button4Down = false;
        Button5Down = false;
        Button6Down = false;
        CameraAngle = 0f;
    }
}

public class InputHelper
{
    public static void ReadInput(InputData inputData)
    {
        inputData.Reset();

        Gamepad pad = Gamepad.current;

        if (pad != null)
        {
            Vector2 leftStick = pad.leftStick.ReadValue();
            inputData.Horizontal = leftStick.x;
            inputData.Vertical = leftStick.y;

            inputData.Button1 = pad.buttonSouth.isPressed;                  // jump
            inputData.Button1Down = pad.buttonSouth.wasPressedThisFrame;
            if (pad.buttonNorth.isPressed)
            {
                inputData.Button1 = true;
            }
            if (pad.buttonNorth.wasPressedThisFrame)
            {
                inputData.Button1Down = true;
            }
            inputData.Button2 = pad.buttonEast.isPressed;                   // fire
            inputData.Button2Down = pad.buttonEast.wasPressedThisFrame;
            if (pad.buttonWest.isPressed)
            {
                inputData.Button2 = true;
            }
            if (pad.buttonWest.wasPressedThisFrame)
            {
                //Debug.Break();
                inputData.Button2Down = true;
            }

            inputData.Button5Down = pad.leftShoulder.wasPressedThisFrame;   // look left
            inputData.Button6Down = pad.rightShoulder.wasPressedThisFrame;  // look right
            Vector2 rightStick = pad.rightStick.ReadValue();                // free rotate camera
            inputData.LookHorizontal = rightStick.x;
            inputData.LookVertical = rightStick.y;
        }

        Joystick stick = Joystick.current;

        if (stick != null)
        {
            inputData.Horizontal = stick.stick.x.ReadValue();
            inputData.Vertical = stick.stick.y.ReadValue();

            foreach (InputControl control in stick.allControls)
            {
                if (control is ButtonControl)
                {
                    ButtonControl button = (ButtonControl)control;

                    if (button.name == "button2")
                    {
                        inputData.Button1 = button.isPressed;                  // jump
                        inputData.Button1Down = button.wasPressedThisFrame;
                    }
                    else if (button.name == "button5")
                    {
                        inputData.Button5Down = button.wasPressedThisFrame; // look left
                    }
                    else if (button.name == "button6")
                    {
                        inputData.Button6Down = button.wasPressedThisFrame; // look right
                    }
                }
                else if (control is AxisControl)
                {
                    AxisControl axis = (AxisControl)control;

                    // free rotate camera
                    if (axis.name == "rx")
                    {
                        inputData.LookVertical = axis.ReadValue();
                    }
                    if (axis.name == "z")
                    {
                        inputData.LookHorizontal = axis.ReadValue();
                    }
                }
            }

            inputData.Button2 = stick.trigger.isPressed;                   // fire
            inputData.Button2Down = stick.trigger.wasPressedThisFrame;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.rightArrowKey.isPressed)
            {
                inputData.Horizontal = 1f;
            }
            else if (keyboard.leftArrowKey.isPressed)
            {
                inputData.Horizontal = -1f;
            }
            if (keyboard.downArrowKey.isPressed)
            {
                inputData.Vertical = -1f;
            }
            else if (keyboard.upArrowKey.isPressed)
            {
                inputData.Vertical = 1f;
            }
            if (keyboard.aKey.isPressed || keyboard.yKey.isPressed || keyboard.zKey.isPressed)
            {
                inputData.Button1 = true;                   // jump
            }
            if (keyboard.aKey.wasPressedThisFrame || keyboard.yKey.wasPressedThisFrame || keyboard.zKey.wasPressedThisFrame)
            {
                inputData.Button1Down = true;
            }
            if (keyboard.sKey.isPressed || keyboard.xKey.isPressed)
            {
                inputData.Button2 = true;                   // fire
            }
            if (keyboard.sKey.wasPressedThisFrame || keyboard.xKey.wasPressedThisFrame)
            {
                inputData.Button2Down = true;
            }
            if (keyboard.qKey.wasPressedThisFrame)
            {
                inputData.Button5Down = true;               // look left
            }
            if (keyboard.wKey.wasPressedThisFrame)
            {
                inputData.Button6Down = true;               // look right
            }
            if (keyboard.qKey.isPressed)
            {
                inputData.Button5 = true;                   // look left
            }
            if (keyboard.wKey.isPressed)
            {
                inputData.Button6 = true;                   // look right
            }

            if (keyboard.ctrlKey.isPressed)
            {
                inputData.LookHorizontal = inputData.Horizontal;
                inputData.LookVertical = inputData.Vertical;
                inputData.Horizontal = 0f;
                inputData.Vertical = 0f;
            }
        }

        //inputData.Horizontal = Input.GetAxisRaw("Horizontal");
        //inputData.Vertical = Input.GetAxisRaw("Vertical");
        //inputData.Button1 = Input.GetButton("Fire1");
        //inputData.Button1Down = Input.GetButtonDown("Fire1");
        //inputData.Button2 = Input.GetButton("Fire2");
        //inputData.Button2Down = Input.GetButtonDown("Fire2");
        //inputData.Button3 = Input.GetButton("Fire3");
        //inputData.Button3Down = Input.GetButtonDown("Fire3");
        //inputData.Button4Down = Input.GetButtonDown("Fire4");
        //inputData.Button5Down = Input.GetButtonDown("Fire5");
        //inputData.Button6Down = Input.GetButtonDown("Fire6");
        //inputData.CameraAngle = Camera.main.transform.eulerAngles.y;

        //lookHorizontal = Input.GetAxisRaw("LookHorizontal");
        //lookVertical = Input.GetAxisRaw("LookVertical");

        inputData.CameraAngle = Camera.main.transform.eulerAngles.y;
    }

    public static bool GetMenuButtonDown(InputData inputData)
    {
        bool menuButtonDown = false;

        if (inputData.Button1Down)
        {
            menuButtonDown = true;
        }
        if (inputData.Button2Down)
        {
            menuButtonDown = true;
        }

        return menuButtonDown;
    }
}
