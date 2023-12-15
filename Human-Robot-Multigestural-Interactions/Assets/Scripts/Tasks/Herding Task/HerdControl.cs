namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Linq;
    using Mirror;

    public class HerdControl : NetworkBehaviour
    {
        public static HerdControl Instance { get; private set; }

        public SheepDynamics[] Sheep { get; private set; }
        public GameObject[] Dogs { get; private set; }

        SyncExperimentPhase Experiment;
        public Transform Field;
        public Transform TargetRegion;

        private Material wool;
        private float containment_criteria = 0.1f;                     //Scaled containment criteria.

        private int _score;
        public int Score
        {
            get
            {
                if (Experiment.ExperimentPhase < 3)
                {
                    Debug.LogError("Error: Score should not be accessed yet.");
                    return 0;
                }
                else return _score;
            }
            set
            {
                if (value == -1)
                {
                    _score = value;
                }
            }
        }


        private int _contained;
        public int Contained
        {
            get { return _contained; }
            set
            {
                if (value != _contained)
                {
                    int color = Mathf.Abs(value - 1);
                    wool.color = new Color(1f, color, color, 1);
                }
                _contained = value;
            }
        }

        public int containmentCounts { get; private set; }                           //This is a containment counter to keep track of score.

        #region Unity Methods
        private void Awake()
        {
            syncInterval = 0f;
            Instance = this;
            wool = (Material)Resources.Load("Materials/HerdingTask/Sheep");
            wool.color = new Color(1f, 1f, 1f, 1);

            Field = transform.parent.Find("HerdField");
            HideField();
            Sheep = GetSheep();
            Dogs = GetDogs();
        }
        private void Start()
        {
            Experiment = SyncExperimentPhase.Instance;
            HideSheep();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            //CheckSheepSpread();
            if (Experiment.ExperimentPhase == 1)
            {
                _score = 0;
            }
            else if (Experiment.ExperimentPhase == 2)
            {
                _score += CheckAllInTargetRegion();
            }


            //Only Server would have access to this.
            if (Dogs != null)
            {
                if (Experiment.Players.Length != Dogs.Length)
                {
                    Dogs = GetDogs();
                }
            }
        }
        #endregion

        #region Helper Functions
        public GameObject[] GetDogs()
        {
            return GameObject.FindGameObjectsWithTag("Finger");
        }

        public void HideSheep()
        {
            Vector3 hidePos = Vector3.zero;
            foreach (SheepDynamics sheeple in Sheep)
            {
                sheeple.Activate = false;
                sheeple.body.useGravity = false;
                sheeple.Reset();
                hidePos = sheeple.transform.localPosition;
                hidePos.y = -100f;
                sheeple.transform.localPosition = hidePos;
            }
        }
        public void ActivateSheep()
        {
            Vector3 readyPos = Vector3.zero;
            foreach (SheepDynamics sheeple in Sheep)
            {
                readyPos = sheeple.transform.localPosition;
                readyPos.y = sheeple.InitPos.y;
                sheeple.transform.localPosition = readyPos;
                sheeple.body.useGravity = true;
                sheeple.Activate = true;
            }
            containmentCounts = 0;
        }
        public void ResetSheep()
        {
            foreach (SheepDynamics sheeple in Sheep)
            {
                sheeple.Reset();
            }
        }

        public SheepDynamics[] GetSheep()
        {
            SheepDynamics[] output = this.GetComponentsInChildren<SheepDynamics>();
            return output;
        }

        public void ShowField()
        {
            Field.localPosition = Vector3.zero;
        }
        public void HideField()
        {
            Field.localPosition = new Vector3(0, -100f, 0);
        }

        private int CheckAllInTargetRegion()
        {
            float max_dist = 0;

            Vector3 targetPos = TargetRegion.position;

            if (Sheep.Length >= 2)
            {
                for (int i = 0; i < Sheep.Length-1; i++)
                {
                    targetPos.y = Sheep[i].transform.position.y;

                    float sheep_dist = Vector3.Distance(Sheep[i].transform.position, targetPos);

                    if (sheep_dist > max_dist)
                    {
                        max_dist = sheep_dist;
                    }

                    if (Sheep.Length < 2 || max_dist >= containment_criteria) Contained = 0;
                    else Contained = 1;              //0 means wool will change to red color

                }
            }
            return Contained;
        }

        private void CheckSheepSpread()
        {
            float max_dist = 0;

            if (Sheep.Length >= 2)
            {
                for (int i = 0; i < Sheep.Length - 1; i++)
                {
                    for (int j = i + 1; j < Sheep.Length; j++)
                    {
                        float sheep_dist = Vector3.Distance(Sheep[i].transform.position, Sheep[j].transform.position);

                        if (sheep_dist > max_dist)
                        {
                            max_dist = sheep_dist;
                        }
                    }
                }
            }
            if (Sheep.Length < 2 || max_dist >= containment_criteria) Contained = 0;
            else Contained = 1;              //0 means wool will change to red color
        }

        public Vector3 GetHerdCOM()
        {
            Vector3 centerOfMass = Vector3.zero;

            foreach (SheepDynamics animal in Sheep)
            {
                Vector3 s_position = animal.transform.position;
                centerOfMass += s_position;

            }
            centerOfMass = centerOfMass / Sheep.Length;                   //Calculate mean animal position
            return centerOfMass;
        }

        public Vector3 GetTargetCenter()
        {
            return TargetRegion.position;
        }

        public string GetHeader()
        {
            string output = "";

            for (int i = 0; i < Sheep.Length; i++)
            {
                output += ("Agent" + (i + 1).ToString() + "_X," + "Agent" + (i + 1).ToString() + "_Z");
                if (i < Sheep.Length - 1) output += ",";
            }

            return output;
        }
        public string LatestDataString()
        {
            string output = "";

            for (int i = 0; i < Sheep.Length; i++)
            {
                output += (Sheep[i].transform.position.x + "," + Sheep[i].transform.position.z);
                if (i < Sheep.Length - 1) output += ",";
            }

            return output;
        }
        #endregion
    }
}
