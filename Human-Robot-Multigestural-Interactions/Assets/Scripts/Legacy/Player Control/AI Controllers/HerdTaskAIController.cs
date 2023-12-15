namespace MQ.MultiAgent
{
    using UnityEngine;
    using RootMotion.FinalIK;


    //Change code to what PNAS code was. Currently using DST implementation.
    public class HerdTaskAIController : AIController
    {
        SheepDynamics[] Sheep;
        public override void Initialize(Player Self)
        {
            base.Initialize(Self);

            _myGaze.solver.head = new IKSolverLookAt.LookAtBone(Self.Eyes.HeadRoot);
            _myGaze.solver.eyes = new IKSolverLookAt.LookAtBone[2] {
                                        new IKSolverLookAt.LookAtBone(Self.Eyes.Left),
                                        new IKSolverLookAt.LookAtBone(Self.Eyes.Right)};

            _myGaze.solver.SetLookAtWeight(1f, 0.5f, 0.8f, 1f);

            Sheep = HerdControl.Instance.GetSheep();
        }

        private Vector3 ContainmentRegionPos { get { return HerdControl.Instance.TargetRegion.position; } }                               //Center of target region. Need to define.
        private Transform ReadyCube { get { return Self.HandInitCube; } }       //Cube object that AI should move to to start trial

        Transform _partnerPlayer;                                             //The other players engaged with task.
        Transform PartnerPlayer
        {
            get
            {
                if (_partnerPlayer == null)
                {
                    if (Self.tag == "Player")   //If this is primary player
                    {
                        var player = GameObject.FindGameObjectWithTag("OtherPlayer").GetComponent<SyncActors>();
                        _partnerPlayer = player.myFinger;

                    }
                    else if (Self.tag == "OtherPlayer") //If this is local AI partner
                    {
                        var player = GameObject.FindGameObjectWithTag("Player").GetComponent<SyncActors>();
                        _partnerPlayer = player.myFinger;
                    }
                    else
                    {
                        Debug.LogError("Error: Avatar tag not what expected.");
                    }


                }
                return _partnerPlayer;
            }
        }


        #region Private Fields
        // System Variables for integration
        #region State Variable Initiation
        private float dt = Time.deltaTime;      //timestep for integration. Use deltaTime as it gives more control if this is done in Update or FixedUpdate loop.
        private float dogRt = 0.0f;             //Dog Position Radius from Center
        private float dogdRdt = 0.0f;           //Dog Velocity Radius from Center
        private float dogdRvdt = 0.0f;          //Dog Acceleration Radius from Center
        private float dogThetat = 0.0f;         //Dog Angle from Center
        private float dogdThetadt = 0.0f;       //Dog Angle Velocity from Center
        private float dogdThetavdt = 0.0f;      //Dog Angle Acceleration from Center
        private float dogbt = 0.0f;             //Dog Paramater Function Determining S&R/COC
        private float dogdbdt = 0.0f;           //Dog Velocity Parameter Function Determining S&R/COC
        // private float omegadt = 0.00f;          //Dog Frequency Velocity Parameter Function responsible for frequency coupling.
        private float otherRt = 0.00f;          //Other Player Position Radius from Center
        private float oDogThetat = 0.0f;        //Other Player's oscillation position
        private float last_oDogThetat = 0.0f;   //Other Player's oscillation position on last timestep
        private float oDogdThetadt = 0.0f;      //Other Player's oscillation velocity ( (oDogThetat-last_oDogThetat)/dt )
        #endregion

        // System Parameters
        #region Parameter Coefficients
        //Dog Radius Parameters
        private float by = 10.9987f;                                    //Velocity dampening term
        private float epsilon = 98.70672f;                         //Velocity stiffness term
        private float preferred_distance = 0.03f;//0.061539f;           //Preferred mininum distance from targetted sheep
        private float min_containment_dist = .096f;//0.062f;            //Mininum distance which will classify if there is a "farthest sheep."

        //Dog Theta/Oscillation Parameters
        private float beta = 0.161641f;                         //Rayleigh escapement parameter
        private float gamma = 7.22282f;                         //Van der Pol escapement parameter
        private float omegat = 5.652f;                           //Angular Velocity Stiffness/Frequency of Oscillation
        private float coupling_alpha = -0.2f;                   //Coupling Term to Other Dog
        private float coupling_beta = 0.2f;                     //Coupling Term to Other Dog

        //Oscillatory Frequency Dynamics
        private float omega_pref = 5.652f;                      //Preferred Oscillatory Frequency (radians/s)
        // private float freq_couple = 2;                          //Coupling strength to partner's perturbations
        // private float freq_relax = 1;                           //Relaxation paramater towards preferred frequency.

        //Alpha Parameter Function Parameters
        private float delta = 23.08993f;                        //Scaling parameter 1
        private float alpha = 80.59288f;                        //Scaling parameter 2
        private int heaviside = 0;                         //Heaviside value which determines if "dogThetat" should move to furthest sheep (value = 1), or centered on "myEdog"'s sagittal plane
        #endregion

        // Noise Parameters
        private float noiseOsc = 2f;                        //Noise of Oscillation
        private float noiseDist = 2f;                       //Noise of Radius

        private bool isTaskRunning;                             //Flag to denote if task is being run.
        private int _side { get { return Self.Side; } }         //Side player is on.


        private Transform _playerTargetObj;                 //The targetted object to pursue.
        private Vector3 _playerTargetPos;                   //The goal position of hand.
        private Vector3 _clampedPlayerPos;                  //Clamped position (to constrain to be within game field).


        //Radial coordinates
        private float _playerTargetTheta = 0.0f;
        private Vector3 _playerTargetTaskSpace = Vector3.zero;
        private Vector3 _refPole = Vector3.zero;

        float taskStartDelay;
        float trialStartTime;
        #endregion

        #region Override Methods
        //Reset AI behaviour.
        public override void ResetController()
        {
            dogdRdt = 0.0f;
            dogdRvdt = 0.0f;
            dogdThetadt = 0.0f;
            dogdThetavdt = 0.0f;
            dogbt = 0.0f;
            dogdbdt = 0.0f;
            oDogThetat = 0.0f;
            last_oDogThetat = 0.0f;
            oDogdThetadt = 0.0f;
            // omegadt = 0.0f;
            omegat = omega_pref;
            _refPole = ContainmentRegionPos;
            _refPole.y = 0f;

            Vector3 relativepos = Self.Hand.transform.position - _refPole;                                           //Get Position relative to mean sheep position
                                                                                                                     //Vector3 relativepos = thisPlayer.transform.position - targetGoal.transform.position;
            relativepos.y = 0f;
            dogRt = relativepos.magnitude;                                                                                  //Set DogRt
            if (_side < 0) { dogThetat = Mathf.Deg2Rad * Vector3.Angle(Vector3.back, relativepos); }                         //Set dogThetat, correcting for side of gamefield "myEdog" is on, so that theta=0 on "myEdog"'s sagittal plane.
            else { dogThetat = Mathf.Deg2Rad * Vector3.Angle(Vector3.forward, relativepos); }
            if (relativepos.x < 0) { dogThetat = dogThetat * -1; }
        }

        //Compute next state for AI Controller (hand, eyes and head).
        public override void InitiateController()
        {
            ResetController();
            GazeTarget = ReadyCube;
            _playerTargetObj = ReadyCube;
            ActiveAI = true;
            isTaskRunning = false;
        }
        public override void RunController()
        {
            isTaskRunning = true;
            taskStartDelay = Random.Range(0.2f, 0.8f);
            trialStartTime = Time.timeSinceLevelLoad;

        }
        public override void DeactivateController()
        {
            //Need to ensure hand goes back to start location?
            GazeTarget = Self.Hand.transform;
            //GazeTarget = null;
            ActiveAI = false;
            isTaskRunning = false;
        }

        public override void UpdateState()
        {
            base.UpdateState();
            if (ActiveAI)
            {
                if (isTaskRunning)
                {
                    if (Time.timeSinceLevelLoad - trialStartTime > taskStartDelay)
                    {
                        _playerTargetObj = SelectAgentMySide(ContainmentRegionPos, _side);
                        //_playerTargetObj = SelectAgentToPursue(ContainmentRegionPos);
                        ComputeState();
                    }
                    else
                    {
                        GazeTarget = HerdControl.Instance.TargetRegion;     //If not started yet, look at task region.
                    }
                }
                else
                {
                    ComputeState();
                }
            }
        }
        #endregion

        #region Helper Methods
        private GameObject[] GetOtherPlayers(GameObject[] players)
        {
            GameObject[] output = new GameObject[players.Length - 1];

            int counter = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (Self.Hand.transform != players[i].transform)
                {
                    output[counter] = players[i];
                    counter++;
                }
            }

            if (counter != players.Length - 1)
            {
                Debug.LogError("Error Getting Other Players in Herding Task. Number of 'other players' not what expected.");
            }
            return output;

        }

        private Transform SelectAgentToPursue(Vector3 reference)
        {
            //First try to select object that is furthest from reference and closest to this Actor.
            //If none of the objects are closer to this player, select the cloest agent.
            Transform furthestAgent = null;
            Transform closestAgent = null;
            float maxDist = 0;
            float minDist = Mathf.Infinity;
            float distToRef = 0;
            float distToActor = Mathf.Infinity;

            int numAgentsUnderControl = 0;

            foreach (SheepDynamics sheeple in Sheep)
            {
                if (sheeple.Activate)
                {
                    //Get cloest sheeple to player and select the one furthest from reference.
                    if (sheeple.ClosestPlayerName == Self.name)
                    {
                        numAgentsUnderControl++;
                        distToRef = Vector3.Distance(sheeple.transform.position, reference);

                        if (distToRef > maxDist)
                        {
                            maxDist = distToRef;
                            furthestAgent = sheeple.transform;
                        }
                    }
                    //If there is no "cloest" sheeple, just select the one closest.
                    else
                    {
                        distToActor = Vector3.Distance(sheeple.transform.position, Self.Hand.transform.position);

                        if (distToActor < minDist)
                        {
                            minDist = distToActor;
                            closestAgent = sheeple.transform;
                        }
                    }
                }
            }

            //If not agent is select, select closest object
            if (furthestAgent == null)
            {
                return closestAgent;
            }
            else
            {
                return furthestAgent;
            }
        }

        private Transform SelectAgentMySide(Vector3 reference, float side)
        {
            //First try to select object that is furthest from reference and closest to this Actor.
            //If none of the objects are closer to this player, select the cloest agent.
            Transform furthestAgent = null;
            float maxDist = 0;
            float distToRef = 0;

            foreach (SheepDynamics sheeple in Sheep)
            {
                if (sheeple.Activate && Mathf.Sign(sheeple.transform.position.z) == side)
                {
                    distToRef = Vector3.Distance(sheeple.transform.position, reference);

                    if (distToRef > maxDist)
                    {
                        maxDist = distToRef;
                        furthestAgent = sheeple.transform;
                    }
                }
            }

            //If not agent is select, select closest object
            if (furthestAgent == null)
            {
                return SelectAgentToPursue(reference);
            }
            else
            {
                return furthestAgent;
            }
        }

        private void ComputeState()
        {
            dt = Time.deltaTime;
            //This needs to be redone to match closer to PNAS code.
            _refPole = ContainmentRegionPos;
            _refPole.y = 0;

            //***Get Other Player's Angular Position and Velocity in Relationship to Target Center***\\\
            Vector3 otherPlayerTaskPos = PartnerPlayer.position;
            otherPlayerTaskPos.y = 0;
            otherRt = Vector3.Distance(otherPlayerTaskPos, _refPole);
            oDogThetat = Mathf.Atan((otherPlayerTaskPos.x - _refPole.x) / (Mathf.Abs(otherPlayerTaskPos.z - _refPole.z)));
            if (Mathf.Sign(otherPlayerTaskPos.z - _refPole.z) != _side * -1) { oDogThetat = Mathf.Sign(otherPlayerTaskPos.x - _refPole.x) * (Mathf.PI - Mathf.Abs(oDogThetat)); }          //Done so that value is not limited to dog's primary side of the field.
            oDogdThetadt = (oDogThetat - last_oDogThetat) / dt;

            //***Get distance and orientation of furthest sheep from virtual sheepherd object***\\
            _playerTargetPos = _playerTargetObj.position;
            _playerTargetPos.y = 0;

            //Create offset of Actor to targetted Agent. Ensures that Actor stays outside of min_containment_dist
            float offsetMag;
            if (_playerTargetObj == ReadyCube) offsetMag = 0f;  //Move to object if it's the ReadyCube
            else offsetMag = preferred_distance;

            //***Check for behavior mode switch***\\
            if ((_playerTargetPos - _refPole).magnitude < min_containment_dist)         //If the targetted sheep is within the minimum containment distance.
            {
                heaviside = 0;                   //Switch to COC behavior
                offsetMag += (min_containment_dist) - (_playerTargetPos - _refPole).magnitude;      //Ensure that dog is outside min_containment area. Currently not being used because radial component is min_dist+preferred dist.
                omegat = 5.652f;
            }
            else
            {
                heaviside = 1;                    //Switch to S&R behavior
                omegat = 5.652f * 2;
            }

            //Get Position of Actor's target position.
            float playerMagnitude = Vector3.Distance(_playerTargetPos, _refPole) + offsetMag;        //Distance between Targetted Agent and Target Region + Actor's preferred offset.

            Vector3 targetSheepDirection = _playerTargetPos - _refPole;
            targetSheepDirection = targetSheepDirection.normalized;
            _playerTargetTaskSpace = playerMagnitude * targetSheepDirection + _refPole;
            _playerTargetTaskSpace -= _refPole;
            _playerTargetTaskSpace.y = 0;

            if (_side < 0) { _playerTargetTheta = Mathf.Deg2Rad * Vector3.Angle(Vector3.back, _playerTargetTaskSpace); }        //Get Angle of target_sheep if on negative side of game field
            else { _playerTargetTheta = Mathf.Deg2Rad * Vector3.Angle(Vector3.forward, _playerTargetTaskSpace); }              //Get Angle of target_sheep if on positive side of game field
            if (_playerTargetTaskSpace.x < 0) { _playerTargetTheta = _playerTargetTheta * -1; }                                      //Correct Angle if sheep is on negative x region of screen (as Vector3.Angle only returns positive values).

            //***********INTEGRATE SYSTEM*************************************************
            //***Alpha Parameter dynamics equation for energy parameter for hybrid; Hopf bifurcation occurs when dogAt < 0 ***\\
            dogdbdt = -delta * (dogbt - alpha * ((_playerTargetPos - _refPole).magnitude - (min_containment_dist)));

            //***Frequency Dynamics***\\
            //omegadt = freq_relax * (omega_pref - omegat) + (1 - heaviside) * freq_couple * (oDogThetat) * (dogdThetadt / (Mathf.Sqrt(Mathf.Pow(dogThetat, 2) + Mathf.Pow(dogdThetadt, 2))));
            //if (float.IsNaN(omegadt)) omegadt = freq_relax * (omega_pref - omegat); //omegadt is NaN if denominator evaluates to 0.

            //***Set Radius***\\
            dogdRvdt = -by * dogdRdt - epsilon * (dogRt - heaviside * (_playerTargetTaskSpace.magnitude) - (1 - heaviside) * (preferred_distance + min_containment_dist)) + (Random.value - 0.5f) * noiseDist;

            //***Set Polar Angle***\\
            dogdThetavdt = -dogbt * dogdThetadt - (beta * Mathf.Pow(dogdThetadt, 3)) - (gamma * Mathf.Pow(dogThetat, 2) * dogdThetadt) - Mathf.Pow(omegat, 2) * (dogThetat - _playerTargetTheta * heaviside) + (1 - heaviside) * ((dogdThetadt - oDogdThetadt) * (coupling_alpha + coupling_beta * Mathf.Pow((dogThetat - oDogThetat), 2))) + (Random.value - 0.5f) * noiseOsc;
            //dogdThetavdt = -dogbt * dogdThetadt - (beta * Mathf.Pow(dogdThetadt, 3)) - (gamma * Mathf.Pow(dogThetat, 2) * dogdThetadt) - Mathf.Pow(omega, 2) * (dogThetat - _playerTargetTheta * heaviside);

            //Check for Errors.
            if (float.IsNaN(dogdThetavdt) || float.IsInfinity(dogdThetavdt))
            {
                Debug.LogError("ERROR: Theta Acceleration a not valid number. Variable values: dogdThetavdt: " + dogdThetavdt + ", dogdThetadt: " + dogdThetadt + ", dogdThetat: " + dogThetat + ", oDogThetat: " + oDogThetat);
                ResetController();
            }
            if (float.IsNaN(dogdRvdt) || float.IsInfinity(dogdRvdt))
            {
                Debug.LogError("ERROR: Radius Acceleration a not valid number.");
                ResetController();
            }

            //***Update Variables for next iteration***\\
            dogRt = dogRt + (dogdRdt * dt);
            dogdRdt = dogdRdt + (dogdRvdt * dt);
            dogThetat = dogThetat + (dogdThetadt * dt);
            dogdThetadt = dogdThetadt + (dogdThetavdt * dt);
            dogbt = dogbt + (dogdbdt * dt);
            //omegat = omegat + (omegadt * dt);


            //Ensure dogThetat is within 2PI
            if (Mathf.Abs(dogThetat) > 2 * Mathf.PI)
            {
                dogThetat = dogThetat - 1 * Mathf.Sign(dogThetat) * 2 * Mathf.PI * (Mathf.Floor(Mathf.Abs(dogThetat) / (2 * Mathf.PI)));
            }

            if (float.IsNaN(dogThetat) || float.IsInfinity(dogThetat))         //If for some reason a NaN value results, force the trial to end.
            {
                Debug.LogError("ERROR: NaN dogThetat");
                ResetController();
            }
            else if (float.IsNaN(dogRt) || float.IsInfinity(dogRt))
            {
                Debug.LogError("ERROR: NaN dogRt");
                ResetController();
            }
            else
            {
                _clampedPlayerPos = new Vector3(Mathf.Clamp((Mathf.Sin(dogThetat) * dogRt) + _refPole.x, -1 * HerdControl.Instance.Field.lossyScale.x / 2f, HerdControl.Instance.Field.lossyScale.x / 2f), _playerTargetObj.position.y, Mathf.Clamp((_side * Mathf.Cos(dogThetat) * dogRt) + _refPole.z, -1 * HerdControl.Instance.Field.lossyScale.z / 2f, HerdControl.Instance.Field.lossyScale.z / 2f));         //Convert polar coordinate values to cartesian and clamp "myEdog"'s position within game field.

                if (float.IsNaN(_clampedPlayerPos.magnitude))
                {
                    Debug.LogError("Unable to convert from polar to cartesian. State of dogThetat: " + dogThetat + ". State of dogRt: " + dogRt);
                }
                last_oDogThetat = oDogThetat;                           //Set previous angle for other player.

                //Update Player States.
                HandTarget = _clampedPlayerPos;     //Set new position for "this player"

                GazeTarget = heaviside == 0 ? HerdControl.Instance.TargetRegion : _playerTargetObj; //If oscillating, look at center of field.
            }
        }

        public override void Initiate()
        {
        }

        #endregion
    }
}