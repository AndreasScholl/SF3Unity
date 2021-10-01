using UnityEngine;
using UnityEngine.InputSystem;

public class SoundTest : MonoBehaviour
{
    int _voiceNumber = 0;

    public float _pitch = 0.5f;

    public string testPostfix = "";

    public bool _test = true;

    void Start()
    {
    }

    private void PlaySound(int layer = 0, bool loop = false)
    {
        AudioPlayer.GetInstance().PlaySound(_voiceNumber.ToString() + "_" + layer + testPostfix, loop, _pitch);

        Debug.Log("SoundTest: " + _voiceNumber + "_" + layer);
    }
     
    void Update()
    {
        if (_test == false)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard.f1Key.wasPressedThisFrame)
        {
            PlaySound();
        }

        if (keyboard.f2Key.wasPressedThisFrame)
        {
            _voiceNumber++;
            PlaySound();
        }

        if (keyboard.f3Key.wasPressedThisFrame)
        {
            _voiceNumber--;
            PlaySound();
        }

        if (keyboard.f4Key.wasPressedThisFrame)
        {
            PlaySound(1);
        }

        if (keyboard.f5Key.wasPressedThisFrame)
        {
            PlaySound(0, true);
        }
    }
}
