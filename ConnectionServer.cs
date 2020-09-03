using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

[Serializable]
public class ConnectionServer : MonoBehaviour
{

    #region private members 	
    /// <summary> 	
    /// TCPListener to listen for incomming TCP connection 	
    /// requests. 	
    /// </summary> 	
    private TcpListener tcpListener;
    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread tcpListenerThread;
    /// <summary> 	
    /// Create handle to connected tcp client. 	
    /// </summary> 	
    private TcpClient connectedTcpClient;
    public InputField IPInput;
    public string ip;
    //private GameManager gm = GameObject.Find("PR_GameManager").GetComponent<GameManager>();
    public GameManager gm;
    #endregion

    // Use this for initialization
    public void ButtonServer()
    {
        ip = IPInput.text;
        // Start TcpServer background thread 		
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncomingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
        gm.cs = this;
        gm.MakeBoard();
        gm.mPieceManager.SetInteractive(gm.mPieceManager.mWhitePieces, false);
        gm.mPieceManager.SetInteractive(gm.mPieceManager.mBlackPieces, false);
    }

    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncomingRequests()
    {
        try
        {
            // Create listener on localhost port 8052. 			
            tcpListener = new TcpListener(IPAddress.Parse(ip), 30000);
            tcpListener.Start();
            Debug.Log("Server is listening");
            Byte[] bytes = new Byte[2048];
            while (true)
            {
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    gm.p = "SERVERRECEIVE";
                    // Get a stream object for reading 					
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        int length;
                        // Read incomming stream into byte arrary. 						
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
                            //byte[] buffer = new byte[socketConnection.ReceiveBufferSize];
                            //int bytesRead = stream.Read(buffer, 0, socketConnection.ReceiveBufferSize);
                            string dataReceived = Encoding.ASCII.GetString(incomingData);
                            Debug.Log("Message received from client: " + dataReceived);
                            /*var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
                            // Convert byte array to string message. 						
                            //string serverMessage = Encoding.ASCII.GetString(incommingData);
                            Debug.Log("we here");
                            */
                            List<string> positions = new List<string>();
                            string[] p = dataReceived.Split('-');
                            for (int i = 0; i < p.Length; i++)
                            {
                                positions.Add(p[i]);
                            }
                            gm.positions = positions;
                            gm.b = true;
                            //gm.ReplacePieces();
                            //gm.PlacePieces(positions);
                            bytes = new byte[2048];
                            gm.p = "SERVERRECEIVE";
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }
    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    public void SendMessage()
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing. 			
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                string[] s = gm.generateBoardList(gm.mBoard).ToArray();
                string positions = "";
                for (int i = 0; i < s.Length; i++)
                {
                    positions += s[i] + "-";
                }
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(positions);
                Debug.Log(positions);
                //string clientMessage = "This is a message from one of your clients.";
                // Convert string message to byte array.                 
                //byte[] clientMessageAsByteArray = ObjectToByteArray(gm.generateBoardList(gm.mBoard).ToArray());
                // Write byte array to socketConnection stream.                 
                Debug.Log("Sending message");
                stream.Write(bytesToSend, 0, bytesToSend.Length);
                //Cursor.lockState = CursorLockMode.Locked;
                // Convert string message to byte array.                 
                //byte[] clientMessageAsByteArray = ObjectToByteArray(gm.generateBoardList(gm.mBoard).ToArray());
                // Write byte array to socketConnection stream.                 
                //stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                //Cursor.lockState = CursorLockMode.Locked;
                gm.p = "SERVERSEND";
                Debug.Log("Server message sent - should be received by client");
            }
            //stream.Close();
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    public byte[] ObjectToByteArray(string[] obj)
    {
        string positions = "";
        for (int i = 0; i < obj.Length; i++)
        {
            positions += obj[i] + " ";
        }
        Debug.Log(positions);
        MemoryStream stream = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.AssemblyFormat
            = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
        formatter.Serialize(stream, obj);
        return stream.ToArray();
        /*using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            var rows = obj.GetLength(0);
            writer.Write(rows);
            for (int i = 0; i < rows; i++)
            {
                writer.Write(obj[i]);
            }
            writer.Flush();
            return stream.ToArray();
        }*/
    }

    public string[] ByteArrayToObject(byte[] arrBytes)
    {
        MemoryStream stream = new MemoryStream(arrBytes);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.AssemblyFormat

            = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
        formatter.Binder

            = new VersionConfigToNamespaceAssemblyObjectBinder();
        string positions = (string)formatter.Deserialize(stream);
        return positions.Split(' ');
        /*using (var stream = new MemoryStream(arrBytes))
        using (var reader = new BinaryReader(stream, Encoding.UTF8))
        {
            var rows = reader.ReadInt32();
            var result = new string[rows];
            for (int i = 0; i < rows; i++)
            {
                result[i] = reader.ReadString();
            }
            return result;
        }*/
    }
}
