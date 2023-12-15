//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.IcubRos
{
    public class MTarget : Message
    {
        public const string RosMessageName = "icub_ros/Target";

        public float x;
        public float y;
        public float z;

        public MTarget()
        {
            this.x = 0.0f;
            this.y = 0.0f;
            this.z = 0.0f;
        }

        public MTarget(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public override List<byte[]> SerializationStatements()
        {
            var listOfSerializations = new List<byte[]>();
            listOfSerializations.Add(BitConverter.GetBytes(this.x));
            listOfSerializations.Add(BitConverter.GetBytes(this.y));
            listOfSerializations.Add(BitConverter.GetBytes(this.z));

            return listOfSerializations;
        }

        public override int Deserialize(byte[] data, int offset)
        {
            this.x = BitConverter.ToSingle(data, offset);
            offset += 4;
            this.y = BitConverter.ToSingle(data, offset);
            offset += 4;
            this.z = BitConverter.ToSingle(data, offset);
            offset += 4;

            return offset;
        }

        public override string ToString()
        {
            return "MTarget: " +
            "\nx: " + x.ToString() +
            "\ny: " + y.ToString() +
            "\nz: " + z.ToString();
        }
    }
}