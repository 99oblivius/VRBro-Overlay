using System;
using System.Collections.Generic;
using System.Text;


public enum ActionTypeRequest : byte {
    Ping = 0,
    ReplayBufferActive,
    RecordingActive,
    StreamingActive,
    GetCurrentScene,
    GetScenes,
}
public enum ActionTypeEvent : byte {
    Ping = 0,
    StartReplayBuffer,
    StopReplayBuffer,
    SaveReplayBuffer,
    StartRecording,
    StopRecording,
    StartStreaming,
    StopStreaming,
    RecordingSplitFile,
    SetScene,
}

public static class BinaryOperation {
    public static byte[] CreatePacket(bool isPost, bool isBinary, byte actionType, string payload = "") {
        List<byte> packet = new();
        byte header = CreateHeader(isPost, isBinary, actionType);
        packet.Add(header);

        if (!isBinary && !string.IsNullOrEmpty(payload)) {
            packet.AddRange(Encoding.UTF8.GetBytes(payload));
        }
        return packet.ToArray();
    }

    public static (bool isPost, bool isBinary, byte actionType, string payload) ParsePacket(byte[] packet) {
        if (packet == null || packet.Length == 0) {
            throw new ArgumentException("Packet is empty");
        }

        byte header = packet[0];
        bool isPost = (header & 0x80) != 0;
        bool isBinary = (header & 0x40) != 0;
        byte actionType = (byte)(header & 0x3F);

        string payload = string.Empty;
        if (!isBinary && packet.Length > 1) {
            payload = Encoding.UTF8.GetString(packet, 1, packet.Length - 1);
        }

        return (isPost, isBinary, actionType, payload);
    }

    private static byte CreateHeader(bool isPost, bool isBinary, byte actionType) {
        byte header = 0;
        header |= (byte)((isPost ? 1 : 0) << 7);
        header |= (byte)((isBinary ? 1 : 0) << 6);
        header |= actionType;
        return header;
    }
}
