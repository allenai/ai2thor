using System;
using System.IO.Pipes;

using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FifoServer {

    public class Client {

        protected const int SHM_PERMS = 511; // octal value is 0o00000777
        [DllImport("libc")]
        protected static extern int shmget( long/*(key_t)*/ key, int size, int shmflg );
        [DllImport("libc")]
        protected static extern IntPtr shmat( int shmid, int/*(void*)*/ shmaddr, int shmflg );
        [DllImport("libc")]
        protected static extern int shmdt( IntPtr shmaddr );

        private static Client singletonClient;
        private int headerLength = 5;
        private NamedPipeClientStream serverPipe;
        private NamedPipeClientStream clientPipe;
        private string serverPipePath;
        private string clientPipePath;
        private byte[] eomHeader;
        private IntPtr shmaddr;
        public bool hasShm;
        private int shmSize;
        private int shmOffset;

        
        public static Dictionary<string, FieldType> FormMap = new Dictionary<string, FieldType>{
            {"image", FieldType.RGBImage},
            {"image_depth", FieldType.DepthImage},
            {"image_normals", FieldType.NormalsImage},
            {"image_classes", FieldType.ClassesImage},
            {"image_flow", FieldType.FlowsImage},
            {"image_ids", FieldType.IDsImage},
            {"image-thirdParty-camera", FieldType.ThirdPartyCameraImage}

        };

        static public int UnpackNetworkBytes(byte[] data, int offset=0) {
            int networkInt = System.BitConverter.ToInt32(data, offset);
            return IPAddress.NetworkToHostOrder(networkInt);
        }

        static public byte[] PackNetworkBytes(int val) {
            int networkInt = IPAddress.HostToNetworkOrder(val);
            return System.BitConverter.GetBytes(networkInt);
        }

        public string ReceiveMessage() {
            if (clientPipe == null) {
                clientPipe = new NamedPipeClientStream(".", this.clientPipePath, PipeDirection.In);
                clientPipe.Connect();
            }
            string action = null;
            while (true) {
                byte[] header = new byte[headerLength];
                clientPipe.Read(header, 0, header.Length);
                FieldType fieldType = (FieldType)header[0];
                if (fieldType == FieldType.EndOfMessage) {
                        //Console.WriteLine("Got eom");
                    break;
                }
                int fieldLength = UnpackNetworkBytes(header, 1);
                byte[] body = new byte[fieldLength];
                clientPipe.Read(body, 0, body.Length);

                switch (fieldType) {
                    case FieldType.Action:
                        //Console.WriteLine("Got action");
                        action = Encoding.Default.GetString(body);
                        break;
                    default:
                        throw new Exception("Invalid field type for Client: " + fieldType);
                }
            }
            return action;
        }

        public void SendEOM() {
            serverPipe.Write(this.eomHeader, 0, this.headerLength);
            serverPipe.Flush();
            this.shmaddr = IntPtr.Subtract(this.shmaddr, this.shmOffset);
            this.shmOffset = 0;
        }

        public void SendMessage(FieldType t, byte[] body) {
            if (this.serverPipe == null) {
                this.serverPipe = new NamedPipeClientStream(".", this.serverPipePath, PipeDirection.Out);
                this.serverPipe.Connect();
            }
            //Console.WriteLine("server pipe + connected " + this.serverPipe.IsConnected );
            byte[] header = new byte[headerLength];
            header[0] = (byte)t;
            PackNetworkBytes(body.Length).CopyTo(header, 1);

            //Console.WriteLine("free memory:" + this.shmFree());
            //Console.WriteLine("header type is " + t);
            if (this.hasShm && (headerLength + body.Length) <= this.shmFree()) {
                //Console.WriteLine("using shared memory field");
                byte[] shmHeader = new byte[headerLength];
                shmHeader[0] = (byte)FieldType.SharedMemoryField;
                PackNetworkBytes(this.shmOffset).CopyTo(shmHeader, 1);

                Marshal.Copy(header, 0, this.shmaddr, header.Length);
                this.shmaddr = IntPtr.Add(this.shmaddr, header.Length);
                this.shmOffset += header.Length;

                //Marshal.Copy(body, 0, this.shmaddr, body.Length);
                //this.shmOffset += body.Length;
                //this.shmaddr = IntPtr.Add(this.shmaddr, body.Length);
                serverPipe.Write(shmHeader, 0, shmHeader.Length);
            } else {
                serverPipe.Write(header, 0, header.Length);
                serverPipe.Write(body, 0, body.Length);
            }
        }

        private int shmFree() {
            return this.shmSize - this.shmOffset;
        }


        private Client (string serverPipePath, string clientPipePath, int shmKey, int shmSize) {
            this.serverPipePath = serverPipePath;
            this.clientPipePath = clientPipePath;
            this.eomHeader = new byte[headerLength];
            this.eomHeader[0] = (byte)FieldType.EndOfMessage;
            if (shmKey != 0) { // key == 0 is IPC_PRIVATE, so this will never be sent as a shared key
                int shmid = shmget(shmKey, shmSize, SHM_PERMS);
                if (shmid == -1) {
                    Debug.Log("shmget returned -1, not using shared memory");
                } else {
                    this.shmaddr = shmat(shmid, 0, 0);
                    this.hasShm = true;
                    this.shmSize = shmSize;
                    Debug.Log("shared memory initialized");
                }
            }
        }

        // finalizer
        ~Client() {
            if (this.hasShm) {
                shmdt(this.shmaddr);
                this.hasShm = false;
                this.shmSize = 0;

            }
        }


        public static Client GetInstance(string serverPipePath, string clientPipePath, int shmKey, int shmSize) {
            if (singletonClient == null) {
                singletonClient = new Client(serverPipePath, clientPipePath, shmKey, shmSize);
            }
            return singletonClient;
        }


    }

    public enum FieldType:byte {
        Metadata = 0x01,
        Action = 0x02,
        ActionResult = 0x03,
        RGBImage = 0x04,
        DepthImage = 0x05,
        NormalsImage = 0x06,
        FlowsImage = 0x07,
        ClassesImage = 0x08,
        IDsImage = 0x09,
        ThirdPartyCameraImage = 0x0a, // 10
        SharedMemoryField = 0xc8, // 200
        EndOfMessage = 0xff
    }


}
