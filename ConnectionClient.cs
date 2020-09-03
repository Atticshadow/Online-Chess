using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

[Serializable]
public class ConnectionClient : MonoBehaviour {

    #region private members 	
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    //private GameManager gm = GameObject.Find("PR_GameManager").GetComponent<GameManager>();
    public InputField IPInput;
    public GameManager gm;
    #endregion
    // Use this for initialization 	
    public string ip;
    public void ButtonClient()
    {
        ip = IPInput.text;
        ConnectToTcpServer();
        gm.mPieceManager.ResetPieces();
        gm.cc = this;
        gm.MakeBoard();
        gm.mPieceManager.SetInteractive(gm.mPieceManager.mWhitePieces, false);
        gm.mPieceManager.SetInteractive(gm.mPieceManager.mBlackPieces, false);
    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToTcpServer()
    {
        try
        {
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }
    /// <summary> 	
    /// Runs in background clientReceiveThread; Listens for incomming data. 	
    /// </summary>     
    private void ListenForData()
    {
        try
        {
            socketConnection = new TcpClient(ip, 30000);
            Debug.Log("Hi");
            Byte[] bytes = new Byte[2048];
            while (true)
            {
                // Get a stream object for reading 			
                Debug.Log("How are ya");
                using (NetworkStream stream = socketConnection.GetStream())
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
                        Debug.Log("Message received from server: " + dataReceived);
                        /*var incomingData = new byte[length];
                        Array.Copy(bytes, 0, incomingData, 0, length);
                        // Convert byte array to string message. 						
                        //string serverMessage = Encoding.ASCII.GetString(incommingData);
                        Debug.Log("we here");
                        */
                        List<string> positions = new List<string>();
                        string[] p = dataReceived.Split('-');
                        for(int i=0; i<p.Length; i++)
                        {
                            positions.Add(p[i]);
                        }
                        gm.positions = positions;
                        gm.b = true;
                        //gm.ReplacePieces();
                        //gm.PlacePieces(positions);
                        bytes = new byte[2048];

                        //Cursor.lockState = CursorLockMode.None;
                        gm.p = "CLIENTRECEIVE";
                        //Debug.Log("server message received as: " + serverMessage);
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
        catch(EndOfStreamException se)
        {
            Debug.Log("Stream exception: " + se);
        }
    }
    /// <summary> 	
    /// Send message to server using socket connection. 	
    /// </summary> 	
    public void SendMessage()
    {
        if (socketConnection == null)
        {
            Debug.Log("No connection");
            return;
        }
        try
        {
            Debug.Log("Attempting to send");
            // Get a stream object for writing. 			
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                string[] s = gm.generateBoardList(gm.mBoard).ToArray();
                string positions = "";
                for (int i = 0; i < s.Length; i++)
                {
                    positions += s[i] + "-";
                }
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(positions);
                //string clientMessage = "This is a message from one of your clients.";
                // Convert string message to byte array.                 
                //byte[] clientMessageAsByteArray = ObjectToByteArray(gm.generateBoardList(gm.mBoard).ToArray());
                // Write byte array to socketConnection stream.                 
                Debug.Log("Sending message");
                stream.Write(bytesToSend, 0, bytesToSend.Length);
                //Cursor.lockState = CursorLockMode.Locked;
                gm.p = "CLIENTSEND";
                Debug.Log("Client sent his message - should be received by server");
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
        for(int i = 0; i < obj.Length; i++)
        {
            positions += obj[i] + " ";
        }
        Debug.Log(positions);
        MemoryStream stream = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();
        /*formatter.AssemblyFormat
            = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;*/
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
        try
        {
            MemoryStream stream = new MemoryStream(arrBytes);
            BinaryFormatter formatter = new BinaryFormatter();
            /*formatter.AssemblyFormat

                = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            formatter.Binder

                = new VersionConfigToNamespaceAssemblyObjectBinder();*/
            Debug.Log("before ds");
            string positions = formatter.Deserialize(stream) as string;
            //int i = Convert.ToInt32(positions); //to INT
            Debug.Log(positions);
            return positions.Split(null);
        } catch(Exception es)
        {
            Debug.Log(es.Message);
        }
        return null;
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

internal sealed class VersionConfigToNamespaceAssemblyObjectBinder : SerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        Type typeToDeserialize = null;
        try
        {
            string ToAssemblyName = assemblyName.Split(',')[0];
            Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly ass in Assemblies)
            {
                if (ass.FullName.Split(',')[0] == ToAssemblyName)
                {
                    typeToDeserialize = ass.GetType(typeName);
                    break;
                }
            }
        }
        catch (System.Exception exception)
        {
            throw exception;
        }
        return typeToDeserialize;
    }
}
