using System;
using System.IO.Pipes;

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text;

namespace FifoServer {

    public class Client {
        private static Client singletonClient;
        private int headerLength = 5;
        private NamedPipeClientStream serverPipe;
        private NamedPipeClientStream clientPipe;
        private string serverPipePath;
        private string clientPipePath;
        private byte[] eomHeader;

        
        public static Dictionary<string, FieldType> FormMap = new Dictionary<string, FieldType>{
            {"image", FieldType.RGBImage},
            {"image_depth", FieldType.DepthImage},
            {"image_normals", FieldType.NormalsImage},
            {"image_classes", FieldType.ClassesImage},
            {"image_flow", FieldType.FlowsImage},
            {"image_ids", FieldType.IDsImage},
            {"image-thirdParty-camera", FieldType.ThirdPartyCameraImage},
            {"image_thirdParty_depth", FieldType.ThirdPartyDepth},
            {"image_thirdParty_normals", FieldType.ThirdPartyNormals},
            {"image_thirdParty_classes", FieldType.ThirdPartyClasses},
            {"image_thirdParty_image_ids", FieldType.ThirdPartyImageIds},
            {"image_thirdParty_flow", FieldType.ThirdPartyFlow}

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
            serverPipe.Write(header, 0, header.Length);
            serverPipe.Write(body, 0, body.Length);
        }



        private Client (string serverPipePath, string clientPipePath) {
            this.serverPipePath = serverPipePath;
            this.clientPipePath = clientPipePath;
            this.eomHeader = new byte[headerLength];
            this.eomHeader[0] = (byte)FieldType.EndOfMessage;
        }


        public static Client GetInstance(string serverPipePath, string clientPipePath) {
            if (singletonClient == null) {
                singletonClient = new Client(serverPipePath, clientPipePath);
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
        ThirdPartyCameraImage = 0x0a,
        MetadataPatch = 0x0b,
        ThirdPartyDepth = 0x0c,
        ThirdPartyNormals = 0x0d,
        ThirdPartyImageIds = 0x0e,
        ThirdPartyClasses = 0x0f,
        ThirdPartyFlow = 0x10,
        EndOfMessage = 0xff
    }


}
