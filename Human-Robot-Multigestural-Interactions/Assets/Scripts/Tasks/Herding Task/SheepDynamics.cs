namespace MQ.MultiAgent
{
    using UnityEngine;
    using System.Collections;
    using Mirror;

    //Maybe make scale 2?

    //This script control's the sheep's dynamics. Modified from the script used from Nalepka et al., (2017) "Herd Those Sheep: Emergent Multiagent Coordination and Behavioral-Mode Switching" https://www.ncbi.nlm.nih.gov/pubmed/28375693
    //Questions? Contact Patrick Nalepka (https://scholar.google.com/citations?user=P9M91TgAAAAJ&hl=en)


    //Note: Sheep's rigidbody is set to default values (mass = 1, angular drag = 0.05).
    //Note: Sheep's scale = (0.024, 0.024, 0.024)
    //Note: Game field dimensions: (1.17 (x) x 0.62m (z)). Up is y-positive.


    //Need to determine if Current dynamics are appropriate.

    public class SheepDynamics : NetworkBehaviour
    {
        #region Fields
        private float sheepSpeed = 0.6f;                //Sheep Speed
        private float repulsionFactor = 6;              //Amount of repulsion
        private float repulsionDistance = 0.12f;        //Distance threshold for repulsion

        private float maxforce_random = 10f;
        private float maxForce_repel = 30f;

        private HerdControl HERD;

        public Rigidbody body;
        private Vector3 movementForce;              //Force vector acting upon this sheep.
        public Vector3 InitPos;                     //Initial Position in Editor.
        #endregion

        #region Properties
        public bool Activate { get; set; }

        private Transform _closestDogAndPlayer;
        public Transform ClosestDogAndPlayer
        {
            get
            {
                return _closestDogAndPlayer;
            }
        }
        [SyncVar] public string ClosestPlayerName;
        private SyncActors[] _players;
        private SyncActors[] Players
        {
            get
            {
                if (_players == null) _players = GetPlayers(HerdControl.Instance.Dogs);
                else if (SyncExperimentPhase.Instance.Players.Length != _players.Length) _players = GetPlayers(HerdControl.Instance.Dogs);
                return _players;
            }
        }
        #endregion


        private void Awake()
        {
            Activate = false;
            body = this.GetComponent<Rigidbody>();
            movementForce = Vector3.zero;
            Physics.IgnoreLayerCollision(8, 9);         //Ignore physical collisions between sheep (8), and dog (9). This has to be defined in Unity's "Layers". All sheep should be assigned to the same layer, and all dogs to the same layer (8 and 9 respecively here)
            InitPos = this.transform.localPosition;
        }
        private void Start()
        {
            HERD = HerdControl.Instance;
        }
        void FixedUpdate()
        {
            if (isServer)
            {

                if (Activate)
                {
                    _closestDogAndPlayer = UpdateClosestDogAndPlayer();
                    Vector3 repulseVector = Vector3.zero;                                                           //The repulsion vector applied to sheep.

                    if (HERD.Dogs != null)
                    {
                        foreach (GameObject dog in HERD.Dogs)
                        {
                            Vector3 dist_to_dog = transform.position - dog.transform.position;
                            dist_to_dog.y = 0;

                            if (dist_to_dog.magnitude < repulsionDistance)                                                 //If dog is within repulsion distance, their influence is added to repulse vector
                            {
                                repulseVector += dist_to_dog.normalized * (repulsionDistance / dist_to_dog.magnitude);
                            }
                        }
                    }

                    if (repulseVector == Vector3.zero)                                                              //If neither dog is within repulsionDistance, a random force is added to movementForce vector. This will produce Brownian motion if dogs are not within range
                    {
                        movementForce += RandomMotion();
                        AddForce(maxforce_random, false);                                                                            //Random forces acting on sheep is clamped at 10.
                    }
                    else
                    {
                        movementForce = repulseVector * repulsionFactor;
                        AddForce(maxForce_repel, true);                                                                            //Add Force to sheep, clamped at 30
                    }

                    Vector3 tempDist = this.transform.position - HERD.Field.position;
                    tempDist.y = 0;

                    if (Mathf.Abs(tempDist.x) > HERD.Field.lossyScale.x / 2f || Mathf.Abs(tempDist.z) > HERD.Field.lossyScale.z / 2f)
                    {
                        Debug.Log("A sheep escaped!");
                        SyncExperimentPhase.Instance.ExperimentPhase = 3;
                        HerdControl.Instance.Score = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Reset sheep's dynamics and position.
        /// </summary>
        public void Reset()
        {
            body.velocity = Vector3.zero;                                  //Reset velocity
            body.angularVelocity = Vector3.zero;                           //Reset angular velocity
            this.transform.localPosition = InitPos;
        }

        /// <summary>
        /// Return a random force direction in XZ plane.
        /// </summary>
        /// <returns></returns>
        private Vector3 RandomMotion()                                                                                  //Calculate random force.
        {
            return new Vector3(Random.value - .5f, 0.0f, Random.value - .5f);
        }

        /// <summary>
        /// Add Force.
        /// </summary>
        /// <param name="maxForce">Maximum Force Allowed.</param>
        /// <param name="reset">Resets force information.</param>
        private void AddForce(float maxForce, bool reset)
        {
            movementForce = Vector3.ClampMagnitude(movementForce, maxForce);                                //Multiply radius by the unit circle value (movementForce.normalized.magnitude = 1).
            GetComponent<Rigidbody>().AddForce(movementForce * sheepSpeed * Time.fixedDeltaTime);           //Add force to sheep.
            if (reset) movementForce = Vector3.zero;
        }

        /// <summary>
        /// Return transform of closest player "dog" object
        /// </summary>
        /// <returns></returns>
        private Transform UpdateClosestDogAndPlayer()
        {
            Transform closestPlayer = null;

            float minDist = Mathf.Infinity;
            float distToPlayer = 0;

            foreach (SyncActors player in Players)
            {
                distToPlayer = Vector3.Distance(this.transform.position, player.myHead.position) + Vector3.Distance(this.transform.position, player.myFinger.position);        //should I only look at x and z dimension?

                if (distToPlayer < minDist)
                {
                    minDist = distToPlayer;
                    closestPlayer = player.myFinger;
                }
            }
            ClosestPlayerName = closestPlayer.parent.name;
            return closestPlayer;
        }

        /// <summary>
        /// Return Player "Player" Class objects.
        /// </summary>
        /// <param name="players"></param>
        /// <returns></returns>
        private SyncActors[] GetPlayers(GameObject[] players)
        {
            SyncActors[] controllers = new SyncActors[players.Length];

            for (int i = 0; i <players.Length;i++)
            {
                try
                {
                    controllers[i] = players[i].transform.parent.GetComponent<SyncActors>();
                }
                catch
                {
                    Debug.LogError("SheepDynamics Error: Cannot find player with 'Hand' object");
                }
            }

            return controllers;
        }
    }
}