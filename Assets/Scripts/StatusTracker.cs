using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class StatusTracker : MonoBehaviour
{
    [SerializeField] private VRBro VRBro;
    [SerializeField] private Image bufferIndicator;
    [SerializeField] private Image recordingIndicator;
    [SerializeField] private Image streamingIndicator;
    
    private static readonly Color32 ActiveColor = new(116, 132, 117, 255);   // Green
    private static readonly Color32 InactiveColor = new(132, 117, 127, 255); // Red
    private static readonly Color32 PendingColor = new(132, 132, 117, 255);  // Yellow-ish

    private async void Start()
    {
        await Task.Delay(1000);
        
        bufferIndicator.material.SetFloat("_WaveAmplitude", VRBro.bufferActive ? 0.5f : 0.0f);
        recordingIndicator.color  = VRBro.recordingActive  ? ActiveColor : InactiveColor;
        streamingIndicator.color  = VRBro.streamingActive  ? ActiveColor : InactiveColor;
    }

    public async Task<bool> StartReplayBuffer() {
        VRBro.startBuffer = true;

        for (int i = 0; i < 12; i++) {
            int status = await VRBro.IsReplayBuffer();
            if (status == 1) {
                bufferIndicator.material.SetFloat("_WaveAmplitude", 0.5f);
                return true;
            }
            await Task.Delay(250);
        }
        return false;
    }

    public async Task<bool> StopReplayBuffer() {
        VRBro.stopBuffer = true;

        for (int i = 0; i < 12; i++) {
            int status = await VRBro.IsReplayBuffer();
            if (status != 1) {
                bufferIndicator.material.SetFloat("_WaveAmplitude", 0.0f);
                return true;
            }
            await Task.Delay(400);
        }
        return false;
    }

    public async Task<bool> StartRecording() {
        VRBro.startRecording = true;
        recordingIndicator.color = PendingColor;

        for (int i = 0; i < 12; i++) {
            int status = await VRBro.IsRecording();
            if (status == 1) {
                recordingIndicator.color = ActiveColor;
                return true;
            }
            await Task.Delay(250);
        }
        
        recordingIndicator.color = InactiveColor;
        return false;
    }

    public async Task<bool> StopRecording() {
        VRBro.stopRecording = true;
        recordingIndicator.color = PendingColor;

        for (int i = 0; i < 20; i++) {
            int status = await VRBro.IsRecording();
            if (status != 1) {
                recordingIndicator.color = InactiveColor;
                return true;
            }
            await Task.Delay(500);
        }
        
        recordingIndicator.color = ActiveColor;
        return false;
    }

    public async Task<bool> StartStreaming() {
        VRBro.startStreaming = true;
        streamingIndicator.color = PendingColor;

        for (int i = 0; i < 12; i++) {
            int status = await VRBro.IsStreaming();
            if (status == 1) {
                streamingIndicator.color = ActiveColor;
                return true;
            }
            await Task.Delay(250);
        }
        
        streamingIndicator.color = InactiveColor;
        return false;
    }

    public async Task<bool> StopStreaming() {
        VRBro.stopStreaming = true;
        streamingIndicator.color = PendingColor;

        for (int i = 0; i < 10; i++) {
            int status = await VRBro.IsStreaming();
            if (status != 1) {
                streamingIndicator.color = InactiveColor;
                return true;
            }
            await Task.Delay(500);
        }
        
        streamingIndicator.color = ActiveColor;
        return false;
    }
}