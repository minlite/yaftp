using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace yaftp
{
    public partial class Main : Form
    {
        private Thread serverThread;
        private Thread clientThread;
        public string file;
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                file = openFileDialog1.FileName;
                try
                {
                    this.textBox2.Text = file;
                }
                catch (IOException)
                {
                }
            }
            //Console.WriteLine(size); // <-- Shows file size in debugging mode.
            //Console.WriteLine(result); // <-- For debugging use only.
        }
        public string getFilePath()
        {
            return file;
        }
        private void startServer()
        {
            TCPServer.Server server = new TCPServer.Server(Int32.Parse(this.textBox1.Text), this.textBox2.Text, this.openFileDialog1.SafeFileName);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.serverThread = new Thread(new ThreadStart(startServer));
            this.serverThread.Start();
            this.toolStripStatusLabel2.Text = "Server Started!";
        }
        private void startClient()
        {
            TCPClient.Client client = new TCPClient.Client(Int32.Parse(this.textBox4.Text), this.textBox3.Text);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            this.clientThread = new Thread(new ThreadStart(startClient));
            this.clientThread.Start();
            this.toolStripStatusLabel2.Text = "Transfering...";
        }
    }
}
namespace TCPServer
{
    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        public string file;
        public string filenameext;
        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            int bytesRead;
            byte[] buffer = new byte[4096];
            byte[] message = new byte[4096];
            byte[] fileByte = File.ReadAllBytes(file);
            int intValue;
            string strValue;
            while (true)
            {
                bytesRead = 0;
                try
                {
                    // Assuming that the client is sending a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    // A socker error has occured
                    MessageBox.Show("An error has occured when trying to connect to the client... 0x01", "Error: 0x01");
                    break;
                }
                if (bytesRead == 0)
                {
                    // The client has disconnected or didn't send any message
                    MessageBox.Show("The client has disconnected... 0x02", "Error: 0x02");
                    break;
                }
                // A message has been received.. We should validate it now
                string msg = encoder.GetString(message).Replace("\0", "");
                switch (msg)
                {
                    case "HELLO":
                        buffer = encoder.GetBytes("HELLO");
                        clientStream.Write(buffer, 0, buffer.Length);
                        break;
                    case "GETFILESIZE":
                        intValue = fileByte.Length;
                        strValue = Convert.ToString(intValue);
                        char[] charValue = strValue.ToCharArray();
                        byte[] byteValue = encoder.GetBytes(charValue, 0, charValue.Length);
                        clientStream.Write(byteValue, 0, byteValue.Length);
                        break;
                    case "GETFILENAME":
                        buffer = encoder.GetBytes(filenameext);
                        clientStream.Write(buffer, 0, buffer.Length);
                        break;
                    case "GETFILECONTENTS":
                        clientStream.Write(fileByte, 0, fileByte.Length);
                        break;
                    case "CLOSETHECONNECTION":
                        buffer = encoder.GetBytes("CLOSED");
                        clientStream.Write(buffer, 0, buffer.Length);                       
                        clientStream.Close();
                        tcpClient.Close();
                        Thread.CurrentThread.Abort();
                        break;
                    default:
                        MessageBox.Show("Unknown command received from the client... 0x03", "Error: 0x03");
                        break;
                }
            }
        }
        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();
               
                //create a thread to handle communication
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }
        public Server(int port, string filepath, string filename)
        {
            file = filepath;
            filenameext = filename;
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }
    }
}
namespace TCPClient
{
    class Client
    {
        public Client(int port, string IP)
        {
            TcpClient client = new TcpClient();
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(IP), port);
            client.Connect(serverEndPoint);
            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = new byte[4096];
            int bytesRead, bytesRead2, fileSizeI, bytesRead3, bytesRead4;
            buffer = encoder.GetBytes("HELLO");
            byte[] message = new byte[4096];
            byte[] fileSizeB = new byte[4096];
            byte[] fileNameB = new byte[4096];
            clientStream.Write(buffer, 0, buffer.Length);
            while (true)
            {
                bytesRead = 0;
                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    MessageBox.Show("An error has occured when trying to connect to the server... 0x04", "Error: 0x04");
                    break;
                }
                if (bytesRead == 0)
                {
                    MessageBox.Show("We have disconnected from the server... 0x05", "Error: 0x05");
                    break;
                }

                if (encoder.GetString(message).Replace("\0", "") == "HELLO")
                {
                    buffer = encoder.GetBytes("GETFILESIZE");
                    clientStream.Write(buffer, 0, buffer.Length);
                    while (true)
                    {
                        bytesRead2 = 0;
                        try
                        {
                            bytesRead2 = clientStream.Read(fileSizeB, 0, 4096);
                        }
                        catch
                        {
                            MessageBox.Show("An error has occured when trying to receive the file size... 0x07", "Error: 0x07");
                            break;
                        }
                        if (bytesRead2 == 0)
                        {
                            MessageBox.Show("We have disconnected from the server... 0x08", "Error: 0x08");
                            break;
                        }
                        fileSizeI = Int32.Parse(encoder.GetString(fileSizeB).Replace("\0", ""));
                        buffer = encoder.GetBytes("GETFILENAME");
                        clientStream.Write(buffer, 0, buffer.Length);
                        while (true)
                        {
                            bytesRead3 = 0;
                            try
                            {
                                bytesRead3 = clientStream.Read(fileNameB, 0, 4096);
                            }
                            catch
                            {
                                MessageBox.Show("An error has occured when trying to receive the filename... 0x09", "Error: 0x09");
                                break;
                            }
                            if(bytesRead3 == 0) {
                                MessageBox.Show("We have disconnected from the server... 0x10", "Error: 0x10");
                                break;
                            }
                            string fileNameS = encoder.GetString(fileNameB).Replace("\0", "");
                            buffer = encoder.GetBytes("GETFILECONTENTS");
                            clientStream.Write(buffer, 0, buffer.Length);
                            byte[] file = new byte[fileSizeI];
                            while (true)
                            {
                                bytesRead4 = 0;
                                try
                                {
                                    bytesRead4 = clientStream.Read(file, 0, fileSizeI);
                                }
                                catch
                                {
                                    MessageBox.Show("An error has occured when trying to receive the file... 0x11", "Error: 0x11");
                                    break;
                                }
                                if (bytesRead4 == 0)
                                {
                                    MessageBox.Show("We have disconnected from the server... 0x12", "Error: 0x12");
                                    break;

                                }
                                    try
                                    {
                                        string path = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Downloads\\YAFTP\\" + fileNameS;
                                        FileStream fs = File.Create(path, fileSizeI, FileOptions.None);
                                        BinaryWriter bw = new BinaryWriter(fs);
                                        bw.Write(file);
                                        bw.Close();
                                        fs.Close();
                                    }
                                    catch (IOException)
                                    {
                                    }
                                    buffer = encoder.GetBytes("CLOSETHECONNECTION");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Close();
                                    client.Close();
                                    Thread.CurrentThread.Abort();
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Unknown command reply received from the server...0x06", "Error: 0x06");
                    break;
                }
            }
        }
    }
}
