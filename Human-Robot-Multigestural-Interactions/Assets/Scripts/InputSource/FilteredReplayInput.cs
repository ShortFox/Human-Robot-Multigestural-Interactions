using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MQ.MultiAgent
{
    public class FilteredReplayInput : ReplayInput
    {

        public int FilterWindowSize = 5;

        public override void Initiate()
        {
            base.Initiate();
            _playbackData = FilterPlaybackInfo(_playbackData);
        }

        protected List<Box.PlaybackInfo> FilterPlaybackInfo(List<Box.PlaybackInfo> info)
        {
            if (FilterWindowSize < 1)
            {
                return info;
            }

            List<Box.PlaybackInfo> filtered  = new List<Box.PlaybackInfo>();
            List<Box.PlaybackInfo> window = new List<Box.PlaybackInfo>();

            for (int i = 0; i < FilterWindowSize; i++)
            {
                filtered.Add(info[i]);
                window.Add(info[i]);
            }

            for (int i = FilterWindowSize; i < info.Count; i++)
            {
                window.RemoveAt(0);
                Box.PlaybackInfo next_value = info[i];

                if (float.IsNaN(next_value.GazePosX) ||
                    float.IsNaN(next_value.GazePosY) ||
                    float.IsNaN(next_value.GazePosZ))
                {
                    next_value = window.Last();
                }

                window.Add(next_value);

                float eye_pos_x = 0, eye_pos_y = 0, eye_pos_z = 0;
                foreach (Box.PlaybackInfo win in window)
                {
                    eye_pos_x += win.GazePosX / (float)FilterWindowSize;
                    eye_pos_y += win.GazePosY / (float)FilterWindowSize;
                    eye_pos_z += win.GazePosZ / (float)FilterWindowSize;
                }
                Box.PlaybackInfo filtered_info = info[i];
                filtered_info.GazePosX = eye_pos_x;
                filtered_info.GazePosY = eye_pos_y;
                filtered_info.GazePosZ = eye_pos_z;
                filtered_info.GazePos = new Vector3(eye_pos_x, eye_pos_y, eye_pos_z);

                filtered.Add(filtered_info);
            }

            return filtered;
        }
    }
}
