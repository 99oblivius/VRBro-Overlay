using System;
using UnityEngine;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public class Client
{
    private readonly SemaphoreSlim _requestLock = new(1, 1);

    private string _address;
    private int _port;
    private TcpClient _client;
    private NetworkStream _stream;

    public Client(string address, int port) {
        _address = address;
        _port = port;
    }

    public async Task ConnectAsync() {
        _client = new TcpClient();
        try {
            await _client.ConnectAsync(_address, _port);
            _stream = _client.GetStream();
        } catch (Exception) {}
    }

    public async Task<byte[]> SendMessageWithResponse(List<byte> packet) {
        if (_client == null || !_client.Connected) return null;

        try {
            await _requestLock.WaitAsync();
            await SendMessage(packet);
            return await ReceiveResponseWithTimeout();
        }
        finally {
            _requestLock.Release();
        }
    }

    public async Task SendMessage(List<byte> packet) {
        if (_client == null || !_client.Connected) return;

        byte[] bMessage = packet.ToArray();
        try {
            await _stream.WriteAsync(bMessage, 0, bMessage.Length);
            StringBuilder sb = new(packet.Count * 8);
            foreach (byte b in packet) { sb.Append(Convert.ToString(b, 2).PadLeft(8, '0')); }
        }
        catch (Exception) {}
    }

    private async Task<byte[]> ReceiveResponseWithTimeout() {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try {
            return await Task.Run(async () => {
                if (_client == null || !_client.Connected) return null;

                byte[] buffer = new byte[512];
                List<byte> response = new();
                int bytesRead;

                do {
                    // Check cancellation between reads
                    if (cts.Token.IsCancellationRequested) {
                        throw new OperationCanceledException();
                    }

                    bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    response.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));
                } while (_stream.DataAvailable);

                return response.ToArray();
            }, cts.Token);
        }
        catch (OperationCanceledException) {
            Debug.LogWarning("Network request timed out");
            return null;
        }
        catch (Exception ex) {
            Debug.LogError($"Error receiving response: {ex.Message}");
            return null;
        }
    }

    public void Close() {
        try {
            _requestLock?.Dispose();
            _stream?.Close();
            _client?.Close();
        }
        catch (Exception) {}
    }

    ~Client() {
        Close();
    }

    public bool IsConnected() {
        return _client != null && _client.Connected;
    }
}