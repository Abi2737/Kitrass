using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServer : MonoBehaviour
{ 
	TcpListener _tcpListener;
	TcpClient _tcpClient;
	const int BUFFER_SIZE = 1;
	byte[] _rx;

	GamePlayerController _playerController;

	// Use this for initialization
	void Start ()
	{
		StartListening();

		_playerController = GameObject.Find("Player").GetComponent<GamePlayerController>();
	}

	private void OnDestroy()
	{
		_tcpListener.Stop();
	}

	void StartListening()
	{
		IPAddress ipaddr;
		int nPort = 2737;

		IPAddress.TryParse("127.0.0.1", out ipaddr);

		_tcpListener = new TcpListener(ipaddr, nPort);

		_tcpListener.Start(1);

		Debug.Log("Server started...");
		Debug.Log("Waiting for client...");

		_tcpListener.BeginAcceptTcpClient(onCompleteAcceptTcpClient, _tcpListener);
	}

	void onCompleteAcceptTcpClient(IAsyncResult iar)
	{
		TcpListener tcpl = (TcpListener)iar.AsyncState;

		try
		{
			_tcpClient = tcpl.EndAcceptTcpClient(iar);

			Debug.Log("Client Connected...");

			tcpl.BeginAcceptTcpClient(onCompleteAcceptTcpClient, tcpl);
			
			_rx = new byte[BUFFER_SIZE];
			_tcpClient.GetStream().BeginRead(_rx, 0, _rx.Length, onCompleteReadFromTCPClientStream, _tcpClient);
		}
		catch (Exception exc)
		{
			Debug.Log(exc.Message);
		}
	}

	void onCompleteReadFromTCPClientStream(IAsyncResult iar)
	{
		TcpClient tcpc;
		int nCountReadBytes = 0;

		try
		{
			tcpc = (TcpClient)iar.AsyncState;

			nCountReadBytes = tcpc.GetStream().EndRead(iar);

			if (nCountReadBytes == 0)// this happens when the client is disconnected
			{
				Debug.Log("Client disconnected.");
				return;
			}

			char c = Convert.ToChar(_rx[0]);
			Debug.Log("COMMAND: " + c);

			_playerController.CommandAction(ToActions(c));


			tcpc.GetStream().BeginRead(_rx, 0, _rx.Length, onCompleteReadFromTCPClientStream, tcpc);
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message);
		}
	}

	Actions ToActions(char c)
	{
		switch (c)
		{
			case 'a':
			case 'A':
				return Actions.MOVE_LEFT;

			case 'd':
			case 'D':
				return Actions.MOVE_RIGHT;

			case 's':
			case 'S':
				return Actions.MOVE_DOWN;

			case 'w':
			case 'W':
				return Actions.MOVE_UP;

			case 'j':
			case 'J':
				return Actions.TURN_LEFT;

			case 'l':
			case 'L':
				return Actions.TURN_RIGHT;

			case 'k':
			case 'K':
				return Actions.TURN_DOWN;

			case 'i':
			case 'I':
				return Actions.TURN_UP;
		}

		return Actions.NONE;
	}
}
