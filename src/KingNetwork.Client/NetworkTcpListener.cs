using KingNetwork.Shared;
using KingNetwork.Shared.Helpers;
using System;
using System.Net.Sockets;

namespace KingNetwork.Client
{
    /// <summary>
    /// This class is responsible for managing the network tcp listener.
    /// </summary>
    public class NetworkTcpListener : TcpClient
    {

        #region private members 	

        /// <summary>
        /// The callback of message received handler implementation.
        /// </summary>
        private MessageReceivedHandler _messageReceivedHandler { get; }
        
        /// <summary>
        /// The callback of client disconnedted handler implementation.
        /// </summary>
        private ClientDisconnectedHandler _clientDisconnectedHandler { get; }
        
        /// <summary>
        /// The buffer of client connection.
        /// </summary>
        private byte[] _buffer;

        #endregion

        #region delegates 

        /// <summary>
		/// The delegate of message reveiced handler from server connection.
		/// </summary>
        /// <param name="data">The data bytes from message received.</param>
        public delegate void MessageReceivedHandler(byte[] data);
        
        /// <summary>
		/// The delegate of client disconnected handler connection.
		/// </summary>
        public delegate void ClientDisconnectedHandler();

        #endregion

        #region properties

        /// <summary>
        /// The stream of tcp client.
        /// </summary>
        public NetworkStream Stream => GetStream();

        /// <summary>
		/// The flag of client connection.
		/// </summary>
		public bool IsConnected => SocketHelper.IsConnected(this);

        #endregion

        #region constructors

        /// <summary>
        /// Creates a new instance of a <see cref="NetworkTcpListener"/>.
        /// </summary>
        /// <param name="messageReceivedHandler">The callback of message received handler implementation.</param>
        /// <param name="clientDisconnectedHandler">The callback of client disconnedted handler implementation.</param>
        public NetworkTcpListener(MessageReceivedHandler messageReceivedHandler, ClientDisconnectedHandler clientDisconnectedHandler)
        {
            try
            {
                _buffer = new byte[ConnectionSettings.MAX_MESSAGE_BUFFER];
                _messageReceivedHandler = messageReceivedHandler;
                _clientDisconnectedHandler = clientDisconnectedHandler;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #endregion

        #region public methods implementation

        /// <summary>
        /// Method responsible for start the client network tcp listener.
        /// </summary>
        /// <param name="ip">The ip address of server.</param>
        /// <param name="port">The port of server.</param>
        public void StartClient(string ip, int port)
        {
            try
            {
                Client.NoDelay = true;
                Connect(ip, port);

                _buffer = new byte[ConnectionSettings.MAX_MESSAGE_BUFFER];

                ReceiveBufferSize = ConnectionSettings.MAX_MESSAGE_BUFFER;
                SendBufferSize = ConnectionSettings.MAX_MESSAGE_BUFFER;
                Stream.BeginRead(_buffer, 0, ReceiveBufferSize, new AsyncCallback(ReceiveDataCallback), null);

                Console.WriteLine("Connected to server!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        /// <summary>
        /// Method responsible for send message to connected server.
        /// </summary>
        /// <param name="data">The data bytes from message.</param>
        public void SendMessage(byte[] data)
        {
            try
            {
                Stream.BeginWrite(data, 0, data.Length, null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}.");
            }
        }

        #endregion

        #region private methods implementation

        /// <summary> 	
        /// The callback from received message from connected server. 	
        /// </summary> 	
        /// <param name="asyncResult">The async result from a received message from connected server.</param>
        private void ReceiveDataCallback(IAsyncResult asyncResult)
        {
            try
            {
                if (IsConnected)
                {
                    int endRead = Stream.EndRead(asyncResult);

                    if (endRead != 0)
                    {
                        byte[] numArray = new byte[endRead];
                        Buffer.BlockCopy(_buffer, 0, numArray, 0, endRead);

                        Stream.BeginRead(_buffer, 0, ReceiveBufferSize, new AsyncCallback(ReceiveDataCallback), null);

                        Console.WriteLine($"Received message from server.");

                        _messageReceivedHandler(_buffer);
                    }
                }

                Close();
                Console.WriteLine($"Client disconnected from server.");

                _clientDisconnectedHandler();
            }
            catch (Exception ex)
            {
                Close();
                Console.WriteLine($"Client disconnected from server.");
            }
        }

        #endregion
    }
}