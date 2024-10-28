using UnityEngine;
using UnityEngine.UI;

public class VRBroSettings : MonoBehaviour
{
    [SerializeField] private VRBro VRBro;
    [SerializeField] private Image imageSaveBuffer;
    [SerializeField] private Image imageStartStopBuffer;

    public void OnEnableButtonClick() {
        VRBro.active = true;
        imageSaveBuffer.color = new Color32(116, 132, 117, 255);
    }

    public void OnDisableButtonClick() {
        VRBro.active = false;
        imageSaveBuffer.color = new Color32(132, 117, 127, 255);
    }

    public void OnStartBufferButtonClick() {
        VRBro.startbuffer = true;
    }

    public void OnStopBufferButtonClick() {
        VRBro.stopbuffer = true;
    }
}