namespace MQ.MultiAgent
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using Mirror;
    using Mirror.Discovery;

    public class UI_TitleScreen : MonoBehaviour
    {
        [SerializeField] Text _participantField;
        [SerializeField] Text _debugMessage;
        [SerializeField] Dropdown _genderSelect;
        [SerializeField] Dropdown _partnerGenderSelect;
        [SerializeField] Dropdown _taskSelect;
        [SerializeField] Toggle _aiSelect;

        [SerializeField] NetworkDiscoveryHUD HUD;

        private string _gender;
        public string Gender
        {
            get { return _gender; }
            set
            {
                _gender = value;
            }
        }

        private void Start()
        {
            HUD.enabled = false;
        }

        /// <summary>Update global parameters on gui update.</summary>
        private void OnGUI()
        {
            if (_aiSelect != null)
            {
                TitleScreenData.IsAI = _aiSelect.isOn;
            }

            if (!String.IsNullOrEmpty(_participantField.text) && int.Parse(_participantField.text) >= 0)
            {
                TitleScreenData.SubjectNum = int.Parse(_participantField.text);
            }

            if (_genderSelect != null && _genderSelect.value != 0)
            {
                TitleScreenData.Gender = _genderSelect.options[_genderSelect.value].text;
                TitleScreenData.GenderIndx = _genderSelect.value - 1;
            }

            if (_partnerGenderSelect != null && _partnerGenderSelect.value != 0)
            {
                TitleScreenData.PartnerGender = _partnerGenderSelect.options[_partnerGenderSelect.value].text;
                TitleScreenData.PartnerGenderIndx = _partnerGenderSelect.value - 1;
            }

            if (_taskSelect != null && _taskSelect.value != 0)
            {
                TitleScreenData.Task = TaskType.Box;
                switch (_taskSelect.value)
                {
                    case 1:
                        TitleScreenData.Condition = "5050_HeadConstrained";
                        break;
                    case 2:
                        TitleScreenData.Condition = "5050_HeadUnConstrained";
                        break;
                    case 3:
                        TitleScreenData.Condition = "7525_HeadConstrained";
                        break;
                    case 4:
                        TitleScreenData.Condition = "7525_HeadUnConstrained";
                        break;
                    case 5:
                        TitleScreenData.Condition = "2575_HeadConstrained";
                        break;
                    case 6:
                        TitleScreenData.Condition = "2575_HeadUnConstrained";
                        break;
                    default:
                        TitleScreenData.Condition = "2575_HeadUnConstrained";
                        break;
                }
            }
        }
    }
}


