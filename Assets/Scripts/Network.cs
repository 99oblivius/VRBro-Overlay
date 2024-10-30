using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;


public class Network
{
    private readonly SemaphoreSlim _networkLock = new(1, 1);
    
    public string serverAddr = "127.0.0.1";
    public int serverPort = 33390;
    private Client _client;

    private async Task Connect(string address, int port) {
        _client = new Client(address, port);
        await _client.ConnectAsync();
    }

    public void Close() {
        _client?.Close();
        _client = null;
        _networkLock?.Dispose();
    }

    public async Task<bool> CheckConnected() {
        try {
            var result = await SendPacket(ActionTypeRequest.Ping);
            return result >= 0;
        } catch {
            return false;
        }
    }

    private async Task<int> SendPacket<T>(T action) where T : Enum {
        if (_client == null || !_client.IsConnected()) await Connect(serverAddr, serverPort);
        if (!_client.IsConnected()) {
            return -1;
        }

        if (action is ActionTypeRequest request) {
            return await HandleAction(request);
        } else if (action is ActionTypeEvent eventAction) {
            return await HandleAction(eventAction);
        } else {
            throw new ArgumentException("Unsupported ActionType");
        }
    }

    private List<byte> CreatePacket(bool isRequest, byte action) {
        List<byte> packet = new List<byte>();
        byte header = 0;
        header |= (byte)(isRequest ? 0 : 1 << 7);
        header |= (byte)(action & 0x3F);
        packet.Add(header);
        packet.Add((byte)'\n');
        return packet;
    }

    private async Task<int> HandleAction(ActionTypeRequest action) {
        List<byte> packet = CreatePacket(true, (byte)action);
        return await SendMessage(packet);
    }

    private async Task<int> HandleAction(ActionTypeEvent action) {
        List<byte> packet = CreatePacket(false, (byte)action);
        return await SendMessage(packet);
    }

    private async Task<int> SendMessage(List<byte> packet) {
        if (!_client.IsConnected()) return -1;

        try {
            await _networkLock.WaitAsync();
            byte[] response = await _client.SendMessageWithResponse(packet);
            if (response == null || response.Length == 0) return -1;
            return (response[0] & (1 << 6)) != 0 ? 1 : 0;
        }
        finally {
            _networkLock.Release();
        }
    }

    public async Task<int> SaveBuffer()            => await SendPacket(ActionTypeEvent.SaveReplayBuffer);
    public async Task<int> StartReplayBuffer()     => await SendPacket(ActionTypeEvent.StartReplayBuffer);
    public async Task<int> StopReplayBuffer()      => await SendPacket(ActionTypeEvent.StopReplayBuffer);
    public async Task<int> IsReplayBufferActive()  => await SendPacket(ActionTypeRequest.ReplayBufferActive);
    public async Task<int> StartRecording()        => await SendPacket(ActionTypeEvent.StartRecording);
    public async Task<int> StopRecording()         => await SendPacket(ActionTypeEvent.StopRecording);
    public async Task<int> StartStreaming()        => await SendPacket(ActionTypeEvent.StartStreaming);
    public async Task<int> StopStreaming()         => await SendPacket(ActionTypeEvent.StopStreaming);
    public async Task<int> SplitRecording()        => await SendPacket(ActionTypeEvent.RecordingSplitFile);
    public async Task<int> IsRecordingActive()     => await SendPacket(ActionTypeRequest.RecordingActive);
    public async Task<int> IsStreamingActive()     => await SendPacket(ActionTypeRequest.StreamingActive);
}