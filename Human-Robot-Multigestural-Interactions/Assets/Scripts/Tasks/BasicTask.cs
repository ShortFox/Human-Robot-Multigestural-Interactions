using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MQ.MultiAgent
{
    /// <summary>Simple task to test different controllers.</summary>
    public class BasicTask : Task
    {
        List<AvatarInputBase> inputManagers;

        /// <summary>List of trials to play in any AI controller.
        List<Box.TrialInfo> Trials;


        public override string TaskName { get; }            //Name of task
        public override string TrialID { get; }             //Unique identifier for trial
        public override int TrialNum { get; }               //Trial num.
        public override int BlockNum { get; }               //Block num.

        public override IEnumerator SetUp()
        {
            Trials = Box.ExpInitializer.Instance.GetTrialList(false);

            inputManagers = new List<AvatarInputBase>();

            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player.GetComponent<AvatarInputBase>() is AvatarInputBase c)
                {
                    if (c is ReplayInput ai_manager)
                    {
                        ai_manager.ReplayEnded += OnReplayEnded;
                    }
                    inputManagers.Add(c);
                }
            }

            Phase++;
            yield break;
        }

        public override IEnumerator Initiate()
        {

            Box.TrialInfo trial = Trials[0];

            foreach (AvatarInputBase c in inputManagers)
            {
                if (c is ReplayInput ai_controller)
                {
                    ai_controller.SetTrialInfo(trial);
                    ai_controller.Initiate();
                }
            }

            Phase++;
            yield break;
        }

        public override IEnumerator Run()
        {
            foreach (AvatarInputBase c in inputManagers)
            {
                if (c is ReplayInput ai_manager)
                {
                    ai_manager.Run();
                }
            }

            yield break;
        }

        public override IEnumerator Done()
        {
            foreach (AvatarInputBase c in inputManagers)
            {
                if (c is ReplayInput ai_manager && ai_manager.IsRunning())
                {
                    ai_manager.DeactivateController();
                }
            }

            Phase = TaskPhase.Initiate;
            yield break;
        }

        public override IEnumerator Finished()
        {
            UnityEditor.EditorApplication.isPlaying = false;
            yield break;
        }

        void OnReplayEnded(object sender, EventArgs e)
        {
            StartCoroutine(SetDoneAfter(50));
        }

        IEnumerator SetDoneAfter(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            Phase = TaskPhase.Done;
        }
    }
}
