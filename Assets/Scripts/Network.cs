// File: Network.cs
using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class Network
{
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
    }

    public async Task<bool> CheckConnected() {
        await SendPacket(ActionTypeRequest.Ping);
        return _client != null && _client.IsConnected();
    }

    private async Task<int> SendPacket<T>(T action) where T : Enum {
        if (_client == null || !_client.IsConnected()) await Connect(serverAddr, serverPort);
        if (!_client.IsConnected()) {
            return -1;
        }

        if (action is ActionTypeRequest request) {
            return await HandleAction(request);
        }
        else if (action is ActionTypeEvent eventAction) {
            return await HandleAction(eventAction);
        }
        else {
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

        await _client.SendMessage(packet);
        byte[] response = await _client.ReceiveResponseAsync();
        if (response == null || response.Length == 0) return -1;

        StringBuilder sb = new(response.ToList().Count * 8);
        foreach (byte b in packet) { sb.Append(Convert.ToString(b, 2).PadLeft(8, '0')); }
        return (response[0] & (1 << 6)) != 0 ? 1 : 0;
    }

    public async Task<int> SaveReplayBuffer() {
        return await SendPacket(ActionTypeEvent.SaveReplayBuffer);
    }

    public async Task<int> StartReplayBuffer() {
        return await SendPacket(ActionTypeEvent.StartReplayBuffer);
    }

    public async Task<int> StopReplayBuffer() {
        return await SendPacket(ActionTypeEvent.StopReplayBuffer);
    }

    public async Task<int> IsReplayBufferActive() {
        return await SendPacket(ActionTypeRequest.ReplayBufferActive);
    }
}
