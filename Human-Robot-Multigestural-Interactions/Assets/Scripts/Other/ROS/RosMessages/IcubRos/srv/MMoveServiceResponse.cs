//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.IcubRos
{
    public class MMoveServiceResponse : Message
    {
        public const string RosMessageName = "icub_ros/MoveService";

        public Moveit.MRobotTrajectory[] trajectories;

        public MMoveServiceResponse()
        {
            this.trajectories = new Moveit.MRobotTrajectory[0];
        }

        public MMoveServiceResponse(Moveit.MRobotTrajectory[] trajectories)
        {
            this.trajectories = trajectories;
        }
        public override List<byte[]> SerializationStatements()
        {
            var listOfSerializations = new List<byte[]>();
            
            listOfSerializations.Add(BitConverter.GetBytes(trajectories.Length));
            foreach(var entry in trajectories)
                listOfSerializations.Add(entry.Serialize());

            return listOfSerializations;
        }

        public override int Deserialize(byte[] data, int offset)
        {
            
            var trajectoriesArrayLength = DeserializeLength(data, offset);
            offset += 4;
            this.trajectories= new Moveit.MRobotTrajectory[trajectoriesArrayLength];
            for(var i = 0; i < trajectoriesArrayLength; i++)
            {
                this.trajectories[i] = new Moveit.MRobotTrajectory();
                offset = this.trajectories[i].Deserialize(data, offset);
            }

            return offset;
        }

        public override string ToString()
        {
            return "MMoveServiceResponse: " +
            "\ntrajectories: " + System.String.Join(", ", trajectories.ToList());
        }
    }
}