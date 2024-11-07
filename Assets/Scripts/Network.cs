using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Network {
    #region Fields
    private Client _client;
    private SemaphoreSlim _networkLock = new(1, 1);

    public string serverAddr = "127.0.0.1";
    public int serverPort = 33390;
    #endregion

    #region Connection Management
    private async Task Connect(string address, int port) {
        _client = new Client(address, port);
        await _client.ConnectAsync();
    }

    public void Close() {
        _networkLock?.Dispose();
        _networkLock = new(1, 1);
        _client?.Close();
        _client = null;
    }

    public async Task<bool> CheckConnected() {
        try {
            var result = await SendRequest(ActionTypeRequest.Ping);
            return result >= 0;
        } catch {
            return false;
        }
    }
    #endregion

    #region Network Operations
    private async Task<(int, string)> SendPayloadRequest(ActionTypeRequest action) {
        var packet = CreatePacket(true, (byte)action);
        return await SendPacketWithPayload(packet);
    }

    private async Task<int> SendPayloadEvent(ActionTypeEvent action, string msg) {
        var packet = CreatePacket(false, (byte)action, msg);
        var (payload, _) = await SendPacketWithPayload(packet);
        return payload;
    }

    private async Task<int> SendRequest(ActionTypeRequest action) {
        var packet = CreatePacket(true, (byte)action);
        return await SendPacketWithConnection(packet);
    }

    private async Task<int> SendEvent(ActionTypeEvent action) {
        var packet = CreatePacket(false, (byte)action);
        return await SendPacketWithConnection(packet);
    }

    private async Task<(int, string)> SendPacketWithPayload(List<byte> packet) {
        if (_client == null || !_client.IsConnected()) {
            await Connect(serverAddr, serverPort);
        }
        if (!_client.IsConnected()) return (-1, string.Empty);

        try {
            await _networkLock.WaitAsync();
            byte[] response = await _client.SendMessageWithResponse(packet);
            
            if (response == null || response.Length == 0) 
                return (-1, string.Empty);
            
            int status = ParseResponse(response);
            string payload = string.Empty;
            if (response.Length > 1) {
                payload = System.Text.Encoding.UTF8.GetString(response, 1, response.Length - 1);
                payload = payload.TrimEnd('\n', '\r');
            }

            return (status, payload);
        } catch {
            return (-1, string.Empty);
        } finally {
            _networkLock.Release();
        }
    }

    private async Task<int> SendPacketWithConnection(List<byte> packet) {
        if (_client == null || !_client.IsConnected()) {
            await Connect(serverAddr, serverPort);
        }
        if (!_client.IsConnected()) return -1;

        try {
            await _networkLock.WaitAsync();
            byte[] response = await _client.SendMessageWithResponse(packet);
            return response == null || response.Length == 0 ? -1 : ParseResponse(response);
        } catch {
            return -1;
        } finally {
            _networkLock.Release();
        }
    }

    private static List<byte> CreatePacket(bool isRequest, byte action, string payload = "") {
        var packet = new List<byte> {
            (byte)((isRequest ? 0 : 1 << 7) | (action & 0x3F))
        };

        if (!string.IsNullOrEmpty(payload)) {
            packet.AddRange(System.Text.Encoding.UTF8.GetBytes(payload));
        }
        packet.Add((byte)'\n');
        return packet;
    }

    private static int ParseResponse(byte[] response) {
        return (response[0] & (1 << 6)) != 0 ? 1 : 0;
    }
    #endregion

    #region Public API
    // Buffer Operations
    public async Task<int> IsReplayBufferActive() => await SendRequest(ActionTypeRequest.ReplayBufferActive);
    public async Task<int> StartReplayBuffer() => await SendEvent(ActionTypeEvent.StartReplayBuffer);
    public async Task<int> StopReplayBuffer() => await SendEvent(ActionTypeEvent.StopReplayBuffer);
    public async Task<int> SaveBuffer() => await SendEvent(ActionTypeEvent.SaveReplayBuffer);

    // Recording Operations
    public async Task<int> IsRecordingActive() => await SendRequest(ActionTypeRequest.RecordingActive);
    public async Task<int> StartRecording() => await SendEvent(ActionTypeEvent.StartRecording);
    public async Task<int> StopRecording() => await SendEvent(ActionTypeEvent.StopRecording);
    public async Task<int> SplitRecording() => await SendEvent(ActionTypeEvent.RecordingSplitFile);

    // Streaming Operations
    public async Task<int> IsStreamingActive() => await SendRequest(ActionTypeRequest.StreamingActive);
    public async Task<int> StartStreaming() => await SendEvent(ActionTypeEvent.StartStreaming);
    public async Task<int> StopStreaming() => await SendEvent(ActionTypeEvent.StopStreaming);

    // Scene Operations
    public async Task<(int, string)> GetCurrentScene() => await SendPayloadRequest(ActionTypeRequest.GetCurrentScene);
    public async Task<(int, string)> GetScenes() => await SendPayloadRequest(ActionTypeRequest.GetScenes);
    public async Task<int> SetScene(string msg) => await SendPayloadEvent(ActionTypeEvent.SetScene, msg);
    #endregion
}