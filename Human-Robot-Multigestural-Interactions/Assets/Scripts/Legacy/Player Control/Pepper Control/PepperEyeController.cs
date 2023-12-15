namespace MQ.MultiAgent
{
    using UnityEngine;
    using Tobii.XR;

    class PepperEyeController: EyeController
    {
        private void Update()
        {
            //Default looking behavior if not changed below.
            Vector3 gazeDirectionL = Vector3.forward;
            Vector3 gazeDirectionR = Vector3.forward;
            bool leftEyeClosed = true;
            bool rightEyeClosed = true;

            if (IsAI)
            {
                if (GazeValid)
                {
                    leftEyeClosed = false;
                    rightEyeClosed = false;
                }
            }
            else
            {
                //If Eye-tracker is detected.
                if (EYES != null && EYES.Connected)
                {
                    // Get local copies.
                    var eyeDataLeft = EYES.LatestProcessedGazeData.Left;
                    var eyeDataRight = EYES.LatestProcessedGazeData.Right;

                    // Get local transform direction.
                    gazeDirectionL = _cameraTransform.InverseTransformDirection(eyeDataLeft.GazeRayWorld.direction);
                    gazeDirectionR = _cameraTransform.InverseTransformDirection(eyeDataRight.GazeRayWorld.direction);

                    // If direction data is invalid use other eye's data or if that's invalid use last good data.
                    if (!eyeDataLeft.GazeRayWorldValid)
                    {
                        gazeDirectionL = eyeDataRight.GazeRayWorldValid ? gazeDirectionL : _lastGoodDirectionL;
                    }
                    if (!eyeDataRight.GazeRayWorldValid)
                    {
                        gazeDirectionR = eyeDataLeft.GazeRayWorldValid ? gazeDirectionR : _lastGoodDirectionR;
                    }

                    // Combine vertical gaze direction by using the average Y.
                    var averageYDirection = (gazeDirectionL.y + gazeDirectionR.y) / 2f;
                    gazeDirectionL.y = averageYDirection;
                    gazeDirectionR.y = averageYDirection;

                    // Save last good data.
                    _lastGoodDirectionL = gazeDirectionL;
                    _lastGoodDirectionR = gazeDirectionR;

                    var eyeTrackingDataLocal = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.Local);
                    leftEyeClosed = eyeTrackingDataLocal.IsLeftEyeBlinking;
                    rightEyeClosed = eyeTrackingDataLocal.IsRightEyeBlinking;
                }
            }

            // AnimateEyeLids(leftEyeClosed, rightEyeClosed);
            if (!IsAI) SmoothAndUpdateEyes(gazeDirectionL, gazeDirectionR);
        }

        protected override void AnimateEyeLids(bool leftEyeClosed, bool rightEyeClosed) {}
    }
}