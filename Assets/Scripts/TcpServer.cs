using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TcpServer : MonoBehaviour
{ 
	TcpListener _tcpListener;
	TcpClient _tcpClient;
	const int BUFFER_SIZE = 1;
	byte[] _rx;

	GamePlayerController _playerController;

	public Text infoText;
	string _message;

	bool _restartGame;
	
	void Awake()
	{
		DontDestroyOnLoad(this);

		_playerController = null;
		_message = null;
		_restartGame = false;

		StartListening();
	}

	void Update()
	{
		if (_playerController == null)
		{
			GameObject player = GameObject.Find("Player");
			if (player != null)
				_playerController = player.GetComponent<GamePlayerController>();
		}

		if (_message != null)
		{
			ShowMessage(_message);
			_message = null;
		}

		if (_restartGame)
		{
			SceneManager.LoadScene(1);
			_playerController = null;
			_restartGame = false;
		}
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
		
		_message = "Server started... Waiting for client...";

		_tcpListener.BeginAcceptTcpClient(OnCompleteAcceptTcpClient, _tcpListener);
	}

	void OnCompleteAcceptTcpClient(IAsyncResult iar)
	{
		TcpListener tcpl = (TcpListener)iar.AsyncState;

		try
		{
			_tcpClient = tcpl.EndAcceptTcpClient(iar);

			
			_message = "Client connected.";
			

			tcpl.BeginAcceptTcpClient(OnCompleteAcceptTcpClient, tcpl);
			
			_rx = new byte[BUFFER_SIZE];
			_tcpClient.GetStream().BeginRead(_rx, 0, _rx.Length, OnCompleteReadFromTCPClientStream, _tcpClient);
		}
		catch (Exception ex)
		{
			_message = ex.Message;
		}
	}

	void OnCompleteReadFromTCPClientStream(IAsyncResult iar)
	{
		TcpClient tcpc;
		int nCountReadBytes = 0;

		try
		{
			tcpc = (TcpClient)iar.AsyncState;

			nCountReadBytes = tcpc.GetStream().EndRead(iar);

			if (nCountReadBytes == 0)// this happens when the client is disconnected
			{
				_message = "Client disconnected.";
				return;
			}

			char c = Convert.ToChar(_rx[0]);
			if (c == 'r' || c == 'R')
			{
				_restartGame = true;
			}
			else
				_playerController.CommandAction(ToActions(c));

			tcpc.GetStream().BeginRead(_rx, 0, _rx.Length, OnCompleteReadFromTCPClientStream, tcpc);
		}
		catch (Exception ex)
		{
			_message = ex.Message;
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

	void ShowMessage(string message)
	{
		if (infoText != null)
		{
			infoText.text = message;
		}
		else
		{
			Debug.Log(message);
		}
	}
}
