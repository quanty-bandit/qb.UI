using qb.Events;
using TriInspector;
using UnityEngine;

namespace qb.UI
{
    /// <summary>
    /// Abstract base class for a UI progress bar component that manages and displays a normalized value.
    /// </summary>
    public abstract class ProgressBar : MonoBehaviour
    {
        [OnValueChanged(nameof(UpdateUI))]
        [SerializeField,Range(0,1)]
        float value;

        [SerializeField,PropertySpace(spaceAfter:10)]
        ECProvider_R<float> setValueChannel;


        public float Value
        {
            get { return value; }
            set 
            {
                this.value = Mathf.Clamp01(value);
                UpdateUI();
            }
        }
        protected virtual void UpdateUI()
        {

        }

        protected virtual void OnEnable()
        {
            setValueChannel.AddListener(SetValue);
        }


        protected virtual void OnDisable()
        {
            setValueChannel.RemoveListener(SetValue);
        }

        private void SetValue(float value,object sender)
        {
            Value = value;
        }

    }
}
