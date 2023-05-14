using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;
using Meshtastic.Protobufs;

namespace Hams_Final
{
    internal class TestingHams
    {
        void Method1()
        {

            MeshPacket meshPacket = new MeshPacket();
            ByteString message = ByteString.CopyFromUtf8("hello");
            meshPacket.Decoded.Payload = message;
            meshPacket.Decoded.Portnum = PortNum.TextMessageCompressedApp;
            byte[] meshPacketByteArray = meshPacket.Decoded.ToByteArray();

            


            Meshtastic.Protobufs.MyNodeInfo nodeInfo = new Meshtastic.Protobufs.MyNodeInfo();

        }
    }
}
